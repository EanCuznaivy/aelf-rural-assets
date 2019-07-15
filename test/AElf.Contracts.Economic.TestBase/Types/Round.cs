using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    internal partial class Round
    {
        public long RoundId =>
            RealTimeMinersInformation.Values.Select(bpInfo => bpInfo.ExpectedMiningTime.Seconds).Sum();

        public Hash GetHash(bool isContainPreviousInValue = true)
        {
            return Hash.FromRawBytes(GetCheckableRound(isContainPreviousInValue));
        }
        
        /// <summary>
        /// This method is only available when the miners of this round is more than 1.
        /// </summary>
        /// <returns></returns>
        public int GetMiningInterval()
        {
            if (RealTimeMinersInformation.Count == 1)
            {
                // Just appoint the mining interval for single miner.
                return 4000;
            }

            var firstTwoMiners = RealTimeMinersInformation.Values.Where(m => m.Order == 1 || m.Order == 2)
                .ToList();
            var distance =
                (int) (firstTwoMiners[1].ExpectedMiningTime.ToDateTime() -
                       firstTwoMiners[0].ExpectedMiningTime.ToDateTime())
                .TotalMilliseconds;
            return distance > 0 ? distance : -distance;
        }

        internal bool IsTimeSlotPassed(string publicKey, DateTime dateTime,
            out MinerInRound minerInRound)
        {
            minerInRound = null;
            var miningInterval = GetMiningInterval();
            if (!RealTimeMinersInformation.ContainsKey(publicKey)) return false;
            minerInRound = RealTimeMinersInformation[publicKey];
            return minerInRound.ExpectedMiningTime.ToDateTime().AddMilliseconds(miningInterval) <= dateTime;
        }

        /// <summary>
        /// If one node produced block this round or missed his time slot,
        /// whatever how long he missed, we can give him a consensus command with new time slot
        /// to produce a block (for terminating current round and start new round).
        /// The schedule generated by this command will be cancelled
        /// if this node executed blocks from other nodes.
        /// 
        /// Notice:
        /// This method shouldn't return the expected mining time from round information.
        /// To prevent this kind of misuse, this method will return a invalid timestamp
        /// when this node hasn't missed his time slot.
        /// </summary>
        /// <returns></returns>
        public Timestamp ArrangeAbnormalMiningTime(string publicKey, DateTime dateTime,
            int miningInterval = 0)
        {
            if (!RealTimeMinersInformation.ContainsKey(publicKey))
            {
                return new Timestamp {Seconds = long.MaxValue};;
            }

            if (miningInterval == 0)
            {
                miningInterval = GetMiningInterval();
            }

            if (!IsTimeSlotPassed(publicKey, dateTime, out var minerInRound) && minerInRound.OutValue == null)
            {
                return new Timestamp {Seconds = long.MaxValue};;
            }

            if (GetExtraBlockProducerInformation().Pubkey == publicKey)
            {
                var distance = (GetExtraBlockMiningTime() - dateTime).TotalMilliseconds;
                if (distance > 0)
                {
                    return GetExtraBlockMiningTime().ToTimestamp();
                }
            }

            if (RealTimeMinersInformation.ContainsKey(publicKey) && miningInterval > 0)
            {
                var distanceToRoundStartTime = (dateTime - GetStartTime()).TotalMilliseconds;
                var missedRoundsCount = (int) (distanceToRoundStartTime / TotalMilliseconds(miningInterval));
                var expectedEndTime = GetExpectedEndTime(missedRoundsCount, miningInterval);
                return expectedEndTime.ToDateTime().AddMilliseconds(minerInRound.Order * miningInterval).ToTimestamp();
            }

            // Never do the mining if this node has no privilege to mime or the mining interval is invalid.
            return new Timestamp {Seconds = long.MaxValue};;
        }
        
        /// <summary>
        /// Actually the expected mining time of the miner whose order is 1.
        /// </summary>
        /// <returns></returns>
        public DateTime GetStartTime()
        {
            return RealTimeMinersInformation.Values.First(m => m.Order == 1).ExpectedMiningTime.ToDateTime();
        }
        
        public Round ApplyNormalConsensusData(string publicKey, Hash previousInValue,
            Hash outValue, Hash signature)
        {
            if (!RealTimeMinersInformation.ContainsKey(publicKey))
            {
                return this;
            }

            RealTimeMinersInformation[publicKey].OutValue = outValue;
            RealTimeMinersInformation[publicKey].Signature = signature;
            if (previousInValue != Hash.Empty)
            {
                RealTimeMinersInformation[publicKey].PreviousInValue = previousInValue;
            }

            var minersCount = RealTimeMinersInformation.Count;
            var sigNum =
                BitConverter.ToInt64(
                    BitConverter.IsLittleEndian ? signature.Value.Reverse().ToArray() : signature.Value.ToArray(),
                    0);
            var supposedOrderOfNextRound = GetAbsModulus(sigNum, minersCount) + 1;

            // Check the existence of conflicts about OrderOfNextRound.
            // If so, modify others'.
            var conflicts = RealTimeMinersInformation.Values
                .Where(i => i.FinalOrderOfNextRound == supposedOrderOfNextRound).ToList();

            foreach (var orderConflictedMiner in conflicts)
            {
                // Though multiple conflicts should be wrong, we can still arrange their orders of next round.

                for (var i = supposedOrderOfNextRound + 1; i < minersCount * 2; i++)
                {
                    var maybeNewOrder = i > minersCount ? i % minersCount : i;
                    if (RealTimeMinersInformation.Values.All(m => m.FinalOrderOfNextRound != maybeNewOrder))
                    {
                        RealTimeMinersInformation[orderConflictedMiner.Pubkey].FinalOrderOfNextRound =
                            maybeNewOrder;
                        break;
                    }
                }
            }

            RealTimeMinersInformation[publicKey].SupposedOrderOfNextRound = supposedOrderOfNextRound;
            // Initialize FinalOrderOfNextRound as the value of SupposedOrderOfNextRound
            RealTimeMinersInformation[publicKey].FinalOrderOfNextRound = supposedOrderOfNextRound;

            return this;
        }
        
        public Hash CalculateSignature(Hash inValue)
        {
            // Check the signatures
            foreach (var minerInRound in RealTimeMinersInformation)
            {
                if (minerInRound.Value.Signature == null)
                {
                    minerInRound.Value.Signature = Hash.FromString(minerInRound.Key);
                }
            }

            return Hash.FromTwoHashes(inValue,
                RealTimeMinersInformation.Values.Aggregate(Hash.Empty,
                    (current, minerInRound) => Hash.FromTwoHashes(current, minerInRound.Signature)));
        }
        
        public Hash CalculateInValue(Hash randomHash)
        {
            return Hash.FromTwoHashes(Hash.FromMessage(new Int64Value {Value = RoundId}), randomHash);
        }

        /// <summary>
        /// This method for now is able to handle the situation of a miner keeping offline so many rounds,
        /// by using missedRoundsCount.
        /// </summary>
        /// <param name="miningInterval"></param>
        /// <param name="missedRoundsCount"></param>
        /// <returns></returns>
        public Timestamp GetExpectedEndTime(int missedRoundsCount = 0, int miningInterval = 0)
        {
            if (miningInterval == 0)
            {
                miningInterval = GetMiningInterval();
            }

            var totalMilliseconds = TotalMilliseconds(miningInterval);
            return GetStartTime().AddMilliseconds(totalMilliseconds)
                // Arrange an ending time if this node missed so many rounds.
                .AddMilliseconds(missedRoundsCount * totalMilliseconds)
                .ToTimestamp();
        }

        /// <summary>
        /// In current AElf Consensus design, each miner produce his block in one time slot, then the extra block producer
        /// produce a block to terminate current round and confirm the mining order of next round.
        /// So totally, the time of one round is:
        /// MiningInterval * MinersCount + MiningInterval.
        /// </summary>
        /// <param name="miningInterval"></param>
        /// <returns></returns>                                                
        public int TotalMilliseconds(int miningInterval = 0)
        {
            if (miningInterval == 0)
            {
                miningInterval = GetMiningInterval();
            }

            return RealTimeMinersInformation.Count * miningInterval + miningInterval;
        }
        
        public MinerInRound GetExtraBlockProducerInformation()
        {
            return RealTimeMinersInformation.First(bp => bp.Value.IsExtraBlockProducer).Value;
        }
        
        public DateTime GetExtraBlockMiningTime()
        {
            return RealTimeMinersInformation.OrderBy(m => m.Value.ExpectedMiningTime.ToDateTime()).Last().Value
                .ExpectedMiningTime.ToDateTime()
                .AddMilliseconds(GetMiningInterval());
        }
        
        /// <summary>
        /// Maybe tune other miners' supposed order of next round,
        /// will record this purpose to their FinalOrderOfNextRound field.
        /// </summary>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public UpdateValueInput ExtractInformationToUpdateConsensus(string publicKey)
        {
            if (!RealTimeMinersInformation.ContainsKey(publicKey))
            {
                return null;
            }

            var minerInRound = RealTimeMinersInformation[publicKey];

            var tuneOrderInformation = RealTimeMinersInformation.Values
                .Where(m => m.FinalOrderOfNextRound != m.SupposedOrderOfNextRound)
                .ToDictionary(m => m.Pubkey, m => m.FinalOrderOfNextRound);

            var decryptedPreviousInValues = RealTimeMinersInformation.Values.Where(v =>
                    v.Pubkey != publicKey && v.DecryptedPreviousInValues.ContainsKey(publicKey))
                .ToDictionary(info => info.Pubkey, info => info.DecryptedPreviousInValues[publicKey]);

            var minersPreviousInValues =
                RealTimeMinersInformation.Values.Where(info => info.PreviousInValue != null).ToDictionary(info => info.Pubkey,
                    info => info.PreviousInValue);

            return new UpdateValueInput
            {
                OutValue = minerInRound.OutValue,
                Signature = minerInRound.Signature,
                PreviousInValue = minerInRound.PreviousInValue ?? Hash.Empty,
                RoundId = RoundId,
                ProducedBlocks = minerInRound.ProducedBlocks,
                ActualMiningTime = minerInRound.ActualMiningTimes.First(),
                SupposedOrderOfNextRound = minerInRound.SupposedOrderOfNextRound,
                TuneOrderInformation = {tuneOrderInformation},
                EncryptedInValues = {minerInRound.EncryptedInValues},
                DecryptedPreviousInValues = {decryptedPreviousInValues},
                MinersPreviousInValues = {minersPreviousInValues}
            };
        }
        
        public long GetMinedBlocks()
        {
            return RealTimeMinersInformation.Values.Sum(minerInRound => minerInRound.ProducedBlocks);
        }

        public bool IsTimeToChangeTerm(Round previousRound, Timestamp blockchainStartTimestamp,
            long termNumber, long timeEachTerm)
        {
            var minersCount = previousRound.RealTimeMinersInformation.Values.Count(m => m.OutValue != null);
            var minimumCount = minersCount.Mul(2).Div(3).Add(1);
            var approvalsCount = RealTimeMinersInformation.Values.Where(m => m.ActualMiningTimes.Any())
                .Select(m => m.ActualMiningTimes.Last())
                .Count(actualMiningTimestamp =>
                    IsTimeToChangeTerm(blockchainStartTimestamp, actualMiningTimestamp, termNumber, timeEachTerm));
            return approvalsCount >= minimumCount;
        }

        /// <summary>
        /// If daysEachTerm == 7:
        /// 1, 1, 1 => 0 != 1 - 1 => false
        /// 1, 2, 1 => 0 != 1 - 1 => false
        /// 1, 8, 1 => 1 != 1 - 1 => true => term number will be 2
        /// 1, 9, 2 => 1 != 2 - 1 => false
        /// 1, 15, 2 => 2 != 2 - 1 => true => term number will be 3.
        /// </summary>
        /// <param name="blockchainStartTimestamp"></param>
        /// <param name="termNumber"></param>
        /// <param name="blockProducedTimestamp"></param>
        /// <param name="timeEachTerm"></param>
        /// <returns></returns>
        private bool IsTimeToChangeTerm(Timestamp blockchainStartTimestamp, Timestamp blockProducedTimestamp,
            long termNumber, long timeEachTerm)
        {
            return (blockProducedTimestamp - blockchainStartTimestamp).Seconds.Div(timeEachTerm) != termNumber - 1;
        }

        private byte[] GetCheckableRound(bool isContainPreviousInValue = true)
        {
            var minersInformation = new Dictionary<string, MinerInRound>();
            foreach (var minerInRound in RealTimeMinersInformation.Clone())
            {
                var checkableMinerInRound = minerInRound.Value.Clone();
                checkableMinerInRound.EncryptedInValues.Clear();
                checkableMinerInRound.ActualMiningTimes.Clear();
                if (!isContainPreviousInValue)
                {
                    checkableMinerInRound.PreviousInValue = Hash.Empty;
                }

                minersInformation.Add(minerInRound.Key, checkableMinerInRound);
            }

            var checkableRound = new Consensus.AEDPoS.Round
            {
                RoundNumber = RoundNumber,
                TermNumber = TermNumber,
                RealTimeMinersInformation = {minersInformation},
                BlockchainAge = BlockchainAge
            };
            return checkableRound.ToByteArray();
        }
        
        private static int GetAbsModulus(long longValue, int intValue)
        {
            return Math.Abs((int) longValue % intValue);
        }
    }
}
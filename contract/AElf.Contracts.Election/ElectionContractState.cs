using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        public BoolState VotingEventRegistered { get; set; }
        
        public SingletonState<Hash> TreasuryHash { get; set; }
        public SingletonState<Hash> WelfareHash { get; set; }
        public SingletonState<Hash> SubsidyHash { get; set; }
        public SingletonState<Hash> VotesRewardHash { get; set; }
        public SingletonState<Hash> ReElectionRewardHash { get; set; }

        public MappedState<string, ElectorVote> ElectorVotes { get; set; }

        public MappedState<string, CandidateVote> CandidateVotes { get; set; }

        public MappedState<string, CandidateInformation> CandidateInformationMap { get; set; }

        public SingletonState<long> CurrentTermNumber { get; set; }

        public SingletonState<PubkeyList> Candidates { get; set; }

        public SingletonState<PubkeyList> InitialMiners { get; set; }

        public SingletonState<PubkeyList> BlackList { get; set; }

        /// <summary>
        /// Vote Id -> Lock Time (seconds)
        /// </summary>
        public MappedState<Hash, long> LockTimeMap { get; set; }

        public MappedState<long, TermSnapshot> Snapshots { get; set; }

        public SingletonState<int> MinersCount { get; set; }

        /// <summary>
        /// Time unit: seconds
        /// </summary>
        public SingletonState<long> MinimumLockTime { get; set; }

        /// <summary>
        /// Time unit: seconds
        /// </summary>
        public SingletonState<long> MaximumLockTime { get; set; }

        public SingletonState<long> TimeEachTerm { get; set; }

        public SingletonState<long> MinerIncreaseInterval { get; set; }

        public SingletonState<Hash> MinerElectionVotingItemId { get; set; }
        
    }
}
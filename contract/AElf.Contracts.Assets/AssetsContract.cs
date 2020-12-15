using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Assets
{
    public class AssetsContract : AssetsContractContainer.AssetsContractBase
    {
        public override Empty Initialize(Empty input)
        {
            State.AEDPoSContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
            foreach (var miner in State.AEDPoSContract.GetCurrentMinerList.Call(new Empty()).Pubkeys)
            {
                var address = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(miner.ToHex()));
                State.PermissionMap[address] = true;
            }

            Context.LogDebug(() => "Assets contract initialized.");

            return new Empty();
        }

        public override Empty SetAssetInfo(AssetInfo input)
        {
            // Permission check.
            Assert(State.PermissionMap[Context.Sender], "No permission.");

            Assert(!string.IsNullOrEmpty(input.Name) && !string.IsNullOrEmpty(input.IdCard) && input.AssetType != 0,
                $"Incorrect asset info: {input}");

            State.AssetInfoMap[input.IdCard][input.AssetType] = new AssetInfo
            {
                Name = input.Name,
                IdCard = input.IdCard,
                AssetType = input.AssetType,
                AssetIdList = {input.AssetIdList}
            };

            foreach (var asset in input.AssetList)
            {
                Assert(asset.AssetId != 0 && !string.IsNullOrEmpty(asset.Status), $"Incorrect asset detail: {asset}");
                State.AssetMap[asset.AssetId] = asset;
            }

            return new Empty();
        }

        public override Empty SetAsset(Asset input)
        {
            // Permission check.
            Assert(State.PermissionMap[Context.Sender], "No permission.");

            Assert(input.AssetId != 0 && !string.IsNullOrEmpty(input.Status), $"Incorrect asset detail: {input}");

            State.AssetMap[input.AssetId] = input;

            return new Empty();
        }

        public override AssetInfo GetAssetInfo(GetAssetInfoInput input)
        {
            return State.AssetInfoMap[input.IdCard][input.AssetType] ?? new AssetInfo();
        }

        public override AssetInfo GetAssetInfoWithDetails(GetAssetInfoInput input)
        {
            var info = State.AssetInfoMap[input.IdCard][input.AssetType];
            if (info == null)
            {
                return new AssetInfo();
            }

            foreach (var assetId in info.AssetIdList)
            {
                var asset = State.AssetMap[assetId];
                if (asset != null)
                {
                    info.AssetList.Add(asset);
                }
            }

            return info;
        }

        public override Empty RecordJsonMessage(JsonMessage input)
        {
            // Permission check.
            Assert(State.PermissionMap[Context.Sender], "No permission.");

            Assert(!string.IsNullOrEmpty(input.Key) && !string.IsNullOrEmpty(input.Message),
                $"Incorrect json message: {input}");

            State.JsonMessageMap[input.Key] = input.Message;

            return new Empty();
        }

        public override StringValue GetJsonMessage(StringValue input)
        {
            var message = State.JsonMessageMap[input.Value] ?? string.Empty;
            return new StringValue {Value = message};
        }
    }
}
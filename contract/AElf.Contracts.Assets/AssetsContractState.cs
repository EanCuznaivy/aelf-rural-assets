using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Assets
{
    public partial class AssetsContractState : ContractState
    {
        /// <summary>
        /// Id Card -> Asset Type -> Asset Info
        /// </summary>
        public MappedState<string, int, AssetInfo> AssetInfoMap { get; set; }

        /// <summary>
        /// Asset Id -> Asset
        /// </summary>
        public MappedState<int, Asset> AssetMap { get; set; }

        public MappedState<Address, bool> PermissionMap { get; set; }
    }
}
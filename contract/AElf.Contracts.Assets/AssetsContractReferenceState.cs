using AElf.Contracts.Consensus.AEDPoS;

namespace AElf.Contracts.Assets
{
    public partial class AssetsContractState
    {
        internal AEDPoSContractContainer.AEDPoSContractReferenceState AEDPoSContract { get; set; }
    }
}
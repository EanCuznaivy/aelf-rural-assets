using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.EconomicSystem
{
    public class AssetsSmartContractAddressNameProvider: ISmartContractAddressNameProvider, ISingletonDependency
    {
        public static Hash Name = HashHelper.ComputeFrom("AElf.ContractNames.Assets");

        public static readonly string StringName = Name.ToStorageKey();
        public Hash ContractName => Name;
        public string ContractStringName => StringName;
    }
}
using System.Collections.Generic;
using AElf.Contracts.Assets;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.EconomicSystem
{
    public class AssetsContractInitializationProvider : IContractInitializationProvider, ITransientDependency
    {
        public Hash SystemSmartContractName { get; } = AssetsSmartContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "AElf.Contracts.Assets";


        public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
        {
            return new List<ContractInitializationMethodCall>
            {
                new ContractInitializationMethodCall
                {
                    MethodName = nameof(AssetsContractContainer.AssetsContractStub.Initialize),
                    Params = ByteString.Empty
                }
            };
        }
    }
}
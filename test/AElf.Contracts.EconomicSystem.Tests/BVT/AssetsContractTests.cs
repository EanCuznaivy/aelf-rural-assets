using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Assets;
using Shouldly;
using Xunit;

namespace AElf.Contracts.EconomicSystem.Tests.BVT
{
    public partial class EconomicSystemTest
    {
        [Fact]
        public async Task AssetsContractTests()
        {
            await AssetsContractStub.SetAssetInfo.SendAsync(new AssetInfo
            {
                Name = "ean",
                IdCard = "1314",
                AssetType = 1,
                AssetIdList = { 123,456,789},
                AssetList = { new Asset
                {
                    AssetId = 456,
                    Status = "Active"
                }}
            });

            await AssetsContractStub.SetAsset.SendAsync(new Asset
            {
                AssetId = 123,
                Status = "Active"
            });
            
            await AssetsContractStub.SetAsset.SendAsync(new Asset
            {
                AssetId = 789,
                Status = "Frozen"
            });

            var assetInfo = await AssetsContractStub.GetAssetInfoWithDetails.CallAsync(new GetAssetInfoInput
            {
                IdCard = "1314",
                AssetType = 1
            });

            assetInfo.AssetList.Count.ShouldBe(3);
            assetInfo.AssetList.First().AssetId.ShouldBe(123);
        }
    }
}
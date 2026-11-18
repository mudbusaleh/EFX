using EFX.Core.Discovery;
using EFX.Sample.Models;
using Xunit;

namespace EFX.Tests;
public class ModelDiscoveryTests
{
    [Fact]
    public void DiscoversModelsFromSampleAssembly()
    {
        var models = new ModelDiscovery().Discover(typeof(User).Assembly);
        Assert.Contains(models, x => x.Name == "User");
        Assert.Contains(models, x => x.Name == "Product");
        Assert.Contains(models, x => x.Name == "Order");
    }
}

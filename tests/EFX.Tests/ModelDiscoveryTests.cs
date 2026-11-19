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

        Assert.Contains(models, model => model.Name == "User");
        Assert.Contains(models, model => model.Name == "Product");
        Assert.Contains(models, model => model.Name == "Order");
    }

    [Fact]
    public void NavigationPropertiesAreNotDiscoveredAsColumns()
    {
        var order = new ModelDiscovery().Discover(typeof(Order).Assembly).Single(model => model.Name == "Order");

        Assert.Contains(order.Properties, property => property.Name == "UserId");
        Assert.DoesNotContain(order.Properties, property => property.Name == "User");
        Assert.DoesNotContain(order.Properties, property => property.ClrType.Contains("User", StringComparison.Ordinal));
        Assert.Contains("User", order.EffectiveNavigationProperties);
    }

    [Fact]
    public void UnknownComplexPropertiesAreReportedInsteadOfSilentlyMappedAsColumns()
    {
        var contact = new ModelDiscovery().Discover(typeof(EFX.Tests.Models.Contact).Assembly)
            .Single(model => model.Name == "Contact");

        Assert.DoesNotContain(contact.Properties, property => property.Name == "Address");
        Assert.Contains("Address", contact.EffectiveUnsupportedProperties);
    }

    [Fact]
    public void DuplicateColumnMappingsAreReportedAsUnsupported()
    {
        var model = new ModelDiscovery().Discover(typeof(EFX.Tests.Models.DuplicateColumns).Assembly)
            .Single(model => model.Name == "DuplicateColumns");

        Assert.Contains(model.EffectiveUnsupportedProperties, item => item.Contains("Duplicate column mapping", StringComparison.Ordinal));
    }

    [Fact]
    public void ModelTableNamesUsePluralConvention()
    {
        var sampleModels = new ModelDiscovery().Discover(typeof(User).Assembly);
        var testModels = new ModelDiscovery().Discover(typeof(EFX.Tests.Models.Category).Assembly);

        Assert.Contains(sampleModels, model => model.Name == "User" && model.EffectiveTableName == "Users");
        Assert.Contains(testModels, model => model.Name == "Category" && model.EffectiveTableName == "Categories");
    }
}

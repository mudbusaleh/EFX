namespace EFX.Core.Metadata;

public sealed record ModelInfo(string Name, IReadOnlyList<ModelProperty> Properties);

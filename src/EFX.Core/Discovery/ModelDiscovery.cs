using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using EFX.Core.Metadata;

namespace EFX.Core.Discovery;

/// <summary>
/// Discovers model types and deliberately separates scalar columns from navigation properties.
/// Unknown complex properties are reported as unsupported instead of being silently discarded.
/// </summary>
public sealed class ModelDiscovery
{
    private static readonly HashSet<Type> KnownScalarTypes = new()
    {
        typeof(string), typeof(decimal), typeof(DateTime), typeof(DateTimeOffset),
        typeof(DateOnly), typeof(TimeOnly), typeof(TimeSpan), typeof(Guid), typeof(byte[]), typeof(char),
        typeof(bool), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
        typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double)
    };

    public IReadOnlyList<ModelInfo> Discover(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var modelTypes = GetLoadableTypes(assembly)
            .Where(IsModelType)
            .Distinct()
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();
        var knownModels = modelTypes.ToHashSet();
        var nullability = new NullabilityInfoContext();

        return modelTypes
            .Select(type => CreateModelInfo(type, nullability, knownModels))
            .ToArray();
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }

    private static bool IsModelType(Type type)
    {
        if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition || type.IsDefined(typeof(NotMappedAttribute)))
            return false;

        var modelNamespace = type.Namespace;
        if (modelNamespace is null)
            return false;

        return modelNamespace.Split('.').Contains("Models", StringComparer.OrdinalIgnoreCase)
            || type.IsDefined(typeof(TableAttribute));
    }

    private static ModelInfo CreateModelInfo(Type type, NullabilityInfoContext nullability, HashSet<Type> knownModels)
    {
        var candidateProperties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetIndexParameters().Length == 0
                && property.GetMethod?.IsPublic == true
                && property.SetMethod?.IsPublic == true
                && !property.IsDefined(typeof(NotMappedAttribute)))
            .ToArray();

        var scalarProperties = candidateProperties
            .Where(property => IsScalar(property.PropertyType))
            .Select(property => CreateProperty(type, property, nullability))
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToArray();

        var navigationProperties = candidateProperties
            .Where(property => !IsScalar(property.PropertyType) && IsNavigation(property.PropertyType, knownModels))
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var unsupportedProperties = candidateProperties
            .Where(property => !IsScalar(property.PropertyType) && !IsNavigation(property.PropertyType, knownModels))
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        foreach (var duplicateColumn in scalarProperties
            .GroupBy(property => property.EffectiveColumnName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1))
        {
            unsupportedProperties.Add($"Duplicate column mapping '{duplicateColumn.Key}'");
        }

        if (scalarProperties.Count(property => property.IsPrimaryKey) > 1)
            unsupportedProperties.Add("Composite keys (not supported by this EFX version)");

        var tableAttribute = type.GetCustomAttribute<TableAttribute>();
        if (!string.IsNullOrWhiteSpace(tableAttribute?.Schema))
            unsupportedProperties.Add("Schema-qualified table mapping ([Table(Schema=...)])");

        var declaredTableName = tableAttribute?.Name;
        var hasExplicitTableName = !string.IsNullOrWhiteSpace(declaredTableName);
        var tableName = hasExplicitTableName ? declaredTableName : Pluralize(type.Name);

        return new ModelInfo(
            type.Name,
            Array.AsReadOnly(scalarProperties),
            tableName,
            Array.AsReadOnly(navigationProperties),
            unsupportedProperties.AsReadOnly(),
            hasExplicitTableName);
    }

    private static ModelProperty CreateProperty(Type modelType, PropertyInfo property, NullabilityInfoContext nullability)
    {
        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
        var declaredColumnName = columnAttribute?.Name;
        var isKey = property.IsDefined(typeof(KeyAttribute))
            || string.Equals(property.Name, "Id", StringComparison.OrdinalIgnoreCase)
            || string.Equals(property.Name, $"{modelType.Name}Id", StringComparison.OrdinalIgnoreCase);
        var referenceNullability = !propertyType.IsValueType
            && nullability.Create(property).ReadState != NullabilityState.NotNull;
        var isNullable = Nullable.GetUnderlyingType(property.PropertyType) is not null || referenceNullability;

        if (property.IsDefined(typeof(RequiredAttribute)) || isKey)
            isNullable = false;

        return new ModelProperty(
            property.Name,
            propertyType.IsEnum ? "System.Enum" : propertyType.FullName ?? propertyType.Name,
            isNullable,
            columnAttribute?.TypeName,
            isKey,
            string.IsNullOrWhiteSpace(declaredColumnName) ? null : declaredColumnName);
    }

    private static bool IsScalar(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsEnum || KnownScalarTypes.Contains(type);
    }

    private static bool IsNavigation(Type propertyType, HashSet<Type> knownModels)
    {
        propertyType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if (knownModels.Contains(propertyType))
            return true;

        if (propertyType.IsArray)
            return propertyType.GetElementType() is { } elementType && knownModels.Contains(elementType);

        if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(string))
        {
            var enumerableType = propertyType.GetInterfaces()
                .Append(propertyType)
                .FirstOrDefault(candidate => candidate.IsGenericType
                    && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            return enumerableType is not null && knownModels.Contains(enumerableType.GetGenericArguments()[0]);
        }

        return false;
    }

    private static string Pluralize(string name)
    {
        if (name.EndsWith("y", StringComparison.OrdinalIgnoreCase)
            && name.Length > 1
            && !"aeiou".Contains(char.ToLowerInvariant(name[^2])))
            return name[..^1] + "ies";

        if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("x", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("z", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("ch", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
            return name + "es";

        return name + "s";
    }
}

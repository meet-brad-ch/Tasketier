using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tasketier.Core;

/// <summary>The one JSON configuration for the task file, the settings file and the CLI output.</summary>
public static class TaskJson
{
    /// <summary>Indented, camel case, enums as names, nulls left out.</summary>
    public static JsonSerializerOptions Options { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };
}

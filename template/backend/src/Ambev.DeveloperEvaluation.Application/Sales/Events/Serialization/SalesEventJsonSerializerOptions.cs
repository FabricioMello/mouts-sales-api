using System.Text.Json;

namespace Ambev.DeveloperEvaluation.Application.Sales.Events.Serialization;

public static class SalesEventJsonSerializerOptions
{
    public static JsonSerializerOptions Default { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}

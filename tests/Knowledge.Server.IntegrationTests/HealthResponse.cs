using System.Text.Json;

namespace Knowledge.Server.IntegrationTests;

public sealed record HealthResponse(string Status, IReadOnlyList<HealthCheckResponse> Checks);

public sealed record HealthCheckResponse(
    string Name,
    string Status,
    string? Description,
    IReadOnlyDictionary<string, JsonElement> Data);

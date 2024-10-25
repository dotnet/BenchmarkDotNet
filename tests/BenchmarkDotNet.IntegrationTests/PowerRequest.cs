namespace BenchmarkDotNet.IntegrationTests;

internal class PowerRequest(string requestType, string requesterType, string requesterName, string? reason)
{
    public string RequestType { get; } = requestType;
    public string RequesterType { get; } = requesterType;
    public string RequesterName { get; } = requesterName;
    public string? Reason { get; } = reason;
}

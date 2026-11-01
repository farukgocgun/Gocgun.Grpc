namespace Gocgun.Grpc;

internal sealed class RouteRegistration
{
    public required string ClientName { get; init; }

    public required string Method { get; init; }

    public required string ResponseFullName { get; init; }

    public required TimeSpan DefaultDeadline { get; init; }
}

namespace Gocgun.Grpc;

/// <summary>Service marker mapped by <c>MapGocgunGrpc</c>. It has no RPC of its own.</summary>
public sealed class GocgunGrpcMarker
{
    /// <summary>Gets the marker used when the package maps handlers.</summary>
    public string Name { get; } = "gocgun.grpc";
}

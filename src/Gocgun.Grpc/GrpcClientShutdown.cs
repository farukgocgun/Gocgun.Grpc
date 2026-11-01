namespace Gocgun.Grpc;

internal sealed class GrpcClientShutdown(GrpcClientRegistry registry) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => registry.ShutdownAsync();
}

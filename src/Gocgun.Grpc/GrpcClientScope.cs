namespace Gocgun.Grpc;

internal static class GrpcClientScope
{
    private static readonly AsyncLocal<string?> Current = new();

    public static string Name => Current.Value ?? string.Empty;

    public static IDisposable Push(string name)
    {
        var previous = Current.Value;
        Current.Value = name;
        return new Pop(previous);
    }

    private sealed class Pop(string? previous) : IDisposable
    {
        public void Dispose() => Current.Value = previous;
    }
}

using Microsoft.Extensions.Logging;

namespace Gocgun.Grpc.Tests;

/// <summary>Sends every category to one <see cref="CaptureLogger"/>.</summary>
internal sealed class CaptureLoggerProvider : ILoggerProvider
{
    /// <summary>Gets the shared logger.</summary>
    public CaptureLogger Logger { get; } = new();

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => Logger;

    /// <inheritdoc />
    public void Dispose()
    {
    }
}

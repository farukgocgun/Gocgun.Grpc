using Microsoft.Extensions.Logging;

namespace Gocgun.Grpc.Tests;

/// <summary>Collects formatted log lines, including exception text when a logger receives one.</summary>
internal sealed class CaptureLogger : ILogger
{
    /// <summary>Gets the formatted lines.</summary>
    internal List<string> Lines { get; } = [];

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull => NullScope.Instance;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Lines.Add(formatter(state, exception));
        if (exception is not null)
        {
            Lines.Add(exception.ToString());
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}

namespace Gocgun.Grpc;

/// <summary>Checks caller-supplied metadata before it goes on the wire.</summary>
internal static class GrpcHeaderRules
{
    private static readonly HashSet<string> Reserved = new(StringComparer.Ordinal)
    {
        "correlation-id",
        "causation-id",
        "traceparent",
        "tracestate",
        "user-agent",
        "te",
        "content-type",
        "authorization",
    };

    /// <summary>Throws when <paramref name="name"/> or <paramref name="value"/> may not be sent as caller metadata.</summary>
    /// <param name="name">Header name.</param>
    /// <param name="value">Header value.</param>
    /// <exception cref="ArgumentException">The header is reserved, binary, or not valid ASCII metadata.</exception>
    internal static void ThrowIfUnsafe(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);
        if (name.StartsWith(':') || name.StartsWith("grpc-", StringComparison.Ordinal) || Reserved.Contains(name))
        {
            throw new ArgumentException($"Header '{name}' is reserved and set by the library.", nameof(name));
        }

        if (name.EndsWith("-bin", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Header '{name}' is binary metadata, which is not supported.", nameof(name));
        }

        foreach (var character in name)
        {
            if (!(character is >= 'a' and <= 'z' || character is >= '0' and <= '9' || character is '-' or '_' or '.'))
            {
                throw new ArgumentException($"Header '{name}' must be lowercase ASCII letters, digits, '-', '_' or '.'.", nameof(name));
            }
        }

        if (value.Any(static character => character is < ' ' or > '~'))
        {
            throw new ArgumentException($"Header '{name}' value must be printable ASCII.", nameof(value));
        }
    }
}

namespace Gocgun.Grpc;

/// <summary>Per-call metadata. A null deadline uses the client default, which is required.</summary>
/// <param name="CorrelationId">Caller correlation id. Sent as metadata and on the response path.</param>
/// <param name="Deadline">Call deadline. Null uses <see cref="GrpcClientOptions.DefaultDeadline"/>.</param>
/// <param name="Headers">Extra metadata. Values must be ids, never payloads or secrets.</param>
public sealed record GrpcCallOptions(
    string? CorrelationId = null,
    TimeSpan? Deadline = null,
    IReadOnlyDictionary<string, string>? Headers = null);

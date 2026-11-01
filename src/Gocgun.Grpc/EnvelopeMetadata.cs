namespace Gocgun.Grpc;

/// <summary>Ids copied onto <c>ResponseEnvelope</c>. Not a payload.</summary>
/// <param name="MessageId">Message id.</param>
/// <param name="CorrelationId">Correlation id.</param>
/// <param name="CausationId">Causation id.</param>
/// <param name="TraceParent">W3C traceparent.</param>
/// <param name="SentAt">Send time.</param>
public sealed record EnvelopeMetadata(
    string MessageId,
    string? CorrelationId,
    string? CausationId,
    string? TraceParent,
    DateTimeOffset SentAt);

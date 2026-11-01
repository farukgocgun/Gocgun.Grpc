namespace Gocgun.Grpc;

/// <summary>Metadata for one server call. This package has no tenant.</summary>
/// <param name="MessageId">Server-generated id.</param>
/// <param name="CorrelationId">Correlation id from metadata, when the caller sent one.</param>
/// <param name="SentAt">Server clock when the handler started.</param>
/// <param name="Headers">Non-reserved metadata. Not logged.</param>
/// <param name="PeerCertificateSubject">Client certificate subject when mTLS presented one.</param>
public sealed record GrpcCallContext(
    string MessageId,
    string? CorrelationId,
    DateTimeOffset SentAt,
    IReadOnlyDictionary<string, string> Headers,
    string? PeerCertificateSubject);

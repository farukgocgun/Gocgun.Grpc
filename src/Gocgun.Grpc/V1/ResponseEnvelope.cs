using ProtoBuf;

namespace Gocgun.Grpc.V1;

/// <summary>
/// Every response body. No tenant field. Field numbers and types match the former <c>gocgun.grpc.v1.ResponseEnvelope</c>
/// proto3 message, so bytes from either side parse on the other. Unknown fields are kept through <see cref="Extensible"/>.
/// </summary>
[ProtoContract(Name = "gocgun.grpc.v1.ResponseEnvelope")]
[CompatibilityLevel(CompatibilityLevel.Level300)]
public sealed class ResponseEnvelope : Extensible
{
    /// <summary>Gets or sets the server message id.</summary>
    [ProtoMember(1, Name = "message_id")]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Gets or sets the correlation id. Empty when not set.</summary>
    [ProtoMember(2, Name = "correlation_id")]
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>Gets or sets the causation id. Empty when not set.</summary>
    [ProtoMember(3, Name = "causation_id")]
    public string CausationId { get; set; } = string.Empty;

    /// <summary>Gets or sets the W3C traceparent. Empty when not set.</summary>
    [ProtoMember(4, Name = "traceparent")]
    public string Traceparent { get; set; } = string.Empty;

    /// <summary>Gets or sets the sent time, UTC, written as <c>google.protobuf.Timestamp</c>.</summary>
    [ProtoMember(5, Name = "sent_at")]
    public DateTime? SentAt { get; set; }

    /// <summary>Gets or sets the success payload, written with the <c>google.protobuf.Any</c> layout.</summary>
    [ProtoMember(6, Name = "payload")]
    public ResponsePayload? Payload { get; set; }

    /// <summary>Gets or sets the domain error. Set instead of <see cref="Payload"/>.</summary>
    [ProtoMember(7, Name = "error")]
    public Error? Error { get; set; }
}

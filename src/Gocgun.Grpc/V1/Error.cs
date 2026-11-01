using ProtoBuf;

namespace Gocgun.Grpc.V1;

/// <summary>Domain error on the wire. Same field numbers as the former <c>gocgun.grpc.v1.Error</c> proto3 message.</summary>
[ProtoContract(Name = "gocgun.grpc.v1.Error")]
public sealed class Error
{
    /// <summary>Gets or sets the error type. <see cref="ErrorType.Unspecified"/> is invalid when the error is set.</summary>
    [ProtoMember(1, Name = "type")]
    public ErrorType Type { get; set; }

    /// <summary>Gets or sets the stable error code.</summary>
    [ProtoMember(2, Name = "code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the localization key. Never user text or personal data.</summary>
    [ProtoMember(3, Name = "message_key")]
    public string MessageKey { get; set; } = string.Empty;

    /// <summary>Gets the field errors.</summary>
    [ProtoMember(4, Name = "fields")]
    public IList<FieldError> Fields { get; } = new List<FieldError>();
}

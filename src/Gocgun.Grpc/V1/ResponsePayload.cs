using ProtoBuf;

namespace Gocgun.Grpc.V1;

/// <summary>
/// Typed payload with the same field layout as <c>google.protobuf.Any</c>: type URL, then the message bytes.
/// </summary>
[ProtoContract(Name = "gocgun.grpc.v1.ResponsePayload")]
public sealed class ResponsePayload
{
    /// <summary>Gets or sets the type URL, <c>type.googleapis.com/</c> plus the contract name.</summary>
    [ProtoMember(1, Name = "type_url")]
    public string TypeUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the serialized message.</summary>
    [ProtoMember(2, Name = "value")]
    public ReadOnlyMemory<byte> Value { get; set; }
}

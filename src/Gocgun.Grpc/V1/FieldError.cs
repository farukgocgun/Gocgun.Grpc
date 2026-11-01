using ProtoBuf;

namespace Gocgun.Grpc.V1;

/// <summary>Messages for one field. Same field numbers as the former <c>gocgun.grpc.v1.FieldError</c> proto3 message.</summary>
[ProtoContract(Name = "gocgun.grpc.v1.FieldError")]
public sealed class FieldError
{
    /// <summary>Gets or sets the field name.</summary>
    [ProtoMember(1, Name = "field")]
    public string Field { get; set; } = string.Empty;

    /// <summary>Gets the message keys for the field.</summary>
    [ProtoMember(2, Name = "messages")]
    public IList<string> Messages { get; } = new List<string>();
}

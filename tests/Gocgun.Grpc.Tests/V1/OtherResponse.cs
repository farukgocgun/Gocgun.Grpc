using ProtoBuf;

namespace Gocgun.Grpc.Tests.V1;

/// <summary>Test contract.</summary>
[ProtoContract(Name = "gocgun.grpc.tests.v1.OtherResponse")]
public sealed class OtherResponse
{
    /// <summary>Gets or sets the value.</summary>
    [ProtoMember(1, Name = "value")]
    public string Value { get; set; } = string.Empty;
}

using ProtoBuf;

namespace Gocgun.Grpc.Tests.V1;

/// <summary>Test contract.</summary>
[ProtoContract(Name = "gocgun.grpc.tests.v1.PingResponse")]
public sealed class PingResponse
{
    /// <summary>Gets or sets the greeting.</summary>
    [ProtoMember(1, Name = "greeting")]
    public string Greeting { get; set; } = string.Empty;
}

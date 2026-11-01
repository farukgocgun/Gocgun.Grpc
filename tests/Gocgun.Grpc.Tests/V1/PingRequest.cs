using ProtoBuf;

namespace Gocgun.Grpc.Tests.V1;

/// <summary>Test contract.</summary>
[ProtoContract(Name = "gocgun.grpc.tests.v1.PingRequest")]
public sealed class PingRequest
{
    /// <summary>Gets or sets the name.</summary>
    [ProtoMember(1, Name = "name")]
    public string Name { get; set; } = string.Empty;
}

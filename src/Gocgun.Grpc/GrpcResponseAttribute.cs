namespace Gocgun.Grpc;

/// <summary>
/// Names the payload a service contract method returns inside <c>ResponseEnvelope</c>.
/// Replaces the former <c>gocgun.grpc.v1.response</c> method option; <c>Route</c> checks it at startup.
/// </summary>
/// <param name="payload">protobuf-net contract of the success payload.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class GrpcResponseAttribute(Type payload) : Attribute
{
    /// <summary>Gets the success payload type.</summary>
    public Type Payload { get; } = payload;
}

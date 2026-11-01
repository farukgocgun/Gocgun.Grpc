using Gocgun.Grpc.V1;
using ProtoBuf.Grpc.Configuration;

namespace Gocgun.Grpc.Tests.V1;

/// <summary>Test service contract. <c>Wrong</c> declares a payload that does not match the routed response.</summary>
[Service("gocgun.grpc.tests.v1.Ping")]
public interface IPingService
{
    /// <summary>Returns a greeting.</summary>
    /// <param name="request">Request.</param>
    /// <returns>The response envelope.</returns>
    [Operation("Say")]
    [GrpcResponse(typeof(PingResponse))]
    ValueTask<ResponseEnvelope> SayAsync(PingRequest request);

    /// <summary>Declares <see cref="OtherResponse"/>.</summary>
    /// <param name="request">Request.</param>
    /// <returns>The response envelope.</returns>
    [GrpcResponse(typeof(OtherResponse))]
    ValueTask<ResponseEnvelope> WrongAsync(PingRequest request);

    /// <summary>Declares no payload.</summary>
    /// <param name="request">Request.</param>
    /// <returns>The response envelope.</returns>
    ValueTask<ResponseEnvelope> BareAsync(PingRequest request);
}

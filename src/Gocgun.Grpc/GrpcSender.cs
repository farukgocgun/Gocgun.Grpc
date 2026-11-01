using System.Diagnostics;
using Gocgun.Grpc.V1;
using Grpc.Core;

namespace Gocgun.Grpc;

internal sealed class GrpcSender(IServiceProvider services, GrpcClientRegistry clients, RouteTable routes, TimeProvider time) : IGrpcSender
{
    public async ValueTask<Results.Result<TResponse>> SendAsync<TRequest, TResponse>(
        TRequest request,
        GrpcCallOptions? options = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(request);
        var route = routes.Require<TRequest>();
        if (!string.Equals(route.ResponseFullName, GrpcContractNames.FullName(typeof(TResponse)), StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The registered response for {typeof(TRequest).FullName} is {route.ResponseFullName}.");
        }

        var deadline = options?.Deadline ?? route.DefaultDeadline;
        if (deadline <= TimeSpan.Zero)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "A deadline is required."));
        }

        var metadata = BuildMetadata(options);

        var slash = route.Method.IndexOf('/', StringComparison.Ordinal);
        var service = route.Method[..slash];
        var name = route.Method[(slash + 1)..];
        var method = new Method<TRequest, ResponseEnvelope>(
            MethodType.Unary,
            service,
            name,
            ProtoCodec.CreateMarshaller<TRequest>(),
            ProtoCodec.CreateMarshaller<ResponseEnvelope>());

        using (GrpcClientScope.Push(route.ClientName))
        {
            var invoker = clients.Invoker(route.ClientName, services);
            var call = invoker.AsyncUnaryCall(
                method,
                host: null,
                new CallOptions(metadata, time.GetUtcNow().UtcDateTime.Add(deadline), cancellationToken),
                request);
            var envelope = await call.ResponseAsync.ConfigureAwait(false);
            return ResponseEnvelopeMapper.ToResult<TResponse>(envelope);
        }
    }

    private static Metadata BuildMetadata(GrpcCallOptions? options)
    {
        var metadata = new Metadata();
        if (!string.IsNullOrEmpty(options?.CorrelationId))
        {
            metadata.Add("correlation-id", options.CorrelationId);
        }

        var traceParent = Activity.Current?.Id;
        if (!string.IsNullOrEmpty(traceParent))
        {
            metadata.Add("traceparent", traceParent);
        }

        if (options?.Headers is not null)
        {
            foreach (var header in options.Headers)
            {
                GrpcHeaderRules.ThrowIfUnsafe(header.Key, header.Value);
                metadata.Add(header.Key, header.Value);
            }
        }

        return metadata;
    }
}

using Gocgun.Grpc.V1;
using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gocgun.Grpc;

internal sealed class UnaryMethodBinder<TRequest, TResponse> : IServiceMethodProvider<GocgunGrpcMarker>
    where TRequest : class
    where TResponse : class
{
    private readonly string _method;
    private readonly GrpcServerOptions _serverOptions;
    private readonly TimeProvider _time;
    private readonly ILogger<UnaryMethodBinder<TRequest, TResponse>> _logger;

    public UnaryMethodBinder(
        string method,
        GrpcServerOptions serverOptions,
        TimeProvider time,
        ILogger<UnaryMethodBinder<TRequest, TResponse>> logger)
    {
        _method = method;
        _serverOptions = serverOptions;
        _time = time;
        _logger = logger;
    }

    public void OnServiceMethodDiscovery(ServiceMethodProviderContext<GocgunGrpcMarker> context)
    {
        var slash = _method.IndexOf('/', StringComparison.Ordinal);
        var grpcMethod = new Method<TRequest, ResponseEnvelope>(
            MethodType.Unary,
            _method[..slash],
            _method[(slash + 1)..],
            ProtoCodec.CreateMarshaller<TRequest>(),
            ProtoCodec.CreateMarshaller<ResponseEnvelope>());
        context.AddUnaryMethod(grpcMethod, Array.Empty<object>(), HandleAsync);
    }

    private async Task<ResponseEnvelope> HandleAsync(GocgunGrpcMarker service, TRequest request, ServerCallContext context)
    {
        if (context.Deadline == DateTime.MaxValue)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "A deadline is required."));
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
        if (_serverOptions.MaxDeadline is { } maxDeadline)
        {
            linked.CancelAfter(maxDeadline);
        }

        var handler = context.GetHttpContext().RequestServices.GetRequiredService<IGrpcHandler<TRequest, TResponse>>();
        var sentAt = _time.GetUtcNow();
        var callContext = new GrpcCallContext(
            Guid.NewGuid().ToString("N"),
            UnaryCallDetails.Header(context, "correlation-id"),
            sentAt,
            UnaryCallDetails.Headers(context),
            UnaryCallDetails.CertificateSubject(context));
        UnaryCallDetails.ActivityTags(callContext);
        try
        {
            var result = await handler.HandleAsync(request, callContext, linked.Token).ConfigureAwait(false);
            return ResponseEnvelopeMapper.FromResult(result, new EnvelopeMetadata(
                callContext.MessageId,
                callContext.CorrelationId,
                UnaryCallDetails.Header(context, "causation-id"),
                UnaryCallDetails.Header(context, "traceparent"),
                sentAt));
        }
        catch (RpcException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            // The call token also fires on a client cancel. Only a passed deadline (or the server cap) is a deadline failure.
            var clientCancelled = context.CancellationToken.IsCancellationRequested && _time.GetUtcNow().UtcDateTime < context.Deadline;
            var code = clientCancelled ? StatusCode.Cancelled : StatusCode.DeadlineExceeded;
            GrpcLog.Aborted(_logger, context.Method, code.ToString());
            throw new RpcException(new Status(code, clientCancelled ? "Call cancelled." : "Deadline exceeded."));
        }
        catch (Exception ex)
        {
            GrpcLog.Failed(_logger, context.Method, StatusCode.Internal.ToString(), ex.GetType().FullName ?? ex.GetType().Name);
            throw new RpcException(new Status(StatusCode.Internal, ResponseEnvelopeMapper.InternalDetail));
        }
    }
}

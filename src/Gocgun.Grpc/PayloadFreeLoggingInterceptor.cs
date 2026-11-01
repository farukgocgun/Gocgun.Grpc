using System.Diagnostics;
using Gocgun.Grpc.V1;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Gocgun.Grpc;

/// <summary>Logs method, client, status, duration, and message id. Payloads are not logged.</summary>
internal sealed class PayloadFreeLoggingInterceptor(ILogger<PayloadFreeLoggingInterceptor> logger) : Interceptor
{
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var started = Stopwatch.GetTimestamp();
        var call = continuation(request, context);
        return new AsyncUnaryCall<TResponse>(
            WatchAsync(call.ResponseAsync, context.Method.FullName, started),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            var response = await continuation(request, context).ConfigureAwait(false);
            var messageId = response is ResponseEnvelope envelope ? envelope.MessageId : string.Empty;
            GrpcLog.Completed(logger, context.Method, context.Peer, StatusCode.OK.ToString(), started, messageId);
            return response;
        }
        catch (RpcException ex)
        {
            GrpcLog.Completed(logger, context.Method, context.Peer, ex.StatusCode.ToString(), started, string.Empty);
            throw;
        }
    }

    private async Task<TResponse> WatchAsync<TResponse>(Task<TResponse> responseTask, string method, long started)
    {
        try
        {
            var response = await responseTask.ConfigureAwait(false);
            var messageId = response is ResponseEnvelope envelope ? envelope.MessageId : string.Empty;
            GrpcLog.Completed(logger, method, GrpcClientScope.Name, StatusCode.OK.ToString(), started, messageId);
            return response;
        }
        catch (RpcException ex)
        {
            GrpcLog.Completed(logger, method, GrpcClientScope.Name, ex.StatusCode.ToString(), started, string.Empty);
            throw;
        }
    }
}

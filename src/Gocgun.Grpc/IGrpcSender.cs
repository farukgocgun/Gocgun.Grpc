namespace Gocgun.Grpc;

/// <summary>Sends a unary call for the route registered for the request type.</summary>
public interface IGrpcSender
{
    /// <summary>Sends <paramref name="request"/> and unwraps the response envelope.</summary>
    /// <typeparam name="TRequest">Request contract.</typeparam>
    /// <typeparam name="TResponse">Payload named by the method's <see cref="GrpcResponseAttribute"/>.</typeparam>
    /// <param name="request">Request message.</param>
    /// <param name="options">Per-call deadline and metadata. Null uses the client defaults.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The payload on success, or a domain <see cref="Results.Error"/>.</returns>
    ValueTask<Results.Result<TResponse>> SendAsync<TRequest, TResponse>(
        TRequest request,
        GrpcCallOptions? options = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class;
}

namespace Gocgun.Grpc;

/// <summary>Handles one unary RPC. The package maps the result into <c>ResponseEnvelope</c>.</summary>
/// <typeparam name="TRequest">Request contract.</typeparam>
/// <typeparam name="TResponse">Success payload contract.</typeparam>
public interface IGrpcHandler<in TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    /// <summary>Handles <paramref name="request"/>.</summary>
    /// <param name="request">Request message.</param>
    /// <param name="context">Call metadata. No tenant.</param>
    /// <param name="cancellationToken">Cancels the handler. Includes the server maximum deadline.</param>
    /// <returns>A success payload or a domain error.</returns>
    ValueTask<Results.Result<TResponse>> HandleAsync(
        TRequest request,
        GrpcCallContext context,
        CancellationToken cancellationToken);
}

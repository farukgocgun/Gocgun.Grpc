using Gocgun.Grpc.Tests.V1;
using Gocgun.Results;

namespace Gocgun.Grpc.Tests;

/// <summary>Test handler. The name selects success, a domain error, or a thrown exception.</summary>
internal sealed class PingHandler : IGrpcHandler<PingRequest, PingResponse>
{
    /// <inheritdoc />
    public ValueTask<Result<PingResponse>> HandleAsync(PingRequest request, GrpcCallContext context, CancellationToken cancellationToken)
    {
        if (string.Equals(request.Name, "missing", StringComparison.Ordinal))
        {
            return new(Result<PingResponse>.Failure(Error.NotFound("missing", "not-found-key")));
        }

        if (string.Equals(request.Name, "boom", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("SECRET_EXCEPTION_TEXT");
        }

        if (string.Equals(request.Name, "slow", StringComparison.Ordinal))
        {
            return WaitAsync(cancellationToken);
        }

        return new(Result.Success(new PingResponse { Greeting = "hi " + request.Name }));
    }

    private static async ValueTask<Result<PingResponse>> WaitAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        return Result.Success(new PingResponse());
    }
}

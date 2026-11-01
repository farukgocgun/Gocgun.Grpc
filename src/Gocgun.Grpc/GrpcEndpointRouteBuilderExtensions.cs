using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Gocgun.Grpc;

/// <summary>Maps handlers registered with <see cref="GrpcBuilder.AddHandler{TRequest, TResponse, THandler}"/>.</summary>
public static class GrpcEndpointRouteBuilderExtensions
{
    /// <summary>Maps the package service when at least one handler was registered.</summary>
    /// <param name="endpoints">Endpoint route builder.</param>
    /// <returns>The same builder.</returns>
    public static IEndpointRouteBuilder MapGocgunGrpc(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var table = endpoints.ServiceProvider.GetService<RouteTable>();
        if (table is null)
        {
            throw new InvalidOperationException("Call AddGocgunGrpc before MapGocgunGrpc.");
        }

        if (table.ExposeServer)
        {
            endpoints.MapGrpcService<GocgunGrpcMarker>();
        }

        return endpoints;
    }
}

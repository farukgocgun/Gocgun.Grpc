using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Gocgun.Grpc;

/// <summary>Registers <see cref="IGrpcSender"/> and, when handlers exist, the gRPC server.</summary>
public static class GrpcServiceCollectionExtensions
{
    /// <summary>Adds the gRPC sender and the routes configured by <paramref name="configure"/>.</summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Clients, routes, and handlers.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddGocgunGrpc(this IServiceCollection services, Action<GrpcBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new GrpcBuilder(services);
        configure(builder);
        builder.Complete();
        services.TryAddSingleton(TimeProvider.System);
        if (!builder.HasHandlers)
        {
            return services;
        }

        services.AddGrpc(options =>
        {
            options.EnableDetailedErrors = false;
            options.MaxReceiveMessageSize = builder.ServerOptions.MaxReceiveMessageSize;
            options.MaxSendMessageSize = builder.ServerOptions.MaxSendMessageSize;
            options.Interceptors.Add<PayloadFreeLoggingInterceptor>();
            foreach (var interceptor in builder.ServerInterceptors)
            {
                options.Interceptors.Add(interceptor);
            }
        });
        return services;
    }
}

using Grpc.AspNetCore.Server.Model;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gocgun.Grpc;

/// <summary>Registers named clients, routes, and handlers.</summary>
public sealed class GrpcBuilder
{
    private readonly IServiceCollection _services;
    private readonly RouteTable _routes = new();
    private readonly Dictionary<string, GrpcClientOptions> _clients = new(StringComparer.Ordinal);
    private readonly List<Type> _serverInterceptors = [];
    private readonly List<Action<IServiceCollection>> _handlers = [];
    private readonly GrpcServerOptions _serverOptions = new();
    private readonly GrpcClientRegistry _registry = new();

    internal GrpcBuilder(IServiceCollection services)
    {
        _services = services;
    }

    internal RouteTable Routes => _routes;

    internal IReadOnlyList<Type> ServerInterceptors => _serverInterceptors;

    internal GrpcServerOptions ServerOptions => _serverOptions;

    internal bool HasHandlers => _handlers.Count > 0;

    /// <summary>Registers a named client and returns its HTTP builder so the host can add a resilience handler.</summary>
    /// <param name="name">Client name.</param>
    /// <param name="configure">Address, deadline, sizes, and contracts.</param>
    /// <returns>The HTTP client builder for this client.</returns>
    public IHttpClientBuilder AddClient(string name, Action<GrpcClientOptions> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configure);
        if (_clients.ContainsKey(name))
        {
            throw new InvalidOperationException($"gRPC client {name} is already registered.");
        }

        var options = new GrpcClientOptions();
        configure(options);
        if (options.Address is null)
        {
            throw new InvalidOperationException($"gRPC client {name} requires an address.");
        }

        if (options.DefaultDeadline is null || options.DefaultDeadline <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"gRPC client {name} requires a deadline.");
        }

        if (options.MaxReceiveMessageSize is null or <= 0 || options.MaxSendMessageSize is null or <= 0)
        {
            throw new InvalidOperationException($"gRPC client {name} requires maximum message sizes.");
        }

        _clients.Add(name, options);
        _registry.Add(name, options.Address, options.MaxReceiveMessageSize.Value, options.MaxSendMessageSize.Value);
        var http = _services.AddHttpClient(name);
        if (options.ClientCertificate is not null)
        {
            var certificate = options.ClientCertificate;
            http.ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new SocketsHttpHandler();
                handler.SslOptions.ClientCertificates = [certificate];
                return handler;
            });
        }

        return http;
    }

    /// <summary>Maps one request type to one client and method. The method's <see cref="GrpcResponseAttribute"/> must name <typeparamref name="TResponse"/>.</summary>
    /// <typeparam name="TRequest">Request contract.</typeparam>
    /// <typeparam name="TResponse">Payload named by <see cref="GrpcResponseAttribute"/>.</typeparam>
    /// <param name="clientName">Registered client.</param>
    /// <param name="method">Full method, <c>package.Service/Method</c>.</param>
    /// <returns>This builder.</returns>
    public GrpcBuilder Route<TRequest, TResponse>(string clientName, string method)
        where TRequest : class
        where TResponse : class
    {
        if (!_clients.TryGetValue(clientName, out var client))
        {
            throw new InvalidOperationException($"gRPC client {clientName} is not registered.");
        }

        _routes.Add<TRequest, TResponse>(clientName, method, client.DefaultDeadline!.Value, client.Contracts);
        return this;
    }

    /// <summary>Registers a handler for <paramref name="method"/>.</summary>
    /// <typeparam name="TRequest">Request contract.</typeparam>
    /// <typeparam name="TResponse">Success payload contract.</typeparam>
    /// <typeparam name="THandler">Handler implementation.</typeparam>
    /// <param name="method">Full method, <c>package.Service/Method</c>.</param>
    /// <returns>This builder.</returns>
    public GrpcBuilder AddHandler<TRequest, TResponse, THandler>(string method)
        where TRequest : class
        where TResponse : class
        where THandler : class, IGrpcHandler<TRequest, TResponse>
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        _ = GrpcContractNames.FullName(typeof(TRequest));
        _ = GrpcContractNames.FullName(typeof(TResponse));
        _services.AddScoped<THandler>();
        _services.AddScoped<IGrpcHandler<TRequest, TResponse>>(services => services.GetRequiredService<THandler>());
        _handlers.Add(services => services.AddSingleton<IServiceMethodProvider<GocgunGrpcMarker>>(provider =>
            new UnaryMethodBinder<TRequest, TResponse>(
                method,
                provider.GetRequiredService<GrpcServerOptions>(),
                provider.GetRequiredService<TimeProvider>(),
                provider.GetRequiredService<ILogger<UnaryMethodBinder<TRequest, TResponse>>>())));
        return this;
    }

    /// <summary>Adds a client interceptor resolved from dependency injection.</summary>
    /// <typeparam name="TInterceptor">Interceptor type.</typeparam>
    /// <returns>This builder.</returns>
    public GrpcBuilder AddClientInterceptor<TInterceptor>()
        where TInterceptor : Interceptor
    {
        _services.AddSingleton<TInterceptor>();
        _registry.AddInterceptor(typeof(TInterceptor));

        return this;
    }

    /// <summary>Adds a server interceptor resolved from dependency injection.</summary>
    /// <typeparam name="TInterceptor">Interceptor type.</typeparam>
    /// <returns>This builder.</returns>
    public GrpcBuilder AddServerInterceptor<TInterceptor>()
        where TInterceptor : Interceptor
    {
        _services.AddSingleton<TInterceptor>();
        _serverInterceptors.Add(typeof(TInterceptor));
        return this;
    }

    /// <summary>Sets server limits. Required when any handler is registered.</summary>
    /// <param name="configure">Server options.</param>
    /// <returns>This builder.</returns>
    public GrpcBuilder ConfigureServer(Action<GrpcServerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_serverOptions);
        return this;
    }

    internal void Complete()
    {
        if (HasHandlers
            && (_serverOptions.MaxReceiveMessageSize is null or <= 0
                || _serverOptions.MaxSendMessageSize is null or <= 0
                || _serverOptions.MaxDeadline is null
                || _serverOptions.MaxDeadline <= TimeSpan.Zero))
        {
            throw new InvalidOperationException("gRPC server handlers require a maximum deadline and maximum message sizes.");
        }

        _routes.ExposeServer = HasHandlers;
        _services.AddSingleton(_routes);
        _services.AddSingleton(_registry);
        _services.AddSingleton(_serverOptions);
        _services.AddSingleton<IGrpcSender, GrpcSender>();
        _services.AddSingleton<PayloadFreeLoggingInterceptor>();
        _services.AddSingleton<GrpcClientShutdown>();

        foreach (var register in _handlers)
        {
            register(_services);
        }
    }
}

using System.Collections.Concurrent;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Gocgun.Grpc;

internal sealed class GrpcClientRegistry
{
    private readonly ConcurrentDictionary<string, GrpcChannel> _channels = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ClientEndpoint> _entries = new(StringComparer.Ordinal);
    private readonly List<Type> _interceptors = [];
    private IServiceProvider? _services;

    public void Add(string name, Uri address, int maxReceiveMessageSize, int maxSendMessageSize) =>
        _entries.Add(name, new ClientEndpoint(address, maxReceiveMessageSize, maxSendMessageSize));

    public void AddInterceptor(Type interceptor) => _interceptors.Add(interceptor);

    public CallInvoker Invoker(string name, IServiceProvider services)
    {
        _services = services;
        var channel = _channels.GetOrAdd(name, Create);
        CallInvoker invoker = channel.CreateCallInvoker();
        foreach (var type in _interceptors)
        {
            invoker = invoker.Intercept((Interceptor)services.GetRequiredService(type));
        }

        return invoker.Intercept(services.GetRequiredService<PayloadFreeLoggingInterceptor>());
    }

    public async ValueTask ShutdownAsync()
    {
        foreach (var channel in _channels.Values)
        {
            await channel.ShutdownAsync().ConfigureAwait(false);
        }
    }

    private GrpcChannel Create(string name)
    {
        var entry = _entries[name];
        var http = _services!.GetRequiredService<IHttpClientFactory>().CreateClient(name);
        return GrpcChannel.ForAddress(entry.Address, new GrpcChannelOptions
        {
            HttpClient = http,
            DisposeHttpClient = false,
            MaxReceiveMessageSize = entry.MaxReceiveMessageSize,
            MaxSendMessageSize = entry.MaxSendMessageSize,
        });
    }

    private sealed record ClientEndpoint(Uri Address, int MaxReceiveMessageSize, int MaxSendMessageSize);
}

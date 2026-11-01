using Gocgun.Grpc.Tests.V1;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Gocgun.Grpc.Tests;

/// <summary>Startup checks for routes, service contracts, and the response payload attribute.</summary>
public sealed class RouteStartupTests
{
    /// <summary>A second route for the same request type fails.</summary>
    [Fact]
    public void Duplicate_route_fails()
    {
        var exception = Assert.Throws<InvalidOperationException>(RegisterSayTwice);
        Assert.Contains("already registered", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>The response payload attribute must name the routed response type.</summary>
    [Fact]
    public void Grpc_RouteResponseMismatchesOption_FailsAtStartup()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Add(static builder =>
            builder.Route<PingRequest, PingResponse>("ping", "gocgun.grpc.tests.v1.Ping/Wrong")));
        Assert.Contains("does not match", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>A method without <see cref="GrpcResponseAttribute"/> fails.</summary>
    [Fact]
    public void Method_without_response_payload_fails()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Add(static builder =>
            builder.Route<PingRequest, PingResponse>("ping", "gocgun.grpc.tests.v1.Ping/Bare")));
        Assert.Contains("does not declare a response payload", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>The operation name defaults to the method name without Async, like protobuf-net.Grpc.</summary>
    [Fact]
    public void Default_operation_name_drops_async_suffix()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Add(static builder =>
            builder.Route<PingRequest, PingResponse>("ping", "gocgun.grpc.tests.v1.Ping/WrongAsync")));
        Assert.Contains("not in the client contracts", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>A routed request type must be the method's request parameter.</summary>
    [Fact]
    public void Request_type_must_match_method()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Add(static builder =>
            builder.Route<OtherResponse, PingResponse>("ping", "gocgun.grpc.tests.v1.Ping/Say")));
        Assert.Contains("must take gocgun.grpc.tests.v1.OtherResponse", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>A contract type without [Service] fails.</summary>
    [Fact]
    public void Contract_without_service_name_fails()
    {
        var services = new ServiceCollection();
        var exception = Assert.Throws<InvalidOperationException>(() => services.AddGocgunGrpc(builder =>
        {
            builder.AddClient("ping", options =>
            {
                options.Address = new Uri("http://127.0.0.1:9");
                options.DefaultDeadline = TimeSpan.FromSeconds(1);
                options.MaxReceiveMessageSize = 1024;
                options.MaxSendMessageSize = 1024;
                options.Contracts.Add(typeof(IDisposable));
            });
            builder.Route<PingRequest, PingResponse>("ping", "gocgun.grpc.tests.v1.Ping/Say");
        }));
        Assert.Contains("[Service(", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>A message without a contract name fails at registration, so wire names never follow C# renames.</summary>
    [Fact]
    public void Contract_name_is_required()
    {
        var services = new ServiceCollection();
        var exception = Assert.Throws<InvalidOperationException>(() => services.AddGocgunGrpc(builder =>
            builder.AddHandler<UnnamedRequest, PingResponse, UnnamedHandler>("gocgun.grpc.tests.v1.Ping/Say")));
        Assert.Contains("stable contract name", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>An unknown method name fails.</summary>
    [Fact]
    public void Unmapped_method_fails_at_startup()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Add(static builder =>
            builder.Route<PingRequest, PingResponse>("ping", "gocgun.grpc.tests.v1.Ping/Missing")));
        Assert.Contains("not in the client contracts", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>SendAsync without a route fails.</summary>
    [Fact]
    public async Task Send_without_route_fails()
    {
        var services = new ServiceCollection();
        services.AddGocgunGrpc(builder => AddClient(builder));
        await using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<IGrpcSender>();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync<PingRequest, PingResponse>(new PingRequest { Name = "a" }, cancellationToken: TestContext.Current.CancellationToken).AsTask());
        Assert.Contains("No gRPC route", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>A non-positive per-call deadline is rejected before the call.</summary>
    [Fact]
    public async Task Zero_deadline_is_invalid_argument()
    {
        var services = new ServiceCollection();
        services.AddGocgunGrpc(builder =>
        {
            AddClient(builder);
            builder.Route<PingRequest, PingResponse>("ping", "gocgun.grpc.tests.v1.Ping/Say");
        });
        await using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<IGrpcSender>();
        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            sender.SendAsync<PingRequest, PingResponse>(new PingRequest { Name = "a" }, new GrpcCallOptions(Deadline: TimeSpan.Zero), TestContext.Current.CancellationToken).AsTask());
        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        Assert.Equal("A deadline is required.", exception.Status.Detail);
    }

    private static void RegisterSayTwice() => Add(static builder =>
    {
        builder.Route<PingRequest, PingResponse>("ping", "gocgun.grpc.tests.v1.Ping/Say");
        builder.Route<PingRequest, PingResponse>("ping", "gocgun.grpc.tests.v1.Ping/Say");
    });

    private static void Add(Action<GrpcBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddGocgunGrpc(builder =>
        {
            AddClient(builder);
            configure(builder);
        });
    }

    private static void AddClient(GrpcBuilder builder) =>
        builder.AddClient("ping", options =>
        {
            options.Address = new Uri("http://127.0.0.1:9");
            options.DefaultDeadline = TimeSpan.FromSeconds(1);
            options.MaxReceiveMessageSize = 1024;
            options.MaxSendMessageSize = 1024;
            options.Contracts.Add(typeof(IPingService));
        });

    /// <summary>Contract without a name.</summary>
    [ProtoBuf.ProtoContract]
    internal sealed class UnnamedRequest
    {
        /// <summary>Gets or sets a value.</summary>
        [ProtoBuf.ProtoMember(1)]
        public int Value { get; set; }
    }

    /// <summary>Handler for <see cref="UnnamedRequest"/>. Never called.</summary>
    internal sealed class UnnamedHandler : IGrpcHandler<UnnamedRequest, PingResponse>
    {
        /// <inheritdoc />
        public ValueTask<Results.Result<PingResponse>> HandleAsync(UnnamedRequest request, GrpcCallContext context, CancellationToken cancellationToken) =>
            new(Results.Result.Success(new PingResponse()));
    }
}

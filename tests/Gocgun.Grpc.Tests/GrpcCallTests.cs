using Gocgun.Grpc.Tests.V1;
using Gocgun.Grpc.V1;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gocgun.Grpc.Tests;

/// <summary>In-process unary calls: success, domain errors, deadline, size, and exception text.</summary>
public sealed class GrpcCallTests
{
    /// <summary>A handler success is a successful result.</summary>
    [Fact]
    public async Task Success_returns_item()
    {
        await using var pair = await Pair.StartAsync();
        var result = await pair.Sender.SendAsync<PingRequest, PingResponse>(new PingRequest { Name = "ada" }, new GrpcCallOptions(CorrelationId: "corr-1"), TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Equal("hi ada", result.Item?.Greeting);
        Assert.DoesNotContain(pair.Logs.Lines, static line => line.Contains("hi ada", StringComparison.Ordinal));
    }

    /// <summary>A domain error is status OK and a failed result.</summary>
    [Fact]
    public async Task Grpc_DomainError_ReturnsOkWithEnvelopeError()
    {
        await using var pair = await Pair.StartAsync();
        var result = await pair.Sender.SendAsync<PingRequest, PingResponse>(new PingRequest { Name = "missing" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.Equal("missing", result.Error?.Code);
        Assert.Equal(Gocgun.Results.ErrorType.NotFound, result.Error?.Type);
        Assert.Equal("not-found-key", result.Error?.Message);
    }

    /// <summary>An unhandled exception is Internal and its text is absent from the status and the logs.</summary>
    [Fact]
    public async Task Unhandled_exception_hides_message()
    {
        await using var pair = await Pair.StartAsync();
        var exception = await Assert.ThrowsAsync<RpcException>(() => pair.Sender.SendAsync<PingRequest, PingResponse>(new PingRequest { Name = "boom" }, cancellationToken: TestContext.Current.CancellationToken).AsTask());
        Assert.Equal(StatusCode.Internal, exception.StatusCode);
        Assert.Equal(ResponseEnvelopeMapper.InternalDetail, exception.Status.Detail);
        Assert.DoesNotContain("SECRET_EXCEPTION_TEXT", exception.Status.Detail, StringComparison.Ordinal);
        Assert.DoesNotContain(pair.Logs.Lines, static line => line.Contains("SECRET_EXCEPTION_TEXT", StringComparison.Ordinal));
    }

    /// <summary>A call without a deadline is rejected.</summary>
    [Fact]
    public async Task Grpc_CallWithoutDeadline_Rejected()
    {
        await using var pair = await Pair.StartAsync();
        using var channel = GrpcChannel.ForAddress(pair.Address);
        var method = new Method<PingRequest, ResponseEnvelope>(
            MethodType.Unary,
            "gocgun.grpc.tests.v1.Ping",
            "Say",
            Marshallers.Create(static (PingRequest message) => Wire.Serialize(message), static bytes => Wire.Deserialize<PingRequest>(bytes)),
            Marshallers.Create(static (ResponseEnvelope message) => Wire.Serialize(message), static bytes => Wire.Deserialize<ResponseEnvelope>(bytes)));
        var call = channel.CreateCallInvoker().AsyncUnaryCall(method, null, default(CallOptions), new PingRequest { Name = "ada" });
        var exception = await Assert.ThrowsAsync<RpcException>(() => call.ResponseAsync);
        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        Assert.DoesNotContain("ada", exception.Status.Detail, StringComparison.Ordinal);
    }

    /// <summary>A payload over the server limit is rejected and the body is not in the status.</summary>
    [Fact]
    public async Task Oversized_payload_is_rejected()
    {
        await using var pair = await Pair.StartAsync(maxReceive: 64);
        var canary = "CANARY_OVERSIZE_PAYLOAD_" + new string('x', 4000);
        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            pair.Sender.SendAsync<PingRequest, PingResponse>(new PingRequest { Name = canary }, cancellationToken: TestContext.Current.CancellationToken).AsTask());
        Assert.NotEqual(StatusCode.OK, exception.StatusCode);
        Assert.DoesNotContain("CANARY_OVERSIZE_PAYLOAD", exception.Status.Detail, StringComparison.Ordinal);
        Assert.DoesNotContain(pair.Logs.Lines, static line => line.Contains("CANARY_OVERSIZE_PAYLOAD", StringComparison.Ordinal));
    }

    /// <summary>Reserved, binary and malformed caller headers are rejected before the call.</summary>
    [Fact]
    public async Task Unsafe_caller_headers_are_rejected()
    {
        await using var pair = await Pair.StartAsync();
        foreach (var name in new[] { "traceparent", "correlation-id", "grpc-timeout", "x-data-bin", "Upper", ":path" })
        {
            var headers = new Dictionary<string, string>(StringComparer.Ordinal) { [name] = "v" };
            await Assert.ThrowsAsync<ArgumentException>(() =>
                pair.Sender.SendAsync<PingRequest, PingResponse>(new PingRequest { Name = "ada" }, new GrpcCallOptions(Headers: headers), TestContext.Current.CancellationToken).AsTask());
        }

        var bad = new Dictionary<string, string>(StringComparer.Ordinal) { ["x-ok"] = "line\r\nbreak" };
        await Assert.ThrowsAsync<ArgumentException>(() =>
            pair.Sender.SendAsync<PingRequest, PingResponse>(new PingRequest { Name = "ada" }, new GrpcCallOptions(Headers: bad), TestContext.Current.CancellationToken).AsTask());
        var good = new Dictionary<string, string>(StringComparer.Ordinal) { ["x-tenant-hint"] = "t-1" };
        var result = await pair.Sender.SendAsync<PingRequest, PingResponse>(new PingRequest { Name = "ada" }, new GrpcCallOptions(Headers: good), TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
    }

    /// <summary>The client deadline comes from the registered TimeProvider.</summary>
    [Fact]
    public async Task Deadline_uses_time_provider()
    {
        var past = new FixedTime(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero));
        await using var pair = await Pair.StartAsync(clientTime: past);
        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            pair.Sender.SendAsync<PingRequest, PingResponse>(new PingRequest { Name = "ada" }, cancellationToken: TestContext.Current.CancellationToken).AsTask());
        Assert.Equal(StatusCode.DeadlineExceeded, exception.StatusCode);
    }

    /// <summary>A client cancel is logged as Cancelled on the server, not as a deadline failure.</summary>
    [Fact]
    public async Task Client_cancel_is_not_a_deadline_failure()
    {
        await using var pair = await Pair.StartAsync();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var call = pair.Sender.SendAsync<PingRequest, PingResponse>(new PingRequest { Name = "slow" }, cancellationToken: cts.Token).AsTask();
        await Task.Delay(300, TestContext.Current.CancellationToken);
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<Exception>(() => call);
        var start = Environment.TickCount64;
        while (Environment.TickCount64 - start < 5000 && !pair.Logs.Lines.Exists(static line => line.Contains("ended early", StringComparison.Ordinal)))
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        Assert.True(pair.Logs.Lines.Exists(static line => line.Contains("ended early with status Cancelled", StringComparison.Ordinal)));
        Assert.False(pair.Logs.Lines.Exists(static line => line.Contains("ended early with status DeadlineExceeded", StringComparison.Ordinal)));
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class Pair : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly ServiceProvider _client;

        private Pair(WebApplication app, ServiceProvider client, string address, CaptureLogger logs)
        {
            _app = app;
            _client = client;
            Address = address;
            Logs = logs;
        }

        public string Address { get; }

        public CaptureLogger Logs { get; }

        public IGrpcSender Sender => _client.GetRequiredService<IGrpcSender>();

        public static async Task<Pair> StartAsync(int maxReceive = 1024 * 1024, TimeProvider? clientTime = null)
        {
            var logs = new CaptureLoggerProvider();
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.Logging.AddProvider(logs);
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(System.Net.IPAddress.Loopback, 0, listen => listen.Protocols = HttpProtocols.Http2);
            });
            builder.Services.AddGocgunGrpc(grpc =>
            {
                grpc.ConfigureServer(server =>
                {
                    server.MaxDeadline = TimeSpan.FromSeconds(2);
                    server.MaxReceiveMessageSize = maxReceive;
                    server.MaxSendMessageSize = 1024 * 1024;
                });
                grpc.AddHandler<PingRequest, PingResponse, PingHandler>("gocgun.grpc.tests.v1.Ping/Say");
            });
            var app = builder.Build();
            app.MapGocgunGrpc();
            await app.StartAsync();
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
            var services = new ServiceCollection();
            services.AddLogging(logging => logging.AddProvider(logs));
            if (clientTime is not null)
            {
                services.AddSingleton(clientTime);
            }

            services.AddGocgunGrpc(grpc =>
            {
                grpc.AddClient("ping", options =>
                {
                    options.Address = new Uri(address);
                    options.DefaultDeadline = TimeSpan.FromSeconds(2);
                    options.MaxReceiveMessageSize = 1024 * 1024;
                    options.MaxSendMessageSize = 1024 * 1024;
                    options.Contracts.Add(typeof(IPingService));
                });
                grpc.Route<PingRequest, PingResponse>("ping", "gocgun.grpc.tests.v1.Ping/Say");
            });
            var client = services.BuildServiceProvider();
            return new Pair(app, client, address, logs.Logger);
        }

        public async ValueTask DisposeAsync()
        {
            await _client.DisposeAsync();
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}

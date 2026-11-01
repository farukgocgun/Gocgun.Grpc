# Gocgun.Grpc

> **Beta.** This package is in beta: the public API may change between releases.
>
> It references `Gocgun.Results` through a sibling `ProjectReference`; the base package repository must sit next to this one to build. Publish order: `Gocgun.Results` first.

Client and server helpers for unary gRPC calls that return `gocgun.grpc.v1.ResponseEnvelope`.

- `IGrpcSender.SendAsync` returns `Result<TResponse>` from `Gocgun.Results`.
- A domain failure is gRPC status OK with `ResponseEnvelope.error`. Transport and identity failures stay `RpcException`.
- Contracts are protobuf-net C# classes with a required `[ProtoContract(Name = "package.v1.Message")]`. No `.proto` files and no code generation.
- A service contract is an interface marked `[Service("package.v1.Service")]` (protobuf-net.Grpc). Each method takes one request and returns `ValueTask<ResponseEnvelope>`; `[GrpcResponse(typeof(TPayload))]` names the payload. Add the interface to `GrpcClientOptions.Contracts`.
- `Route<TRequest, TResponse>` checks the method, its request type, and `[GrpcResponse]` at startup.
- The envelope keeps the proto3 wire layout of `gocgun.grpc.v1.ResponseEnvelope` (Timestamp `sent_at`, `Any` layout payload). Unknown fields are kept.
- Every call needs a deadline. The server rejects a missing deadline with `INVALID_ARGUMENT`.
- Logs carry method, client, status, duration, and message id. Payloads are not logged.
- An unhandled handler exception becomes gRPC `INTERNAL` with a fixed detail. The exception message is not in the status or the log.
- No tenant field and no tenant interceptor.

License: MIT (`LICENSE`).

## Publishing to nuget.org

Run `scripts/publish.sh` (or `scripts/publish.ps1` on Windows). It dispatches the manual `release.yml` workflow of each dependency first, in dependency order (Gocgun.Results, Gocgun.Grpc), then of this package, waiting for each run and stopping on the first failure. Requires the GitHub CLI signed in with `gh auth login`; the `nuget` environment and the nuget.org Trusted Publishing policy must exist in each repository.

using System.Reflection;
using Gocgun.Grpc.V1;
using ProtoBuf.Grpc.Configuration;

namespace Gocgun.Grpc;

internal sealed class RouteTable
{
    private const string AsyncSuffix = "Async";

    private readonly Dictionary<Type, RouteRegistration> _routes = [];

    public IReadOnlyDictionary<Type, RouteRegistration> Routes => _routes;

    public bool ExposeServer { get; set; }

    public void Add<TRequest, TResponse>(string clientName, string method, TimeSpan defaultDeadline, IEnumerable<Type> contracts)
        where TRequest : class
        where TResponse : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        if (_routes.ContainsKey(typeof(TRequest)))
        {
            throw new InvalidOperationException($"A gRPC route for {typeof(TRequest).FullName} is already registered.");
        }

        var requestName = GrpcContractNames.FullName(typeof(TRequest));
        var actual = GrpcContractNames.FullName(typeof(TResponse));
        var operation = FindMethod(contracts, method);
        var parameters = operation.GetParameters();
        if (parameters.Length == 0 || parameters[0].ParameterType != typeof(TRequest) || !ReturnsEnvelope(operation))
        {
            throw new InvalidOperationException(
                $"gRPC method {method} must take {requestName} and return {nameof(ResponseEnvelope)}.");
        }

        var response = operation.GetCustomAttribute<GrpcResponseAttribute>(inherit: false)
            ?? throw new InvalidOperationException($"gRPC method {method} does not declare a response payload.");
        var declared = GrpcContractNames.FullName(response.Payload);
        if (!string.Equals(declared, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"gRPC method {method} response payload is '{declared}', which does not match {actual}.");
        }

        _routes.Add(typeof(TRequest), new RouteRegistration
        {
            ClientName = clientName,
            Method = method,
            ResponseFullName = actual,
            DefaultDeadline = defaultDeadline,
        });
    }

    public RouteRegistration Require<TRequest>()
    {
        if (_routes.TryGetValue(typeof(TRequest), out var route))
        {
            return route;
        }

        throw new InvalidOperationException($"No gRPC route is registered for {typeof(TRequest).FullName}.");
    }

    private static MethodInfo FindMethod(IEnumerable<Type> contracts, string method)
    {
        foreach (var contract in contracts)
        {
            var service = ServiceName(contract);
            var found = contract.GetMethods().Concat(contract.GetInterfaces().SelectMany(static parent => parent.GetMethods()))
                .FirstOrDefault(candidate => string.Equals($"{service}/{OperationName(candidate)}", method, StringComparison.Ordinal));
            if (found is not null)
            {
                return found;
            }
        }

        throw new InvalidOperationException($"gRPC method {method} is not in the client contracts.");
    }

    private static string ServiceName(Type contract)
    {
        var name = contract.GetCustomAttribute<ServiceAttribute>(inherit: false)?.Name;
        if (!contract.IsInterface || string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(
                $"gRPC contract {contract.FullName} must be an interface marked [Service(\"package.v1.Service\")].");
        }

        return name;
    }

    // Same default as protobuf-net.Grpc: [Operation] name, else the method name without the Async suffix.
    private static string OperationName(MethodInfo method)
    {
        var name = method.GetCustomAttribute<OperationAttribute>(inherit: false)?.Name;
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return method.Name.EndsWith(AsyncSuffix, StringComparison.Ordinal) ? method.Name[..^AsyncSuffix.Length] : method.Name;
    }

    private static bool ReturnsEnvelope(MethodInfo method) =>
        method.ReturnType == typeof(Task<ResponseEnvelope>) || method.ReturnType == typeof(ValueTask<ResponseEnvelope>);
}

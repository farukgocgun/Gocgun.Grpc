using System.Reflection;
using ProtoBuf;

namespace Gocgun.Grpc;

/// <summary>
/// Contract names of protobuf-net messages. The name is <see cref="ProtoContractAttribute.Name"/>,
/// so renaming or moving the C# type does not change the type URL or the route check.
/// </summary>
public static class GrpcContractNames
{
    /// <summary>Type URL prefix, the same one <c>google.protobuf.Any</c> uses.</summary>
    public const string TypeUrlPrefix = "type.googleapis.com/";

    /// <summary>Gets the full contract name of <paramref name="type"/>.</summary>
    /// <param name="type">Type marked with <see cref="ProtoContractAttribute"/> and a non-empty name.</param>
    /// <returns>The full name without a leading dot, for example <c>crocovity.tools.v1.Reservation</c>.</returns>
    /// <exception cref="InvalidOperationException">The type has no contract or no contract name.</exception>
    public static string FullName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var contract = type.GetCustomAttribute<ProtoContractAttribute>(inherit: false)
            ?? throw new InvalidOperationException($"gRPC message {type.FullName} must be marked with [ProtoContract].");
        var name = contract.Name?.TrimStart('.');
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(
                $"gRPC message {type.FullName} needs a stable contract name: [ProtoContract(Name = \"package.v1.Message\")].");
        }

        return name;
    }

    /// <summary>Gets the type URL of <paramref name="type"/>.</summary>
    /// <param name="type">Type marked with <see cref="ProtoContractAttribute"/> and a non-empty name.</param>
    /// <returns><see cref="TypeUrlPrefix"/> plus the full contract name.</returns>
    public static string TypeUrl(Type type) => TypeUrlPrefix + FullName(type);
}

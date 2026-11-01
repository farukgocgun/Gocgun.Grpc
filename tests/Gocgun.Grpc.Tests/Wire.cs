using ProtoBuf;

namespace Gocgun.Grpc.Tests;

/// <summary>protobuf-net helpers for tests that write raw bytes.</summary>
internal static class Wire
{
    /// <summary>Serializes a contract.</summary>
    /// <typeparam name="T">Contract type.</typeparam>
    /// <param name="value">Value.</param>
    /// <returns>Protobuf bytes.</returns>
    internal static byte[] Serialize<T>(T value)
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, value);
        return stream.ToArray();
    }

    /// <summary>Parses a contract.</summary>
    /// <typeparam name="T">Contract type.</typeparam>
    /// <param name="bytes">Protobuf bytes.</param>
    /// <returns>The value.</returns>
    internal static T Deserialize<T>(byte[] bytes) => Serializer.Deserialize<T>(new ReadOnlyMemory<byte>(bytes));
}

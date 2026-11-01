using System.Buffers;
using Grpc.Core;
using ProtoBuf;

namespace Gocgun.Grpc;

/// <summary>protobuf-net reads and writes. Malformed bytes always surface as <see cref="InvalidDataException"/>.</summary>
internal static class ProtoCodec
{
    public static byte[] Serialize<T>(T value)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value);
        var writer = new ArrayBufferWriter<byte>();
        Serializer.Serialize(writer, value);
        return writer.WrittenSpan.ToArray();
    }

    public static T Deserialize<T>(ReadOnlyMemory<byte> bytes)
        where T : class
    {
        try
        {
            return Serializer.Deserialize<T>(bytes);
        }
        catch (Exception ex) when (ex is ProtoException or EndOfStreamException or OverflowException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            throw new InvalidDataException("The bytes are not a valid protobuf message.", ex);
        }
    }

    public static Marshaller<T> CreateMarshaller<T>()
        where T : class =>
        Marshallers.Create(static (T message) => Serialize(message), static bytes => Deserialize<T>(bytes));
}

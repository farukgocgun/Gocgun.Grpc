using Gocgun.Grpc.V1;
using Grpc.Core;

namespace Gocgun.Grpc;

/// <summary>Packs and unpacks <c>gocgun.grpc.v1.ResponseEnvelope</c> against a known response type.</summary>
public static class ResponseEnvelopeMapper
{
    /// <summary>Fixed gRPC detail for an unhandled exception. The exception text is never copied here.</summary>
    public const string InternalDetail = "Internal error.";

    /// <summary>Fixed gRPC detail when the payload type is not the registered response.</summary>
    public const string UnexpectedPayloadDetail = "Unexpected response payload type.";

    /// <summary>Fixed gRPC detail when the envelope is not a single success or domain error.</summary>
    public const string InvalidEnvelopeDetail = "Response envelope is invalid.";

    /// <summary>Builds an envelope from a handler result.</summary>
    /// <typeparam name="TResponse">Success payload contract.</typeparam>
    /// <param name="result">Handler result.</param>
    /// <param name="metadata">Ids for the envelope. Not a payload.</param>
    /// <returns>The envelope.</returns>
    public static ResponseEnvelope FromResult<TResponse>(Results.Result<TResponse> result, EnvelopeMetadata metadata)
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(metadata);
        var envelope = new ResponseEnvelope
        {
            MessageId = metadata.MessageId,
            CorrelationId = metadata.CorrelationId ?? string.Empty,
            CausationId = metadata.CausationId ?? string.Empty,
            Traceparent = metadata.TraceParent ?? string.Empty,
            SentAt = metadata.SentAt.UtcDateTime,
        };

        if (result.IsSuccess)
        {
            if (result.Item is null)
            {
                throw new RpcException(new Status(StatusCode.Internal, InternalDetail));
            }

            envelope.Payload = new ResponsePayload
            {
                TypeUrl = GrpcContractNames.TypeUrl(typeof(TResponse)),
                Value = ProtoCodec.Serialize(result.Item),
            };
            return envelope;
        }

        envelope.Error = ToWire(result.Error!);
        return envelope;
    }

    /// <summary>Unwraps an envelope into a result. An unexpected payload type is <see cref="StatusCode.Internal"/>.</summary>
    /// <typeparam name="TResponse">Registered response type.</typeparam>
    /// <param name="envelope">Response envelope.</param>
    /// <returns>The payload or a domain error.</returns>
    public static Results.Result<TResponse> ToResult<TResponse>(ResponseEnvelope envelope)
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(envelope);
        var hasPayload = envelope.Payload is not null && !string.IsNullOrEmpty(envelope.Payload.TypeUrl);
        var hasError = envelope.Error is not null && envelope.Error.Type != V1.ErrorType.Unspecified;
        if (hasPayload == hasError)
        {
            throw new RpcException(new Status(StatusCode.Internal, InvalidEnvelopeDetail));
        }

        if (hasError)
        {
            return Results.Result.Failure<TResponse>(ToError(envelope.Error!));
        }

        var payload = envelope.Payload;
        if (payload is null || !string.Equals(TypeName(payload.TypeUrl), GrpcContractNames.FullName(typeof(TResponse)), StringComparison.Ordinal))
        {
            throw new RpcException(new Status(StatusCode.Internal, UnexpectedPayloadDetail));
        }

        try
        {
            return Results.Result.Success(ProtoCodec.Deserialize<TResponse>(payload.Value));
        }
        catch (InvalidDataException)
        {
            throw new RpcException(new Status(StatusCode.Internal, UnexpectedPayloadDetail));
        }
    }

    // Same rule as google.protobuf.Any: the type name is everything after the last '/'.
    private static string TypeName(string typeUrl) => typeUrl[(typeUrl.LastIndexOf('/') + 1)..];

    private static V1.Error ToWire(Results.Error error)
    {
        var wire = new V1.Error
        {
            Type = ToWireType(error.Type),
            Code = error.Code,
            MessageKey = error.Message,
        };
        if (error.Details is null)
        {
            return wire;
        }

        foreach (var pair in error.Details)
        {
            var field = new FieldError { Field = pair.Key };
            foreach (var message in pair.Value)
            {
                field.Messages.Add(message);
            }

            wire.Fields.Add(field);
        }

        return wire;
    }

    private static Results.Error ToError(V1.Error error)
    {
        if (error.Type == V1.ErrorType.Unspecified || string.IsNullOrWhiteSpace(error.Code) || string.IsNullOrWhiteSpace(error.MessageKey))
        {
            throw new RpcException(new Status(StatusCode.Internal, InvalidEnvelopeDetail));
        }

        Dictionary<string, string[]>? details = null;
        if (error.Fields.Count > 0)
        {
            details = new Dictionary<string, string[]>(error.Fields.Count, StringComparer.Ordinal);
            foreach (var field in error.Fields)
            {
                details[field.Field] = [.. field.Messages];
            }
        }

        try
        {
            return new Results.Error(error.Code, error.MessageKey, ToResultType(error.Type), details);
        }
        catch (ArgumentException)
        {
            throw new RpcException(new Status(StatusCode.Internal, InvalidEnvelopeDetail));
        }
    }

    private static V1.ErrorType ToWireType(Results.ErrorType type) => type switch
    {
        Results.ErrorType.Validation => V1.ErrorType.Validation,
        Results.ErrorType.Unauthorized => V1.ErrorType.Unauthorized,
        Results.ErrorType.Forbidden => V1.ErrorType.Forbidden,
        Results.ErrorType.NotFound => V1.ErrorType.NotFound,
        Results.ErrorType.PaymentRequired => V1.ErrorType.PaymentRequired,
        Results.ErrorType.Conflict => V1.ErrorType.Conflict,
        Results.ErrorType.Concurrency => V1.ErrorType.Concurrency,
        Results.ErrorType.BusinessRule => V1.ErrorType.BusinessRule,
        Results.ErrorType.UnsupportedMediaType => V1.ErrorType.UnsupportedMediaType,
        Results.ErrorType.RateLimited => V1.ErrorType.RateLimited,
        Results.ErrorType.ServiceUnavailable => V1.ErrorType.ServiceUnavailable,
        _ => throw new RpcException(new Status(StatusCode.Internal, InvalidEnvelopeDetail)),
    };

    private static Results.ErrorType ToResultType(V1.ErrorType type) => type switch
    {
        V1.ErrorType.Validation => Results.ErrorType.Validation,
        V1.ErrorType.Unauthorized => Results.ErrorType.Unauthorized,
        V1.ErrorType.Forbidden => Results.ErrorType.Forbidden,
        V1.ErrorType.NotFound => Results.ErrorType.NotFound,
        V1.ErrorType.PaymentRequired => Results.ErrorType.PaymentRequired,
        V1.ErrorType.Conflict => Results.ErrorType.Conflict,
        V1.ErrorType.Concurrency => Results.ErrorType.Concurrency,
        V1.ErrorType.BusinessRule => Results.ErrorType.BusinessRule,
        V1.ErrorType.UnsupportedMediaType => Results.ErrorType.UnsupportedMediaType,
        V1.ErrorType.RateLimited => Results.ErrorType.RateLimited,
        V1.ErrorType.ServiceUnavailable => Results.ErrorType.ServiceUnavailable,
        _ => throw new RpcException(new Status(StatusCode.Internal, InvalidEnvelopeDetail)),
    };
}

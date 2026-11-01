using Gocgun.Grpc.Tests.V1;
using Gocgun.Results;
using Grpc.Core;

namespace Gocgun.Grpc.Tests;

/// <summary>Locks envelope packing for success, domain errors, hostile payloads, and the proto3 wire layout.</summary>
public sealed class ResponseEnvelopeMapperTests
{
    /// <summary>A success payload round-trips to Result.Item.</summary>
    [Fact]
    public void Success_round_trips()
    {
        var metadata = new EnvelopeMetadata("m1", "c1", "cause", "00-trace", new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero));
        var envelope = ResponseEnvelopeMapper.FromResult(Result.Success(new PingResponse { Greeting = "hi" }), metadata);
        var result = ResponseEnvelopeMapper.ToResult<PingResponse>(envelope);
        Assert.True(result.IsSuccess);
        Assert.Equal("hi", result.Item!.Greeting);
        Assert.Equal("m1", envelope.MessageId);
        Assert.Equal("c1", envelope.CorrelationId);
    }

    /// <summary>A domain error stays on the envelope and comes back as a failed result.</summary>
    [Fact]
    public void Domain_error_round_trips()
    {
        var error = Error.Validation("name.required", "name-key", new Dictionary<string, string[]>(StringComparer.Ordinal) { ["name"] = ["missing"] });
        var envelope = ResponseEnvelopeMapper.FromResult(Result<PingResponse>.Failure(error), Metadata());
        var result = ResponseEnvelopeMapper.ToResult<PingResponse>(envelope);
        Assert.True(result.IsFailure);
        Assert.Equal("name.required", result.Error!.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("name-key", result.Error.Message);
        Assert.Equal("missing", result.Error.Details!["name"][0]);
    }

    /// <summary>A payload of a different message is rejected without returning that payload.</summary>
    [Fact]
    public void Unexpected_any_type_is_internal()
    {
        var envelope = new Gocgun.Grpc.V1.ResponseEnvelope
        {
            Payload = new Gocgun.Grpc.V1.ResponsePayload
            {
                TypeUrl = GrpcContractNames.TypeUrl(typeof(OtherResponse)),
                Value = Wire.Serialize(new OtherResponse { Value = "secret-payload-canary" }),
            },
        };
        var exception = Assert.Throws<RpcException>(() => ResponseEnvelopeMapper.ToResult<PingResponse>(envelope));
        Assert.Equal(StatusCode.Internal, exception.StatusCode);
        Assert.Equal(ResponseEnvelopeMapper.UnexpectedPayloadDetail, exception.Status.Detail);
        Assert.DoesNotContain("secret-payload-canary", exception.Status.Detail, StringComparison.Ordinal);
    }

    /// <summary>Payload and error together are invalid.</summary>
    [Fact]
    public void Payload_and_error_together_are_internal()
    {
        var envelope = ResponseEnvelopeMapper.FromResult(Result.Success(new PingResponse { Greeting = "hi" }), Metadata());
        envelope.Error = new Gocgun.Grpc.V1.Error { Type = Gocgun.Grpc.V1.ErrorType.NotFound, Code = "x", MessageKey = "y" };
        var exception = Assert.Throws<RpcException>(() => ResponseEnvelopeMapper.ToResult<PingResponse>(envelope));
        Assert.Equal(StatusCode.Internal, exception.StatusCode);
        Assert.Equal(ResponseEnvelopeMapper.InvalidEnvelopeDetail, exception.Status.Detail);
    }

    /// <summary>An empty envelope is invalid.</summary>
    [Fact]
    public void Empty_envelope_is_internal()
    {
        var exception = Assert.Throws<RpcException>(() => ResponseEnvelopeMapper.ToResult<PingResponse>(new Gocgun.Grpc.V1.ResponseEnvelope()));
        Assert.Equal(ResponseEnvelopeMapper.InvalidEnvelopeDetail, exception.Status.Detail);
    }

    /// <summary>Error type unspecified is invalid when an error is present.</summary>
    [Fact]
    public void Unspecified_error_type_is_internal()
    {
        var envelope = new Gocgun.Grpc.V1.ResponseEnvelope
        {
            Error = new Gocgun.Grpc.V1.Error { Type = Gocgun.Grpc.V1.ErrorType.Unspecified, Code = "x", MessageKey = "y" },
        };
        var exception = Assert.Throws<RpcException>(() => ResponseEnvelopeMapper.ToResult<PingResponse>(envelope));
        Assert.Equal(ResponseEnvelopeMapper.InvalidEnvelopeDetail, exception.Status.Detail);
    }

    /// <summary>The payload type URL is the contract name with the Any prefix.</summary>
    [Fact]
    public void Payload_type_url_is_contract_name()
    {
        var envelope = ResponseEnvelopeMapper.FromResult(Result.Success(new PingResponse { Greeting = "hi" }), Metadata());
        Assert.Equal("type.googleapis.com/gocgun.grpc.tests.v1.PingResponse", envelope.Payload?.TypeUrl);
        Assert.Equal("gocgun.grpc.tests.v1.PingResponse", GrpcContractNames.FullName(typeof(PingResponse)));
    }

    /// <summary>Corrupt payload bytes are Internal, never a protobuf-net exception.</summary>
    [Fact]
    public void Corrupt_payload_is_internal()
    {
        byte[] truncated = [0x0A, 0x05, 0x01];
        var envelope = new Gocgun.Grpc.V1.ResponseEnvelope
        {
            Payload = new Gocgun.Grpc.V1.ResponsePayload
            {
                TypeUrl = GrpcContractNames.TypeUrl(typeof(PingResponse)),
                Value = truncated,
            },
        };
        var exception = Assert.Throws<RpcException>(() => ResponseEnvelopeMapper.ToResult<PingResponse>(envelope));
        Assert.Equal(ResponseEnvelopeMapper.UnexpectedPayloadDetail, exception.Status.Detail);
    }

    /// <summary>
    /// Bytes written by the former generated proto3 <c>ResponseEnvelope</c> still parse: field numbers, wire types,
    /// Timestamp for <c>sent_at</c>, the Any layout for the payload, and the nested error with field errors.
    /// </summary>
    [Fact]
    public void Wire_layout_matches_proto3_envelope()
    {
        byte[] payload =
        [
            0x0A, 0x01, (byte)'m', // 1: message_id
            0x2A, 0x02, 0x08, 0x01, // 5: sent_at { seconds: 1 }
            0x32, 0x07, 0x0A, 0x01, (byte)'t', 0x12, 0x02, 0x08, 0x01, // 6: payload { type_url, value }
        ];
        var parsed = Wire.Deserialize<Gocgun.Grpc.V1.ResponseEnvelope>(payload);
        Assert.Equal("m", parsed.MessageId);
        Assert.Equal(DateTime.UnixEpoch.AddSeconds(1), parsed.SentAt);
        Assert.Equal("t", parsed.Payload?.TypeUrl);
        byte[] value = [0x08, 0x01];
        Assert.Equal(value, parsed.Payload?.Value.ToArray());

        byte[] error =
        [
            0x3A, 0x10, // 7: error
            0x08, 0x04, // type: ERROR_TYPE_NOT_FOUND
            0x12, 0x01, (byte)'c', // code
            0x1A, 0x01, (byte)'k', // message_key
            0x22, 0x06, 0x0A, 0x01, (byte)'f', 0x12, 0x01, (byte)'x', // fields { field, messages }
        ];
        var failed = ResponseEnvelopeMapper.ToResult<PingResponse>(Wire.Deserialize<Gocgun.Grpc.V1.ResponseEnvelope>(error));
        Assert.True(failed.IsFailure);
        Assert.Equal(ErrorType.NotFound, failed.Error!.Type);
        Assert.Equal("c", failed.Error.Code);
        Assert.Equal("k", failed.Error.Message);
        Assert.Equal("x", failed.Error.Details!["f"][0]);
    }

    /// <summary>Unknown envelope fields survive a parse and write.</summary>
    [Fact]
    public void Unknown_fields_are_preserved()
    {
        var known = Wire.Serialize(new Gocgun.Grpc.V1.ResponseEnvelope { MessageId = "m" });
        byte[] extra = [.. known, 0xA0, 0x01, 0x01];
        var parsed = Wire.Deserialize<Gocgun.Grpc.V1.ResponseEnvelope>(extra);
        Assert.Equal("m", parsed.MessageId);
        Assert.True(Wire.Serialize(parsed).Length > known.Length);
    }

    /// <summary>A non-.NET consumer can export the envelope schema from the C# contract.</summary>
    [Fact]
    public void Envelope_schema_exports_from_contract()
    {
        var schema = ProtoBuf.Serializer.GetProto<Gocgun.Grpc.V1.ResponseEnvelope>();
        Assert.Contains("ResponseEnvelope", schema, StringComparison.Ordinal);
        Assert.Contains("Timestamp sent_at = 5;", schema, StringComparison.Ordinal);
    }

    /// <summary>The package has no Google.Protobuf or generated-code dependency.</summary>
    [Fact]
    public void Package_does_not_reference_google_protobuf()
    {
        var names = typeof(ResponseEnvelopeMapper).Assembly.GetReferencedAssemblies().Select(static name => name.Name);
        Assert.DoesNotContain(names, static name => string.Equals(name, "Google.Protobuf", StringComparison.Ordinal));
    }

    private static EnvelopeMetadata Metadata() =>
        new("m", null, null, null, DateTimeOffset.UnixEpoch);
}

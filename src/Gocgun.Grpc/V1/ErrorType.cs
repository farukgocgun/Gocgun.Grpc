using ProtoBuf;

namespace Gocgun.Grpc.V1;

/// <summary>Error type on the wire. Values match the former <c>gocgun.grpc.v1.ErrorType</c> proto3 enum.</summary>
[ProtoContract(Name = "gocgun.grpc.v1.ErrorType")]
public enum ErrorType
{
    /// <summary>Not set. Invalid when an error is present.</summary>
    [ProtoEnum(Name = "ERROR_TYPE_UNSPECIFIED")]
    Unspecified = 0,

    /// <summary>Validation failure.</summary>
    [ProtoEnum(Name = "ERROR_TYPE_VALIDATION")]
    Validation = 1,

    /// <summary>Caller is not authenticated.</summary>
    [ProtoEnum(Name = "ERROR_TYPE_UNAUTHORIZED")]
    Unauthorized = 2,

    /// <summary>Caller is not allowed.</summary>
    [ProtoEnum(Name = "ERROR_TYPE_FORBIDDEN")]
    Forbidden = 3,

    /// <summary>Resource not found.</summary>
    [ProtoEnum(Name = "ERROR_TYPE_NOT_FOUND")]
    NotFound = 4,

    /// <summary>Payment or credits required.</summary>
    [ProtoEnum(Name = "ERROR_TYPE_PAYMENT_REQUIRED")]
    PaymentRequired = 5,

    /// <summary>State conflict.</summary>
    [ProtoEnum(Name = "ERROR_TYPE_CONFLICT")]
    Conflict = 6,

    /// <summary>Concurrency conflict.</summary>
    [ProtoEnum(Name = "ERROR_TYPE_CONCURRENCY")]
    Concurrency = 7,

    /// <summary>Business rule failure.</summary>
    [ProtoEnum(Name = "ERROR_TYPE_BUSINESS_RULE")]
    BusinessRule = 8,

    /// <summary>Unsupported media type.</summary>
    [ProtoEnum(Name = "ERROR_TYPE_UNSUPPORTED_MEDIA_TYPE")]
    UnsupportedMediaType = 9,

    /// <summary>Rate limited.</summary>
    [ProtoEnum(Name = "ERROR_TYPE_RATE_LIMITED")]
    RateLimited = 10,

    /// <summary>Dependency unavailable.</summary>
    [ProtoEnum(Name = "ERROR_TYPE_SERVICE_UNAVAILABLE")]
    ServiceUnavailable = 11,
}

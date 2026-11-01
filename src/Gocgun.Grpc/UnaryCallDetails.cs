using Grpc.Core;

namespace Gocgun.Grpc;

internal static class UnaryCallDetails
{
    public static void ActivityTags(GrpcCallContext callContext)
    {
        var activity = System.Diagnostics.Activity.Current;
        activity?.SetTag("message_id", callContext.MessageId);
        if (callContext.CorrelationId is not null)
        {
            activity?.SetTag("correlation_id", callContext.CorrelationId);
        }
    }

    public static string? Header(ServerCallContext context, string name)
    {
        var entry = context.RequestHeaders.Get(name);
        return string.IsNullOrEmpty(entry?.Value) ? null : entry.Value;
    }

    public static Dictionary<string, string> Headers(ServerCallContext context)
    {
        var headers = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in context.RequestHeaders)
        {
            if (entry.Key is "correlation-id" or "causation-id" or "traceparent")
            {
                continue;
            }

            if (entry.Value is not null && !entry.Key.StartsWith(':'))
            {
                headers[entry.Key] = entry.Value;
            }
        }

        return headers;
    }

    public static string? CertificateSubject(ServerCallContext context)
    {
        var certificate = context.GetHttpContext().Connection.ClientCertificate;
        return certificate is null ? null : certificate.Subject;
    }
}

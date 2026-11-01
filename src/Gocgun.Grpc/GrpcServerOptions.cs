namespace Gocgun.Grpc;

/// <summary>Server limits. Production leaves detailed errors off.</summary>
public sealed class GrpcServerOptions
{
    /// <summary>Gets or sets the işleyici süre üst sınırı. İstemci sınırı daha uzun olsa da işleyici bu sürede kesilir.</summary>
    public TimeSpan? MaxDeadline { get; set; }

    /// <summary>Gets or sets the alınan mesaj üst sınırı, bayt. Sınırsız ayar kabul edilmez.</summary>
    public int? MaxReceiveMessageSize { get; set; }

    /// <summary>Gets or sets the gönderilen mesaj üst sınırı, bayt. Sınırsız ayar kabul edilmez.</summary>
    public int? MaxSendMessageSize { get; set; }
}

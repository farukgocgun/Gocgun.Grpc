using System.Security.Cryptography.X509Certificates;

namespace Gocgun.Grpc;

/// <summary>Named client settings. The host loads certificates; this type only holds the reference.</summary>
public sealed class GrpcClientOptions
{
    /// <summary>Gets or sets the hedef adres. Şema https veya şifresiz HTTP/2 için http olabilir.</summary>
    public Uri? Address { get; set; }

    /// <summary>Gets or sets the mTLS istemci sertifikası. Paket sır saklamaz; host doldurur.</summary>
    public X509Certificate2? ClientCertificate { get; set; }

    /// <summary>Gets or sets the varsayılan süre sınırı. Boş, sıfır veya negatif olamaz.</summary>
    public TimeSpan? DefaultDeadline { get; set; }

    /// <summary>Gets or sets the alınan mesaj üst sınırı, bayt. Sınırsız ayar kabul edilmez.</summary>
    public int? MaxReceiveMessageSize { get; set; }

    /// <summary>Gets or sets the gönderilen mesaj üst sınırı, bayt. Sınırsız ayar kabul edilmez.</summary>
    public int? MaxSendMessageSize { get; set; }

    /// <summary>Gets the servis sözleşmeleri: <c>[Service]</c> işaretli arayüzler. Route kontrolü bunlara bakar.</summary>
    public IList<Type> Contracts { get; } = [];
}

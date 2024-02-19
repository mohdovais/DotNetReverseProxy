using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
public class HttpClientService
{

    public SocketsHttpHandler GetSocketsHttpHandler(X509Certificate2? cert)
    {
        var handler = new SocketsHttpHandler()
        {
            AllowAutoRedirect = false,
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1), // default
            PooledConnectionLifetime = TimeSpan.FromMinutes(30),
            ConnectTimeout = TimeSpan.FromMinutes(1),

        };

        if (cert != null)
        {
            var certIndex = handler.SslOptions.ClientCertificates?.Add(cert);

            if (certIndex != 0)
            {
                handler.SslOptions.LocalCertificateSelectionCallback = (
                    object sender,
                    string targetHost,
                    X509CertificateCollection localCertificates,
                    X509Certificate? remoteCertificate,
                    string[] acceptableIssuers) => cert;
            }
        }

        handler.SslOptions.RemoteCertificateValidationCallback = RemoteCertificateValidationCallback;

        return handler;
    }

    private static bool RemoteCertificateValidationCallback(
        object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslErrors)
    {

        Console.WriteLine($"Effective date: {certificate?.GetEffectiveDateString()}");
        Console.WriteLine($"Exp date: {certificate?.GetExpirationDateString()}");
        Console.WriteLine($"Issuer: {certificate?.Issuer}");
        Console.WriteLine($"Subject: {certificate?.Subject}");

        return sslErrors == SslPolicyErrors.None;
    }
}

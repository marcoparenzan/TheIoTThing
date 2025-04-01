using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

// Generate CA certificate
var caCertificate = GenerateCertificate("CN=MyCA", isCa: true);
Console.WriteLine("CA Certificate Thumbprint: " + caCertificate.Thumbprint);

// Generate device certificate signed by the CA
var deviceCertificate = GenerateCertificate("CN=MyDevice", isCa: false, caCertificate);
Console.WriteLine("Device Certificate Thumbprint: " + deviceCertificate.Thumbprint);

X509Certificate2 GenerateCertificate(string subjectName, bool isCa, X509Certificate2 issuerCertificate = null)
{
    using (RSA rsa = RSA.Create(2048))
    {
        var request = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        if (isCa)
        {
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        }
        else
        {
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        }

        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        if (issuerCertificate == null)
        {
            // Self-signed CA certificate
            return request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
        }
        else
        {
            // Device certificate signed by the CA
            return request.Create(issuerCertificate, DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1), Guid.NewGuid().ToByteArray());
        }
    }
}

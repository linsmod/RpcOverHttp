using Pluralsight.Crypto;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    internal static class CertificateHandler
    {
        public static void RemoveCertificateBinding(int port)
        {
            var command = string.Format("http delete sslcert ipport=0.0.0.0:{0}", port);
            ExecuteNetshCommand(command);
        }

        public static bool IsServantCertificateInstalled()
        {
            var certificates = GetCertificates();
            return certificates.Any(x => x.Name == "EzHttp");
        }

        public static void InstallServantCertificate()
        {
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);

            //CRASH!
            // Servant certifikatet kan ikke bindes til Azure serveren, ved mindre det bliver eksporteret og importeret først. Den siger det der med local user blablal.. 

            X509Certificate2 cert;
            using (var ctx = new CryptContext())
            {
                ctx.Open();
                cert = ctx.CreateSelfSignedCertificate(
                    new SelfSignedCertProperties
                    {
                        IsPrivateKeyExportable = true,
                        KeyBitLength = 4096,
                        Name = new X500DistinguishedName("CN=\"EzHttp\"; C=\"EzHttp\"; O=\"EzHttp\"; OU=\"EzHttp\";"),
                        ValidFrom = DateTime.Today,
                        ValidTo = DateTime.Today.AddYears(10)
                    });
                //ensure pfx in cert.
                byte[] pfx = cert.Export(X509ContentType.Pfx);
                byte[] pkbytes = cert.Export(X509ContentType.Cert);
                System.IO.File.WriteAllBytes(".\\ezhttp.cer", pkbytes);
                cert = new X509Certificate2(pfx, (string)null, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);
            }
            cert.FriendlyName = "EzHttp";
            store.Add(cert);
            store.Close();
            System.Threading.Thread.Sleep(1000); // Wait for certificate to be installed
        }

        internal static void ExportPkFile(Certificate cert)
        {
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            var sCert = store.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, false);
            if (sCert.Count == 1)
            {
                var bytes = sCert[0].Export(X509ContentType.Cert);
                System.IO.File.WriteAllBytes(".\\ezhttp.cer", bytes);
            }
            store.Close();
        }

        public static void AddCertificateBinding(int port)
        {
            var certificateHash = GetServantCertHash();
            var command = "http add sslcert ipport=0.0.0.0:" + port + " certhash=" + certificateHash + " appid={53DD655A-24B4-4DF5-B077-C4217610472E}";
            var cmdOutput = ExecuteNetshCommand(command);
        }

        public static bool IsCertificateBound(int port)
        {
            var command = string.Format("http show sslcert ipport=0.0.0.0:{0}", port);
            var cmdOutput = ExecuteNetshCommand(command);
            return !cmdOutput.Contains("The system cannot find the file specified.") && !cmdOutput.Contains("系统找不到指定的文件");
        }

        private static string GetServantCertHash()
        {
            var certificate = GetCertificates().SingleOrDefault(x => x.Name == "EzHttp");
            if (certificate == null)
                return null;

            return certificate.Thumbprint;
        }
        internal static IEnumerable<Certificate> GetCertificates()
        {
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.OpenExistingOnly);
            var certs = store.Certificates.Cast<X509Certificate2>().ToList();

            foreach (var cert in certs)
            {
                var name = cert.FriendlyName;
                if (string.IsNullOrWhiteSpace(name)) // Extracts common name if friendly name isn't available.
                {
                    var commonName = cert.Subject.Split(',').SingleOrDefault(x => x.StartsWith("CN"));
                    if (commonName != null)
                    {
                        var locationOfEquals = commonName.IndexOf('=');
                        name = commonName.Substring(locationOfEquals + 1, commonName.Length - (locationOfEquals + 1));
                    }
                }

                yield return new Certificate { Name = name, Hash = cert.GetCertHash(), Thumbprint = cert.Thumbprint };
            }
        }

        private static string ExecuteNetshCommand(string command)
        {
            var p = new Process
            {
                StartInfo =
                {
                    FileName = "netsh.exe",
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                }
            };
            p.Start();
            var output = p.StandardOutput.ReadToEnd();
            return output;
        }
    }
}

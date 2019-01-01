using RpcOverHttp.Serialization;
using RpcOverHttp.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TinyIoC;

namespace RpcOverHttp
{
    /// <summary>
    /// for server administration using built-in rpc service
    /// </summary>
    public interface IRpcServer
    {
        void Stop();
        void Register<T>() where T : class;
        void Register<T>(T instance) where T : class;
        void Register<T, TImplementation>() where T : class where TImplementation : class, T;
        void Register<T, TImplementation>(TImplementation instance) where T : class where TImplementation : class, T;
    }
    public class RpcServer : IocContainerWrapper, IRpcServer
    {
        public RpcServer() : base(new TinyIoCContainer())
        {
            iocContainer.Register<IRpcDataSerializer, ProtoBufRpcDataSerializer>(new ProtoBufRpcDataSerializer(), "default");
            iocContainer.Register<IRpcHeadSerializer, JsonRpcHeadSerializer>(new JsonRpcHeadSerializer(), "default");
            iocContainer.Register(this, "default");
            iocContainer.Register<IRpcServiceAdministration>(new RpcServiceAdministration());
            iocContainer.Register<IExceptionHandler>(new DefaultExceptionHandler(), "default");
            iocContainer.Register<IAuthroizeHandler>(new DefaultAuthroizeHandler(), "default");
        }

        RpcHttpListener listener;
        public void Start(string urlPrefix)
        {
            listener = new RpcHttpListener(this);
            var uri = new Uri(urlPrefix);
            if (uri.Scheme == "https")
            {
                EnsureCertBindingInstalled(uri.Port);
            }
            listener.Start(urlPrefix);
            Console.WriteLine($"start {urlPrefix}");
        }

        public void Stop()
        {
            listener.Stop();
        }

        private void EnsureCertBindingInstalled(int port)
        {
            var cert = CertificateHandler.GetCertificates().FirstOrDefault(x => x.Name == "EzHttp");
            if (cert == null)
                CertificateHandler.InstallServantCertificate();
            else
            {
                CertificateHandler.ExportPkFile(cert);
            }
            if (!CertificateHandler.IsCertificateBound(port))
            {
                CertificateHandler.AddCertificateBinding(port);
            }
        }
    }
}

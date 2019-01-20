using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RpcOverHttp;
using static RpcOverHttp.Serialization.ProtoBufRpcDataSerializer;

namespace RpcServiceCollection
{

    public class RpcServiceSample : RpcService, IRpcServiceSample
    {
        public RpcServiceSample()
        {
        }

        public event Func<string, int> SampleFuncEvent;
        public event EventHandler SimpleEvent;
        public event EventHandler<string> SampleActionEvent;

        public override RpcIdentity Authroize(string token)
        {
            return base.Authroize(token);
        }

        public void TestRemoteEventHandler()
        {
            if (SimpleEvent != null)
            {
                SimpleEvent(this, EventArgs.Empty);
                Console.WriteLine("test SimpleEvent remote handle ok");
            }
            if (SampleFuncEvent != null)
            {
                var num = SampleFuncEvent.Invoke("hello world");
                Console.WriteLine("test SampleFuncEvent remote handle ok, return " + num);
            }
            if (SampleActionEvent != null)
            {
                SampleActionEvent(this, "hello client.");
                Console.WriteLine("test SampleActionEvent remote handle ok");
            }
        }

        public string GetUserName()
        {
            return this.User.Identity.Name;
        }

        public bool IsUserAuthenticated()
        {

            return this.User.Identity.IsAuthenticated;
        }

        public void TestAccessOk() { }

        [Authorize]
        public void TestAccessDeniend() { }

        public Task TestRemoteTask()
        {
            return Task.Delay(5000);
        }

        public Task<string> TestRemoteTaskWithResult()
        {
            return Task.Delay(5000)
                .ContinueWith(x => ("remote task completed."));
        }

        public async Task<string> TestRemoteAsyncTaskWithResult()
        {
            return await new StringReader("abc").ReadLineAsync();
        }

        public void UploadStream(Stream stream, string fileName)
        {
            var fullPath = Path.Combine(Path.GetTempPath(), fileName);
            var fs = File.Open(fullPath, FileMode.Create);
            stream.CopyTo(fs);
            fs.Dispose();
        }

        public Stream DownloadStream(string fileName)
        {
            var fullPath = Path.Combine(Path.GetTempPath(), fileName);
            var fs = new TempFileStream(fullPath, FileMode.Open);
            return fs;
        }
    }
}

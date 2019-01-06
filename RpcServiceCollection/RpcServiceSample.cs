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
        public event Func<string, int> TestEventHandlerWithReturn;
        public event EventHandler TestEventHandler;
        public event EventHandler<object> TestEventHandlerGeneric;

        public override RpcIdentity Authroize(string token)
        {
            return base.Authroize(token);
        }

        public string GetUserName()
        {
            if (TestEventHandlerWithReturn != null)
            {
                var num = TestEventHandlerWithReturn.Invoke("hello world");
                Console.WriteLine("TestEventHandlerWithReturn remote handle ok");
            }
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

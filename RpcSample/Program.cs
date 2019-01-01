using RpcOverHttp;
using RpcServiceCollection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RpcSample
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var client = RpcClient.Initialize("https://localhost:8970/", "../../../RpcHost/bin/Debug/RpcOverHttp.cer");
                var sample = client.Rpc<IRpcServiceSample>();
                var username = sample.GetUserName();
                Debug.Assert(username.Equals("Anonymous"));
                Console.WriteLine("GetUserName ok");
                var isAuthenticated = sample.IsUserAuthenticated();
                Debug.Assert(isAuthenticated.Equals(false));
                Console.WriteLine("IsUserAuthenticated ok");
                sample.TestAccessOk();
                Console.WriteLine("TestAccessOk ok");
                try
                {
                    sample.TestAccessDeniend();
                }
                catch (Exception ex)
                {
                    Debug.Assert(ex.GetType().Equals(typeof(RpcException)));
                    Console.WriteLine("TestAccessDeniend ok");
                }
                sample.TestRemoteTask().Wait();
                var x = sample.TestRemoteTaskWithResult().Result;
                Debug.Assert(x.Equals("remote task completed."));
                Console.WriteLine("TestRemoteTaskWithResult ok");
                Task.Run(async () =>
                {
                    var y = await sample.TestRemoteAsyncTaskWithResult();
                    Debug.Assert(y.Equals("abc"));
                    Console.WriteLine("TestRemoteAsyncTaskWithResult ok");
                });
                var fsup = File.Open("testupload.bin", FileMode.Create);
                int lines = 100;
                var sw = new StreamWriter(fsup);
                while (lines-- > 0)
                {
                    sw.WriteLine(string.Concat(Enumerable.Range(0, 10000)));
                }
                fsup.Position = 0;
                Console.WriteLine("uploading a file, size=" + new ByteSize(fsup.Length));
                sample.UploadStream(fsup, "testfile.temp");
                Console.WriteLine("UploadStream ok");
                Console.WriteLine("downloading the file...");
                var ms = sample.DownloadStream("testfile.temp");
                Console.WriteLine("DownloadStream ok");
                var fsdown = File.Open("testdownload.bin", FileMode.Create, FileAccess.Write);
                ms.CopyTo(fsdown);
                Debug.Assert(fsup.Length.Equals(fsdown.Length));
                Console.WriteLine("UploadStream.Length is equal to DownloadStream.length? ok");
                fsup.Close();
                fsdown.Close();
                ms.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            Console.ReadLine();
        }
    }
}

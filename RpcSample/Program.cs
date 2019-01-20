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
        private static ThreadLocal<int> vp = new ThreadLocal<int>();
        private static int Sample_SampleFuncEvent(string arg)
        {
            Thread.Sleep(100);
            if (!vp.IsValueCreated)
            {
                vp.Value = new Random(DateTime.Now.Millisecond).Next(1, 1000);
            }
            Console.WriteLine("ThreadId:" + Thread.CurrentThread.ManagedThreadId + ", return " + vp.Value);
            return vp.Value;
            //throw new NotImplementedException();
        }

        private static void Sample_SampleActionEvent(object sender, string message)
        {
            IRpcServiceSample sample = sender as IRpcServiceSample;
            Console.WriteLine("received server SampleActionEvent:message=" + message);
        }

        private static void Sample_SimpleEvent(object sender, EventArgs e)
        {
            Console.WriteLine("received server SimpleEvent");
        }
        static void Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 1024;
            for (int i = 0; i < 10; i++)
            {
                new Thread((state) =>
                {
                    try
                    {
                        var idx = (int)state;
                        Thread.Sleep((idx + 1) * 150);
                        var client = RpcClient.Initialize("http://127.0.0.1:8970/");
                        //var client = RpcClient.Initialize("https://localhost:8443/", "../../../RpcHost/bin/Debug/RpcOverHttp.cer");
                        client.ServerCertificateValidationCallback = (a, b, c, d) => true;
                        var sample = client.Rpc<IRpcServiceSample>();

                        //remote event handler test
                        sample.SampleFuncEvent += Sample_SampleFuncEvent;
                        sample.SampleActionEvent += Sample_SampleActionEvent;
                        sample.SimpleEvent -= Sample_SimpleEvent;
                        sample.TestRemoteEventHandler();

                        //simple call test
                        var username = sample.GetUserName();
                        Debug.Assert(username.Equals("Anonymous"));
                        Console.WriteLine("GetUserName ok");

                        //authroize test
                        var isAuthenticated = sample.IsUserAuthenticated();
                        Debug.Assert(isAuthenticated.Equals(false));
                        Console.WriteLine("IsUserAuthenticated ok");

                        //authroize test 2
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

                        //task call test
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

                        //stream request and response test
                        var fsup = File.Open(string.Format("testupload{0}.bin", idx), FileMode.Create);
                        int lines = 100;
                        var sw = new StreamWriter(fsup);
                        while (lines-- > 0)
                        {
                            sw.WriteLine(string.Concat(Enumerable.Range(0, 10000)));
                        }
                        fsup.Position = 0;
                        Console.WriteLine("uploading a file, size=" + new ByteSize(fsup.Length));
                        sample.UploadStream(fsup, string.Format("testfile{0}.temp", idx));
                        Console.WriteLine("UploadStream ok");
                        Console.WriteLine("downloading the file...");
                        var ms = sample.DownloadStream(string.Format("testfile{0}.temp", idx));
                        Console.WriteLine("DownloadStream ok");
                        var fsdown = File.Open(string.Format("testdownload{0}.bin", idx), FileMode.Create, FileAccess.Write);
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
                })
                { IsBackground = true }.Start(i);
            }

            Console.ReadLine();
        }


    }
}

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RpcOverHttp;

namespace RpcServiceCollection
{

    public class RpcServiceSample : RpcService, IRpcServiceSample
    {
        public RpcServiceSample()
        {
        }

        public override RpcIdentity Authroize(string token)
        {
            return base.Authroize(token);
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
            var fs = File.Open(fileName, FileMode.Create);
            stream.CopyTo(fs);
            fs.Dispose();
        }

        public Stream DownloadStream(string fileName)
        {
            var fs = File.Open(fileName, FileMode.Open);
            return fs;
        }
    }
}
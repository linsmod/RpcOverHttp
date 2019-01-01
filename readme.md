### RpcOverHttp
a .NET interface based rpc framework, process command using http protocol under the hood.
the interface/implementation and its method is like the asp.net mvc controller+action style but more simple to client use.


## How to use?
### 1)define the interface in MyInterface.dll
```
public interface IRpcServiceSample
    {
        string GetUserName();
        bool IsUserAuthenticated();
        void TestAccessDeniend();
        void TestAccessOk();
        Task TestRemoteTask();
        Task<string> TestRemoteTaskWithResult();
        Task<string> TestRemoteAsyncTaskWithResult();
	void UploadStream(Stream stream, string fileName);
        Stream DownloadStream(string fileName);
    }

```

### 2)implement the interface in MyImpl.dll, reference RpcOverHttp.dll, MyInterface.dll

 RpcService is only for access user info here. can be removed if do not accss User object

````
public class RpcServiceSample : RpcService, IRpcServiceSample
    {
        public RpcServiceSample()
        {
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

````

### 3)create server in a console application, reference RpcOverHttp.dll, MyInterface.dll and MyImpl.dll
```
public static void Main(string[] args)
        {
            var url = "http://127.0.0.1:8970/";
            RpcServer server = new RpcServer();
            server.Register<IRpcServiceSample, RpcServiceSample>();
            server.Start(url);
            Console.ReadLine();
        }

```

### 4)create client in a console application, reference RpcOverHttp.dll, MyInterface.dll

```
static void Main(string[] args)
        {
            var client = RpcClient.Initialize("http://localhost:8970/");
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
            Console.ReadLine();
		}

```

### advanced or what i remembered.

- like asp.net mvc. using AuthorizeAttribute/AllowAnonymousAttribute to control your rpc service access
- using Task/Task<T> as the return type to do async stuff.
- default data serializer is a built-in protobuf serializer(ProtoBufRpcDataSerializer),to override the defaults, define your own serializer and register it by using the ioc registration method both in server and client, you should hand the stream type carefully when serialize/deserialize in your own implementation.
- built-in a implementation of IExceptionHandler so do IAuthorizeHandler,you can define your own implementation and register it by using the ioc registration method in server to override the defaults
- ummm... the client request timeout is 120s. so do the Task/Task<T> waiting timeout at the server side.
- the request/response is standard http request, fiddler can review the communication
- metadata is serailized using IRpcHeadSerializer(JsonRpcHeadSerializer) as default, then adds to http header, the header name is "meta"
- the http body both request and response is serialize/deserialize by using IRpcDataSerializer(ProtoBufRpcDataSerializer) as default

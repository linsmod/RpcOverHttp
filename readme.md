Selet language: **ENGLISH** | [中文](readme_cn.md)

### RpcOverHttp
a .NET interface based rpc framework, process command using http protocol under the hood.
the interface/implementation and its method is like the asp.net mvc controller+action style but more simple to client use.



## features.
- interface based. makes you **focus on the business**.
- **async support** by providing Task/Task&lt;T&gt; as a method return type.
- provide **ioc container** both in server and client.
- control **authroization, serialization** by your self or framework default.
- serialize data using the built-in protobuf serializer and serialize rpc header using json serializer as default.
- server **exception wrapping**, client will throw a RpcException when received a server side error.
- support self host and **iis intergration**.
- **.net event** supported (required Windows8 and Server2012 when using iis-intergration). 
- support https, provide https certificate auto generation when self host mode.
- **auto dispose** the method arguments and return value after user code when using Stream or other objects inherited from Idisposible.

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

## argument types supported
- .NET primitive types and String, Array, List&lt;T&gt;, Enum
- Stream
- classes that only using the types mentioned above.

## limitations
- see #argument types supported
- nested Task as return type is not supported.

## https
yes, it supports https with a simple way.
self host mode:
- at server side, if you use a https url, when server starting, framework will auto generate a cert file pair(private key will install to system(LocalMachine->Personal) and the public key is exported as a cert file under working dir for client use)
- at client side, find the exported cert file by server and feed it to the initialize method.
```
public static RpcClient Initialize(string url, string cerFilePath, WebProxy proxy = null)
```
to regenerate cert file pair, delete the cert from system(LocalMachine->Personal). the cert name is "RpcOverHttp"

iis-intergration mode:
- at server side, select the cert file by using iis manager as usual.
- at client side, when ssl server certificate checking error, RpcClient.ServerCertificateValidationCallback will be called. just handle it like using HttpWebReqeust.ServerCertificateValidationCallback.

## iis intergration since version 3.3.0
we provided a http module for host rpc server on iis since version 3.3.0. 
selfhost sample:
```
	public class Program
    {
        public static void Main(string[] args)
        {
            var url = ConfigurationManager.AppSettings["urlPrefix"];
            RpcServer server = new RpcServer();
            server.Register<IRpcServiceSample, RpcServiceSample>();
            server.Start(url);
            Console.ReadLine();
        }
    }

```

and the iis module sample:

```
	//dll name is RpcHost, namespace is RpcHost
    public class RpcWebHostHttpModule : RpcServerHttpModule
    {
        public override void InitRpcServer(IRpcServer server)
        {
            server.Register<IRpcServiceSample, RpcServiceSample>();
        }
    }
```

**you should build your server project as a dll instead of an application(exe).** when using iis-intergration

then copy all the output dlls into site’s bin folder(create one if non exists).

then register the http module in web.config

```
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
    <system.web>
        <compilation debug="true" targetFramework="4.5" />
        <httpRuntime targetFramework="4.5" />
    </system.web>
    <system.webServer>
        <modules>
            <add name="RpcWebHostHttpModule" type="RpcHost.RpcWebHostHttpModule" />
        </modules>
        <httpProtocol>
            <customHeaders>
                <remove name="X-Powered-By" />
            </customHeaders>
        </httpProtocol>
    </system.webServer>
</configuration>

```

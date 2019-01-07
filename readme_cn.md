选择语言: **中文** | [ENGLISH](readme.md)

### RpcOverHttp
一个基于.NET接口的rpc框架，使用http协议。
接口/实现及其方法类似于asp.net mvc的controller/action样式，但更易于客户端使用。



## 特性
- 基于接口, 让你**专注于业务**。
-  **异步支持**, 通过提供Task/Task&lt; T&gt; 作为方法返回类型。
- **依赖注入**, 在服务器和客户端提供ioc容器的类型和对象注册接口。
- 通过你自己或框架内置的默认的实现控制** 调用认证和数据序列化**。
- 使用内置的protobuf serializer序列化请求参数，使用json serializer序列化请求元数据（RpcHead, 包含接口方法路由信息）。
- 使用服务器端**异常包装**，客户端在收到服务器端错误时将抛出RpcException。
- 支持self host 和** iis集成**。
-  **支持.NET事件 **，由websocket驱动（使用iis-集成时要求Windows8，Server2012，及以上）。 
- 支持https，在自托管模式下自动生成https自签名证书。
-  **对象自动释放** 当使用Stream或从IDisposible继承的其他对象，框架将自动调用IDisposible.Dispose。

## 如何使用？
### 1）在MyInterface.dll中定义接口
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

### 2）在MyImpl.dll中实现接口，引用RpcOverHttp.dll，MyInterface.dll

 RpcService仅用于访问用户信息。如果不使用User对象可以删除


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

### 3）在控制台应用程序项目中创建测试服务，引用RpcOverHttp.dll，MyInterface.dll和MyImpl.dll
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

### 4）在控制台应用程序项目中创建测试客户端，引用RpcOverHttp.dll，MyInterface.dll

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

## 支持的参数类型
-  .NET基础类型和String，List&lt; T&gt;，Enum
-  Stream
- 仅使用上述类型的类。
- 上述类型或仅使用上述类型的类的数组或泛型List

## 限制
- 请参阅支持的#argument类型
- 不支持嵌套Task作为返回类型。

## https
self host模式：
- 在服务器端，如果你使用https网址，当服务器启动时，框架将自动生成一个自签名证书文件对（私钥将安装到系统（LocalMachine-> Personal），公钥将保存在工作目录下供客户使用）
- 在客户端，将服务端导出的证书公钥文件提供给initialize方法。
```
public static RpcClient Initialize（string url，string cerFilePath，WebProxy proxy = null）
```
要重新生成证书文件对，请从系统中删除证书（LocalMachine-> Personal）。证书名称为“RpcOverHttp”

iis-integration模式：
- 在服务器端，使用iis管理器选择证书文件。
- 在客户端，当ssl服务器证书检查出错时，将调用RpcClient.ServerCertificateValidationCallback。只需要像使用HttpWebReqeust.ServerCertificateValidationCallback一样处理它。

## 从版本3.3.0开始支持iis集成，为在iis上托管rpc服务器提供了http模块
self host示例：
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

iis模块示例：


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

**在使用iis-integration时，请将服务器项目构建为dll而不是应用程序（exe）。**

然后将服务端项目所有输出dll复制到site的bin文件夹中（如果bin文件夹不存在则创建一个）。

然后在web.config中注册http模块

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
    </system.webServer>
</configuration>

```

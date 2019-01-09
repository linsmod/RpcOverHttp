using System;
using System.IO;
using System.Threading.Tasks;

namespace RpcServiceCollection
{
    public interface IRpcServiceSample
    {
        event Func<string, int> TestEventHandlerWithReturn;
        event EventHandler<object> TestEventHandlerGeneric;
        event EventHandler TestEventHandler;
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
}
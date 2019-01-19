using System;
using System.IO;
using System.Threading.Tasks;

namespace RpcServiceCollection
{
    public interface IRpcServiceSample
    {
        event Func<string, int> SampleFuncEvent;
        event EventHandler SimpleEvent;
        event EventHandler<string> SampleActionEvent;
        string GetUserName();
        bool IsUserAuthenticated();
        void TestRemoteEventHandler();
        void TestAccessDeniend();
        void TestAccessOk();
        Task TestRemoteTask();
        Task<string> TestRemoteTaskWithResult();
        Task<string> TestRemoteAsyncTaskWithResult();
        void UploadStream(Stream stream, string fileName);
        Stream DownloadStream(string fileName);
    }
}
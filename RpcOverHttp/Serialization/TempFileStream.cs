using System;
using System.IO;
using System.Security.AccessControl;
using Microsoft.Win32.SafeHandles;
#pragma warning disable CS0618 // Type or member is obsolete
namespace RpcOverHttp.Serialization
{
    public partial class ProtoBufRpcDataSerializer
    {
        public class TempFileStream : FileStream
        {
            public TempFileStream(string path, FileMode mode) : base(path, mode)
            {
            }

            public TempFileStream(IntPtr handle, FileAccess access) : base(handle, access)
            {
            }

            public TempFileStream(SafeFileHandle handle, FileAccess access) : base(handle, access)
            {
            }

            public TempFileStream(string path, FileMode mode, FileAccess access) : base(path, mode, access)
            {
            }


            public TempFileStream(IntPtr handle, FileAccess access, bool ownsHandle) : base(handle, access, ownsHandle)

            {
            }

            public TempFileStream(SafeFileHandle handle, FileAccess access, int bufferSize) : base(handle, access, bufferSize)
            {
            }

            public TempFileStream(string path, FileMode mode, FileAccess access, FileShare share) : base(path, mode, access, share)
            {
            }

            public TempFileStream(IntPtr handle, FileAccess access, bool ownsHandle, int bufferSize) : base(handle, access, ownsHandle, bufferSize)
            {
            }

            public TempFileStream(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync) : base(handle, access, bufferSize, isAsync)
            {
            }

            public TempFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize) : base(path, mode, access, share, bufferSize)
            {
            }

            public TempFileStream(IntPtr handle, FileAccess access, bool ownsHandle, int bufferSize, bool isAsync) : base(handle, access, ownsHandle, bufferSize, isAsync)
            {
            }

            public TempFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options) : base(path, mode, access, share, bufferSize, options)
            {
            }

            public TempFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync) : base(path, mode, access, share, bufferSize, useAsync)
            {
            }

            public TempFileStream(string path, FileMode mode, FileSystemRights rights, FileShare share, int bufferSize, FileOptions options) : base(path, mode, rights, share, bufferSize, options)
            {
            }

            public TempFileStream(string path, FileMode mode, FileSystemRights rights, FileShare share, int bufferSize, FileOptions options, FileSecurity fileSecurity) : base(path, mode, rights, share, bufferSize, options, fileSecurity)
            {
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (disposing)
                {
                    File.Delete(this.Name);
                }
            }
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
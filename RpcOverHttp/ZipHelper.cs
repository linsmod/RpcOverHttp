using System.IO;
using System.IO.Compression;

namespace RpcOverHttp
{
    public class ZipHelper
    {
        public static void UnZip(string zipFile, string destination)
        {
            ZipArchive archive = new ZipArchive(File.OpenRead(zipFile));
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                using (var stream = entry.Open())
                {
                    var itemPath = Path.Combine(destination, entry.FullName);
                    var itemDir = Path.GetDirectoryName(itemPath);
                    if (!Directory.Exists(itemDir))
                        Directory.CreateDirectory(itemDir);
                    using (var fs = File.Create(itemPath))
                    {
                        stream.CopyTo(fs);
                    }
                }
            }
        }
        public static void UnZip(Stream zipFileStream, string destination)
        {
            ZipArchive archive = new ZipArchive(zipFileStream);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                using (var stream = entry.Open())
                {
                    var itemPath = Path.Combine(destination, entry.FullName);
                    var itemDir = Path.GetDirectoryName(itemPath);
                    if (!Directory.Exists(itemDir))
                        Directory.CreateDirectory(itemDir);
                    using (var fs = File.Create(itemPath))
                    {
                        stream.CopyTo(fs);
                    }
                }
            }
        }
    }
}

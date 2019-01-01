using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcOverHttp
{
    public class ByteSize
    {
        public long TotalBytes { get; set; }
        public double TotalKbs { get; set; }
        public double TotalMbs { get; set; }
        public double TotalGbs { get; set; }
        public double TotalTbs { get; set; }
        public ByteSize(long bytes)
        {
            TotalBytes = bytes;
            TotalKbs = bytes >> 10;
            TotalMbs = bytes >> 10 >> 10;
            TotalGbs = bytes >> 10 >> 10 >> 10;
            TotalTbs = bytes >> 10 >> 10 >> 10 >> 10;
        }
        public static ByteSize FromBytes(long lengthBytes)
        {
            return new ByteSize(lengthBytes);
        }

        public static ByteSize FromKbs(int kbs)
        {
            return new ByteSize(kbs << 10);
        }

        public static ByteSize FromMbs(int mbs)
        {
            return new ByteSize(mbs << 10 << 10);
        }

        public static ByteSize FromBbs(int gbs)
        {
            return new ByteSize(gbs << 10 << 10 << 10);
        }

        public override string ToString()
        {
            return TotalMbs.ToString("f2") + "M";
        }
    }
}

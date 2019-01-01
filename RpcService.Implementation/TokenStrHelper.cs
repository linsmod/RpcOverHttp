using CSharpApp;
using JiebaNet.Segmenter.PosSeg;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RpcService.Implementation
{
    public class TokenStrHelper
    {
        App app;
        Thread LoadThread;
        object segmenterLock = new object();
        bool user_dict_load = false;
        string user_dict_txt = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/user_dict_txt.txt");
        PosSegmenter segmenter = new PosSegmenter();
        PosSegmenter updatedSegmenter = new PosSegmenter();
        public List<RpcServiceCollection.MatchGroup> TokenMatchGroup(List<RpcServiceCollection.MatchGroup> matchGroups, string str)
        {
            var list = TokenStr(matchGroups, str).Select(x => x.Flag.Split('|').Concat(new string[] { x.Word }).ToArray())
                    .Where(x => x.Length == 3)
                    .Select(x => new RpcServiceCollection.MatchGroup
                    {
                        Type = (RpcServiceCollection.EntryType)int.Parse(x[0]),
                        Id = int.Parse(x[1]),
                        Name = x[2]
                    })
                    .ToList();
            return list;
        }

        void InitTokenSegmenter()
        {
            Trace.WriteLine("InitTokenSegmenter Thread Start.");
            updating = true;
            try
            {
                using (ApplicationDbContext db = new ApplicationDbContext())
                {
                    var list = app.MemoryCache.Get("match_groups", () => db.MatchGroups.ToList());
                    updatedSegmenter = new PosSegmenter();
                    foreach (var item in list)
                    {
                        updatedSegmenter.AddWord(item.Name, 99999, ((int)item.Type).ToString() + "|" + item.Id);
                    }
                    File.WriteAllLines(user_dict_txt, list.Select(x => x.Name + "|" + (int)x.Type + "|" + x.Id).ToArray());
                    user_dict_load = true;
                }
                segmenter = updatedSegmenter;
                updatedSegmenter = null;
            }
            finally
            {
                Trace.WriteLine("InitTokenSegmenter Thread End.");
                updating = false;
            }
        }
        static bool updating = false;

        public TokenStrHelper(App app1)
        {
            this.app = app1;
        }

        public List<Pair> TokenStr(List<RpcServiceCollection.MatchGroup> matchGroups, string str)
        {
            Trace.WriteLine("TokenStr called from thread " + Thread.CurrentThread.ManagedThreadId);
            if (!user_dict_load)
            {
                lock (segmenterLock)
                {
                    if (!user_dict_load && LoadThread == null)
                    {
                        LoadThread = new Thread(InitTokenSegmenter);
                        LoadThread.Start();
                        return new List<Pair>();
                    }
                }
            }
            var last_write_time = File.GetLastWriteTime(user_dict_txt);
            var update_time = matchGroups.Max(x => x.UpdateTime);
            if (DateTime.Now.Subtract(last_write_time).TotalMinutes > 10 || update_time > last_write_time)
            {
                lock (segmenterLock)
                {
                    if (!updating)
                    {
                        LoadThread = new Thread(InitTokenSegmenter);
                        LoadThread.Start();
                    }
                }
            }
            var values = segmenter.Cut(str, true);
            return values.ToList();
        }
    }
}

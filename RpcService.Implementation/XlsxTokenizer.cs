using CSharpApp;
using RpcServiceCollection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ivony.Html;
namespace RpcService.Implementation
{
    public class XlsxTokenizer : IXlsxTokenizer
    {
        ApplicationDbContext db = new ApplicationDbContext();
        TokenStrHelper tokenHelper;
        App app;
        public XlsxTokenizer(App app)
        {
            this.app = app;
            tokenHelper = new TokenStrHelper(app);
        }

        public void DeleteXlsx(string xlsx_id)
        {
            xlsx_id = Path.GetFileNameWithoutExtension(xlsx_id) + ".xlsx";
            app.AppDataVfs.DeleteFile(xlsx_id);
        }

        public Stream DownloadXlsx(string xlsx_id)
        {
            if (xlsx_id.StartsWith("2672759683"))
            {

            }
            xlsx_id = Path.GetFileNameWithoutExtension(xlsx_id) + ".xlsx";
            return app.AppDataVfs.OpenRead(xlsx_id);
        }


        Ivony.Html.Parser.JumonyParser p = new Ivony.Html.Parser.JumonyParser();
        public string SegmentXlsx(string xlsx_id, int column_idx, int start_row_idx)
        {
            xlsx_id = Path.GetFileNameWithoutExtension(xlsx_id) + ".xlsx";
            var book = app.AppDataVfs.GetWorkbookUtil(xlsx_id, false);
            var output = Path.ChangeExtension(xlsx_id, ".output.xlsx");
            app.AppDataVfs.Copy(xlsx_id, output, true);
            var book2 = app.AppDataVfs.GetWorkbookUtil(output, false);
            var columnIdx = book2.GetColumnCount(0);
            List<string> values;
            try
            {
                var count = book.ReadColumn<string>(start_row_idx, column_idx, out values);
                if (count > 0)
                {
                    int rowIdx = start_row_idx;
                    var groups = app.MemoryCache.Get("match_groups", () => db.MatchGroups.ToList());
                    foreach (var item in values)
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            var html = "<p>" + item + "</p>";
                            var document = p.Parse(html, new Uri("http://baidu.com"));
                            var elements = document.Elements();
                            var lines = new List<string>();
                            foreach (var element in elements)
                            {
                                if (element.Name != "style" && element.Name != "script")
                                {
                                    lines.AddRange(ReadLines(element));
                                }
                            }
                            var text = clean(item);
                            var list = tokenHelper.TokenMatchGroup(groups, text);
                            //Console.WriteLine(rowIdx);
                            var typedKws = list.GroupBy(x => x.Type).Select(x => new TypedKw { Type = x.Key, Kws = x.Select(g => g.Name).ToArray() }).ToList();
                            foreach (var typedKw in typedKws)
                            {
                                var v = string.Join(" ", typedKw.Kws.Distinct());
                                switch (typedKw.Type)
                                {
                                    case EntryType.地区:
                                        book2.SetRowCellValue(0, rowIdx, columnIdx + 1, v);
                                        Console.WriteLine("SetRowCellValue column={0} row={1} value={2}", columnIdx + 1, rowIdx, string.Join(" ", typedKw.Kws));
                                        break;
                                    case EntryType.医院:
                                        book2.SetRowCellValue(0, rowIdx, columnIdx + 2, v);
                                        Console.WriteLine("SetRowCellValue column={0} row={1} value={2}", columnIdx + 2, rowIdx, string.Join(" ", typedKw.Kws));
                                        break;
                                    case EntryType.医生:
                                        book2.SetRowCellValue(0, rowIdx, columnIdx + 3, v);
                                        Console.WriteLine("SetRowCellValue column={0} row={1} value={2}", columnIdx + 3, rowIdx, string.Join(" ", typedKw.Kws));
                                        break;
                                    case EntryType.项目:
                                        book2.SetRowCellValue(0, rowIdx, columnIdx + 4, v);
                                        Console.WriteLine("SetRowCellValue column={0} row={1} value={2}", columnIdx + 4, rowIdx, string.Join(" ", typedKw.Kws));
                                        break;
                                    case EntryType.描述:
                                        book2.SetRowCellValue(0, rowIdx, columnIdx + 5, v);
                                        Console.WriteLine("SetRowCellValue column={0} row={1} value={2}", columnIdx + 5, rowIdx, string.Join(" ", typedKw.Kws));
                                        break;
                                    case EntryType.域名:
                                        book2.SetRowCellValue(0, rowIdx, columnIdx + 6, v);
                                        Console.WriteLine("SetRowCellValue column={0} row={1} value={2}", columnIdx + 6, rowIdx, string.Join(" ", typedKw.Kws));
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                        rowIdx++;
                    }
                    book2.Save();

                    return output;
                }
                else
                {
                    throw new Exception("read 0 columns from " + xlsx_id);
                }
            }
            finally
            {
                book.Delete();
            }
        }
        private string clean(string text)
        {
            var html = "<p>" + text + "</p>";
            var document = p.Parse(html, new Uri("http://baidu.com"));
            var elements = document.Elements();
            var lines = new List<string>();
            foreach (var element in elements)
            {
                if (element.Name != "style" && element.Name != "script")
                {
                    lines.AddRange(ReadLines(element));
                }
            }
            return string.Join(" ", lines);
        }
        private List<string> ReadLines(IHtmlElement elem)
        {
            List<string> list = new List<string>();
            var elements = elem.Elements();
            if (!elements.Any() || elem.Name == "p" || elem.Name == "div" || elem.Name == "a")
            {
                try
                {
                    var text = elem.InnerText();
                    if (string.IsNullOrEmpty(text))
                    {
                        list.Add("");
                    }
                    else
                    {
                        list.Add(text);
                    }
                }
                catch
                {

                }
            }
            else if (elem.Name == "br" || elem.Name == "hr")
            {

            }
            else
            {
                foreach (var item in elements)
                {
                    if (item.Name != "style" && item.Name != "script")
                    {
                        list.AddRange(ReadLines(item));
                    }
                }
            }
            return list;
        }

        public List<Pair> Segment(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<Pair>();
            text = clean(text);
            if (text.Trim() == "")
            {
                return new List<Pair>();
            }
            var groups = app.MemoryCache.Get("match_groups", () => db.MatchGroups.ToList());
            return tokenHelper.TokenStr(groups, text).Select(x => new Pair(x.Word, x.Flag)).ToList();
        }

        public string UploadXlsx(byte[] file)
        {
            var id = Guid.NewGuid().ToString("N");
            app.AppDataVfs.Write(id, file);
            return id;
        }

        public string UploadXlsx(string xlsx_id, byte[] file)
        {
            xlsx_id = Path.GetFileNameWithoutExtension(xlsx_id) + ".xlsx";
            if (app.AppDataVfs.FileExists(xlsx_id))
            {
                throw new Exception(string.Format("file {0} already exists.", xlsx_id));
            }
            app.AppDataVfs.Write(xlsx_id, file);
            return xlsx_id;
        }
    }
}

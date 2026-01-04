using NLog;
using QuickEPUB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ContentExtractor
{
    /// <summary>
    /// 一個網頁截取工作，含有一批截取連接
    /// </summary>
    public class FetchJob
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 記錄所有被要求的工作連結
        /// </summary>
        private readonly List<PageFetchItem> allItems;       

        /// <summary>
        /// 是否直接長行解析
        /// </summary>
        private readonly bool isLongLineDirect;

        /// <summary>
        /// 公開方法共用的同步鎖
        /// </summary>
        private readonly object operationLock = new object();

        /// <summary>
        /// ctor
        /// </summary>
        public FetchJob(List<Tuple<string, string>> fetchList, bool isLongLineDirect)
        {
            this.isLongLineDirect = isLongLineDirect;
            allItems = fetchList.Select((f, i) => new PageFetchItem(isLongLineDirect)
            {
                Index = i + 1,
                Url = f.Item1,
                Title = f.Item2
            }).ToList();
        }

        #region Public Methods

        private CancellationTokenSource cts;

        /// <summary>
        /// 開始進行截取工作
        /// </summary>
        public void ProcessAsync(bool isRandomDelay)
        {
            CancelProcessAsync();

            cts = new CancellationTokenSource();
            var rand = new Random(Environment.TickCount);

            Task.Run(() =>
            {
                lock (operationLock)
                {
                    try
                    {
                        var lastSleep = DateTime.Now;
                        var counter = 0;
                        foreach (var fetchItem in allItems)
                        {
                            if (cts.IsCancellationRequested) return;

                            //沒資料就試著拿取
                            if (!fetchItem.IsFetched)
                            {
                                fetchItem.ParseTextContext();

                                //如果有拿到資料而且有要求隨機延遲，就隨機延遲一下
                                if (isRandomDelay && fetchItem.IsFetched) Thread.Sleep(rand.Next(1, 5) * 1000);
                            }

                            //不論成敗，發動事件回報
                            counter++;
                            FireOnProcessStatus((double)counter / allItems.Count, fetchItem);

                            //每拿百個就停一下手，免得被盯
                            if (counter % 100 == 0 && DateTime.Now.Subtract(lastSleep).TotalSeconds > 5)
                            {
                                Thread.Sleep(10000);
                                lastSleep = DateTime.Now;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        //todo : some error control
                        Logger.Error(e);
                    }

                    FireOnProcessCompleted(allItems.All(x => x.IsFetched));
                }
            }, cts.Token);
        }

        /// <summary>
        /// 停下當前的下載
        /// </summary>
        public void CancelProcessAsync()
        {
            if (cts?.IsCancellationRequested == false) cts.Cancel();
        }

        /// <summary>
        /// 將截取結果輸出成文字檔案
        /// </summary>
        /// <param name="path"></param>
        /// <param name="bookName"></param>
        /// <param name="authName"></param>
        /// <returns></returns>
        public bool SaveToTxtFile(string path, string bookName, string authName, Func<int,bool> checkRequireFunc)
        {
            try
            {
                var tarFile = new FileInfo($@"{path}\{authName}-{bookName}.txt");

                if (tarFile.Exists)
                {
                    tarFile.Delete();
                }

                using (var sw = new StreamWriter(tarFile.Create()))
                {
                    foreach (var fetchItem in allItems)
                    {
                        if (!checkRequireFunc(fetchItem.Index)) continue;

                        sw.WriteLine(ModifyTitle(fetchItem.Title, fetchItem.Index));
                        sw.WriteLine();

                        foreach (var contextItem in fetchItem.GetContext())
                        {
                            if (!contextItem.Any())
                            {
                                Logger.Warn($"Empty Item : [{fetchItem.Index}][{fetchItem.Title}]{fetchItem.Url}");
                                continue;
                            }
                            sw.WriteLine(contextItem);
                            sw.WriteLine();
                        }
                    }

                    sw.Close();
                }

                return true;
            }
            catch (Exception ex)
            {
                //todo : error control
                Logger.Error(ex);
                return false;
            }
        }

        public static List<List<PageFetchItem>> SplitList(List<PageFetchItem> locations, int nSize = 1000)
        {
            var list = new List<List<PageFetchItem>>();

            for (var i = 0; i < locations.Count; i += nSize)
            {
                list.Add(locations.GetRange(i, Math.Min(nSize, locations.Count - i)));
            }

            return list;
        }

        /// <summary>
        /// 將截取結果輸出成電子書檔案
        /// </summary>
        /// <param name="path"></param>
        /// <param name="bookName"></param>
        /// <param name="authName"></param>
        /// <param name="chunkSize">指定切分檔 item 數，0 以下為不切分</param>
        /// <returns></returns>
        public bool SaveToEpubFile(string path, string bookName, string authName, Func<int, bool> checkRequiredFunc, int chunkSize = 0)
        {
            if (chunkSize <= 0) return SaveToEpubFile(path, bookName, authName, allItems, checkRequiredFunc);

            var chunkList = SplitList(allItems, chunkSize).Select((x, i) => new { Index = i + 1, Value = x });
            return chunkList.All(chunk => SaveToEpubFile(path, bookName, authName, chunk.Value, checkRequiredFunc, chunk.Index, chunkList.Count().ToString().Length));
        }

        /// <summary>
        /// 指定內容存在 epub
        /// </summary>Func<int, bool> checkRequiredFunc,
        /// <param name="path"></param>
        /// <param name="bookName"></param>
        /// <param name="authName"></param>
        /// <param name="targets"></param>
        /// <param name="chunkNum">指出這是第幾個分割</param>
        /// <param name="chunkPadWidth">指出分割檔名的對齊寬度</param>
        /// <returns></returns>
        private static bool SaveToEpubFile(string path, string bookName, string authName, List<PageFetchItem> targets, Func<int, bool> checkRequiredFunc, int chunkNum = 0, int chunkPadWidth = 1)
        {
            try
            {
                var chunkText = chunkNum <= 0 ? string.Empty : $"_{chunkNum.ToString().PadLeft(chunkPadWidth, '0')}";
                var tarFile = new FileInfo($@"{path}\{authName}-{bookName}{chunkText}.epub");

                if (tarFile.Exists)
                {
                    tarFile.Delete();
                }

                var doc = new Epub($"{bookName}{chunkText}", authName);

                foreach (var pageFetchItem in targets)
                {
                    if (!checkRequiredFunc(pageFetchItem.Index)) continue;

                    var context = pageFetchItem.GetContext();
                    if (!context.Any())
                    {
                        Logger.Warn($"Empty Item : [{pageFetchItem.Index}][{pageFetchItem.Title}]{pageFetchItem.Url}");
                        continue;
                    }

                    var epubContext = context.Select(s => $"<p>{System.Net.WebUtility.HtmlEncode(s ?? "")}</p>");
                    var epubTitle = System.Net.WebUtility.HtmlEncode(pageFetchItem.Title ?? "");
                    if (string.IsNullOrWhiteSpace(epubTitle)) epubTitle = "無標題";
                    epubTitle = ModifyTitle(epubTitle, pageFetchItem.Index);
                    doc.AddSection(epubTitle, $"<h2>{epubTitle}</h2> {string.Join(" ", epubContext)}");
                }

                using (var fs = new FileStream(tarFile.FullName, FileMode.Create))
                {
                    doc.Export(fs);
                }

                return true;
            }
            catch (Exception ex)
            {
                //todo : error control
                Logger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// 取得當下的截取狀態
        /// </summary>
        /// <returns></returns>
        public List<Tuple<int, bool, string, int, string>> GetAllFetchStatus()
        {
            return allItems.Select(item => new Tuple<int, bool, string, int, string>(item.Index, item.IsFetched, item.Title, item.GetRoughContextLen(), item.Url)).ToList();
        }

        /// <summary>
        /// 客制化輸出標題，主要是為了解決部份小說章節名稱在不同小說軟體時，不具全域惟一性，不好快速找到的問題
        /// </summary>
        /// <param name="title"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static string ModifyTitle(string title, int index)
        {
            var prefixTitle = $"第{index:0000}回";
            if (string.IsNullOrWhiteSpace(title)) return prefixTitle;
            if (title.Contains(" "))
            {
                var src = title.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var rest = src.Skip(1).Take(src.Length - 1);
                return $"{prefixTitle} {string.Join("", rest)}";
            }

            return $"{prefixTitle} {title}";
        }

        #endregion

        #region events

        public delegate void ProcessStatusHandler(double progressRate, PageFetchItem item);

        /// <summary>
        /// 當一個截取行為完成時發動進度回報事件
        /// </summary>
        public event ProcessStatusHandler OnProcessStatus;

        private void FireOnProcessStatus(double progressRate, PageFetchItem item)
        {
            var temp = OnProcessStatus;
            temp?.Invoke(progressRate, item);
        }

        public delegate void ProcessCompletedHandler(bool isAllDone);

        /// <summary>
        /// 當整個截取工作完成一輪時發動事件
        /// </summary>
        public event ProcessCompletedHandler OnProcessCompleted;

        private void FireOnProcessCompleted(bool isAllDone)
        {
            var temp = OnProcessCompleted;
            temp?.Invoke(isAllDone);
        }

        #endregion
    }
}
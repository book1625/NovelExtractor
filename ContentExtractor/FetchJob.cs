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
        /// 呼叫網頁連結間的間隔，避免被對方服務器視為攻擊
        /// </summary>
        private const int DefaultFetchPeriodMs = 100;

        /// <summary>
        /// 記錄所有被要求的工作連結
        /// </summary>
        private readonly List<PageFetchItem> _allItems;

        /// <summary>
        /// 公開方法共用的同步鎖
        /// </summary>
        private readonly object _operationLock = new object();

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="fetchList"></param>
        public FetchJob(List<Tuple<string, string>> fetchList)
        {
            var index = 1;
            _allItems = fetchList.Select(f => new PageFetchItem()
            {
                Index = index++,
                Url = f.Item1,
                Title = f.Item2
            }).ToList();
        }

        #region Public Methods

        private CancellationTokenSource cts;

        /// <summary>
        /// 開始進行截取工作
        /// </summary>
        public void ProcessAsync()
        {
            CancelProcessAsync();

            cts = new CancellationTokenSource();

            Task.Run(() =>
            {
                lock (_operationLock)
                {
                    try
                    {
                        var counter = 1;
                        foreach (var fetchItem in _allItems)
                        {
                            if (cts.IsCancellationRequested) return;

                            if (!fetchItem.IsFetched)
                            {
                                fetchItem.ParseTextContext();

                                //如果有發生取得異常，就意思一下先 hold 住不要一直捉
                                if (!fetchItem.IsFetched) 
                                    Thread.Sleep(10000);
                                else 
                                    Thread.Sleep(DefaultFetchPeriodMs);
                            }

                            FireOnProcessStatus((double)counter / _allItems.Count, fetchItem);
                            counter++;
                        }
                    }
                    catch (Exception e)
                    {
                        //todo : some error control
                        Logger.Error(e);
                    }

                    FireOnProcessCompleted(_allItems.All(x => x.IsFetched));
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
        public bool SaveToTxtFile(string path, string bookName, string authName)
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
                    foreach (var fetchItem in _allItems)
                    {
                        sw.WriteLine(fetchItem.Title);
                        sw.WriteLine();

                        foreach (var contextItem in fetchItem.GetContext())
                        {
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

        /// <summary>
        /// 將截取結果輸出成電子書檔案
        /// </summary>
        /// <param name="path"></param>
        /// <param name="bookName"></param>
        /// <param name="authName"></param>
        /// <returns></returns>
        public bool SaveToEpubFile(string path, string bookName, string authName)
        {
            try
            {
                var tarFile = new FileInfo($@"{path}\{authName}-{bookName}.epub");

                if (tarFile.Exists)
                {
                    tarFile.Delete();
                }


                var doc = new Epub(bookName, authName);

                foreach (var pageFetchItem in _allItems)
                {
                    var epubContext = pageFetchItem.GetContext().Select(s => $"<p>{s}</p>");
                    doc.AddSection(pageFetchItem.Title, $"<h2>{pageFetchItem.Title}</h2> {string.Join(" ", epubContext)}");
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
        public List<Tuple<int, bool, string, int>> GetAllFetchStatus()
        {
            return _allItems.Select(item => new Tuple<int, bool, string, int>(item.Index, item.IsFetched, item.Title, item.GetRoughContextLen())).ToList();
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
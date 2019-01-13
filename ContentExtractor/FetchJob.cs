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
        /// <summary>
        /// 呼叫網頁連結間的間隔，避免被對方服務器視為攻擊
        /// </summary>
        private const int DefaultFetchPeriodMs = 300;

        /// <summary>
        /// 記錄所有被要求的工作連結
        /// </summary>
        private List<FetchItem> fAllItems;

        /// <summary>
        /// 公開方法共用的同步鎖
        /// </summary>
        private readonly object fOperationLock = new object();

        /// <summary>
        /// ctor 用來針對 cK101 的網址作設置的
        /// </summary>
        /// <param name="fromIndex"></param>
        /// <param name="toIndex"></param>
        /// <param name="threadId"></param>
        /// <param name="contentKeyword"></param>
        /// <param name="urlTemplate"></param>
        public FetchJob(int fromIndex, int toIndex, int threadId, string contentKeyword, string urlTemplate)
        {
            InitItems(fromIndex, toIndex,threadId, contentKeyword, urlTemplate);
        }

        /// <summary>
        /// 初始化所有需要截取的網頁連接
        /// </summary>
        /// <param name="fromIndex"></param>
        /// <param name="toIndex"></param>
        /// <param name="threadId"></param>
        /// <param name="contentKeyword"></param>
        /// <param name="urlTemplate"></param>
        private void InitItems(int fromIndex, int toIndex, int threadId, string contentKeyword, string urlTemplate)
        {
            fAllItems = new List<FetchItem>();
            
            for (var i = fromIndex; i <= toIndex; i++)
            {
                var tarItem = new FetchItem
                {
                    Url = string.Format(urlTemplate, threadId, i),
                    Keyword = contentKeyword
                };
                fAllItems.Add(tarItem);
            }
        }

        /// <summary>
        /// 取得當前所有截取工作的 url
        /// </summary>
        /// <returns></returns>
        public List<string> GetUrls()
        {
            lock (fOperationLock)
            {
                return fAllItems.Select(x => x.Url).ToList();
            }
        }

        /// <summary>
        /// 開始進行截取工作
        /// </summary>
        public void ProcessAsync()
        {
            Task.Run(() =>
            {
                lock (fOperationLock)
                {
                    try
                    {
                        var index = 1.0;
                        foreach (var fetchItem in fAllItems)
                        {
                            string status = "";
                            string fetchString = "";

                            if (!fetchItem.IsFetched)
                            {
                                fetchItem.Process(out status, out fetchString);
                                Thread.Sleep(DefaultFetchPeriodMs);
                            }

                            FireOnProcessStatus(index / fAllItems.Count, $"{status}");
                            index++;
                        }
                    }
                    catch (Exception e)
                    {   
                        //todo : some error control ?
                    }

                    FireOnProcessCompleted(fAllItems.All(x => x.IsFetched));
                }
            });
        }

        /// <summary>
        /// 取得當前的所有截取結果，已過濾不必要的字元
        /// </summary>
        /// <returns></returns>
        public string GetFilterContent()
        {
            lock (fOperationLock)
            {
                return string.Join(Environment.NewLine, fAllItems.Select(x => x.GetFilterString()));
            }
        }

        /// <summary>
        /// 將截取結果輸出成檔案
        /// </summary>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool SaveToFile(string path, string name)
        {
            try
            {
                var tarFile = new FileInfo($@"{path}\{name}.txt");

                if (tarFile.Exists)
                {
                    tarFile.Delete();
                }

                using (var sw = new StreamWriter(tarFile.Create()))
                {
                    sw.WriteLine(GetFilterContent());
                    sw.Close();
                }

                return true;
            }
            catch (Exception ex)
            {
                //todo : error control
                return false;
            }
        }

        #region events

        public delegate void ProcessStatusHandler(double progressRate, string previewMsg);

        /// <summary>
        /// 當一個截取行為完成時發動
        /// </summary>
        public event ProcessStatusHandler OnProcessStatus;

        private void FireOnProcessStatus(double progressRate, string previewMsg)
        {
            var temp = OnProcessStatus;
            temp?.Invoke(progressRate, previewMsg);
        }

        public delegate void ProcessCompletedHandler(bool isAllDone);
        
        /// <summary>
        /// 當整個截取工作完成時發動
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
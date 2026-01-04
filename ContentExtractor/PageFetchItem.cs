using HtmlAgilityPack;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ContentExtractor
{
    /// <summary>
    /// 針對一個目標網頁的捉取物件
    /// </summary>
    public class PageFetchItem
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 解析出來的最大本文節點
        /// </summary>
        private HtmlNode targetTextNode;

        /// <summary>
        /// 最長行，推測是最大本文
        /// </summary>
        private string targetLongLine;

        /// <summary>
        /// 解析出來的所有下載節點
        /// </summary>
        private List<HtmlNode> downloadLinkNodes;

        /// <summary>
        /// 是否直接長行解析
        /// </summary>
        private bool isLongLineDirect;

        /// <summary>
        /// 優先編碼字典
        /// </summary>
        private readonly Dictionary<int, Encoding> highPriorityEncode = new Dictionary<int, Encoding>();

        /// <summary>
        /// ctor
        /// </summary>
        public PageFetchItem(bool isLongLineDirect)
        {
            this.isLongLineDirect = isLongLineDirect;

            //https://stackoverflow.com/questions/50858209/system-notsupportedexception-no-data-is-available-for-encoding-1252
            //由於新的 .net 需要另行裝套件才能支援其它語言編碼，所以專案裝上套件，並且自已把這個實體註冊給 .net
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            //以下初始化優先處理的編碼

            //big5
            highPriorityEncode.Add(950, Encoding.GetEncoding(950));

            //GB18030 簡體中文
            highPriorityEncode.Add(54936, Encoding.GetEncoding(54936));

            //UTF 8
            highPriorityEncode.Add(65001, Encoding.GetEncoding(65001));
        }

        #region Public Properties

        /// <summary>
        /// 工作索引號
        /// </summary>
        public int Index
        {
            get;
            set;
        }

        /// <summary>
        /// 目標 url
        /// </summary>
        public string Url
        {
            get;
            set;
        }

        /// <summary>
        /// 網頁標題
        /// </summary>
        public string Title
        {
            get;
            set;
        }

        /// <summary>
        /// 是否成功取得
        /// </summary>
        public bool IsFetched => (targetTextNode != null || !string.IsNullOrWhiteSpace(targetLongLine)) || downloadLinkNodes?.Count > 0;

        #endregion

        #region Public Methods

        /// <summary>
        /// 解析最大內文
        /// </summary>
        public void ParseTextContext()
        {
            var context = GetHtmlContent(
                highPriorityEncode,
                Url,
                20000,
                out var errorCode,
                out var errorMessage,
                out var redirectUrl);

            Logger.Debug($"{nameof(GetHtmlContent)} => {errorCode}, {errorMessage}, {redirectUrl}");

            if (isLongLineDirect)
            {
                var lines = context.Split('\r', '\n');
                targetLongLine = lines.OrderByDescending(l => l.Length).FirstOrDefault();
            }
            else
            {
                //抽出最大內文節點
                var dTree = new DomTree(context);
                dTree.InitMaxOuterHtmlAndMaxInnerTextNodeV2();
                targetTextNode = dTree.MaxInnerTextNode;
            }
        }

        /// <summary>
        /// 取得可用的最大本文
        /// </summary>
        /// <returns></returns>
        public string[] GetContext()
        {
            //依模式取得最大內文節點
            var targetText = isLongLineDirect ? targetLongLine : targetTextNode?.InnerText;

            if (string.IsNullOrWhiteSpace(targetText))
            {
                return Array.Empty<string>();
            }

            //過濾 HTML 專屬字元
            var text = targetText
                .Replace("&nbsp;&nbsp;", "\n")
                .Replace("&nbsp&nbsp", "")
                .Replace("&quot;", @"""")
                .Replace("&gt;", ">")
                .Replace("@lt;", "<");

            //特例??
            text = text.Replace("<br />", "");

            //過濾 不處理的 HTML 專屬字元
            var regx = new Regex("&[a-z]+;");
            text = regx.Replace(text, "");

            //過濾不支援的語言編碼
            regx = new Regex("&#[0-9]+;");
            text = regx.Replace(text, "#");

            //打掉空行和重覆標題
            //var keyTitle = Title.Split(' ').First();
            //var result = text.Split('\n', '\r')
            //    .Select(s => s.Trim())
            //    .Where(s => !string.IsNullOrEmpty(s) && !s.Contains(keyTitle))
            //    .ToArray();

            var result = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();

            //如果只有5行以下，則可能網站的排版不是靠換行符號來分段，這時就得考慮自行分段的方式
            if (result.Length < 5)
            {
                result = SplitAndKeepDelimiters(text, new[] { '？', '。', '?', '.', '「', '」' })
                    .Select(s => s.Trim())
                    .ToArray();
            }

            //可能打完後沒半行，只好全拿
            return result.Length == 0 ? new[] { text } : result;
        }

        /// <summary>
        /// 將字串依照指定符號切割（切割點在符號之後），並保留切割符號，且每段至少20個字
        /// </summary>
        /// <param name="input">原始字串</param>
        /// <param name="delimiters">要切割並保留的符號集合</param>
        /// <returns>切割後的字串陣列，包含符號，且每段至少20字</returns>
        public static string[] SplitAndKeepDelimiters(string input, char[] delimiters)
        {
            if (string.IsNullOrEmpty(input) || delimiters == null || delimiters.Length == 0)
                return new[] { input };

            // 建立正則模式，符號在句尾
            var pattern = $@"[^{Regex.Escape(new string(delimiters))}]+[{Regex.Escape(new string(delimiters))}]|[^{Regex.Escape(new string(delimiters))}]+$";
            var matches = Regex.Matches(input, pattern);

            var parts = matches.Cast<Match>().Select(m => m.Value).ToList();

            var result = new List<string>();
            var buffer = new StringBuilder();

            foreach (var part in parts)
            {
                buffer.Append(part);
                if (buffer.Length >= 20)
                {
                    result.Add(buffer.ToString());
                    buffer.Clear();
                }
            }

            // 若最後還有殘留不足20字的片段，則合併到上一段或單獨加入
            if (buffer.Length > 0)
            {
                if (result.Count > 0)
                {
                    result[result.Count - 1] += buffer.ToString();
                }
                else
                {
                    result.Add(buffer.ToString());
                }
            }

            return result.ToArray();
        }

        public int GetRoughContextLen()
        {
            return targetTextNode?.InnerLength ?? targetLongLine?.Length ?? 0;
        }

        /// <summary>
        /// 解析下載清單
        /// </summary>
        public void ParseDownloadList(int limitChildDepth = 5)
        {
            var context = GetHtmlContent(
                highPriorityEncode,
                Url,
                20000,
                out var errorCode,
                out var errorMessage,
                out var redirectUrl);

            Logger.Debug($"{nameof(ParseDownloadList)} => {errorCode}, {errorMessage}, {redirectUrl}");

            //解析下載清單
            var dTree = new DomTree(context);
            downloadLinkNodes = dTree.ExtractContainerLinkNodes(limitChildDepth);
        }

        /// <summary>
        /// 取得解析出來的下載清單
        /// </summary>
        /// <returns></returns>
        public List<Tuple<string, string>> GetDownloadList(bool isReversed = false)
        {
            var result = downloadLinkNodes?
                .Select(n => new Tuple<string, string>(n.Attributes["href"].Value, n.InnerText));

            return isReversed ? result?.Reverse().ToList() : result?.ToList();
        }

        #endregion

        #region HttpRequest抓取網頁內容

        /// <summary>
        /// 將輸出的字串轉為C#的Encoding頁碼
        /// 轉換參照表：http://msdn.microsoft.com/zh-tw/library/system.text.encoding%28v=vs.110%29.aspx
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static int ConvertToCorrectEncodingString(string str)
        {
            int result;

            switch (str)
            {
                case Ude.Charsets.ASCII:
                    {
                        result = 20127;
                        break;
                    }
                case Ude.Charsets.BIG5:
                    {
                        result = 950;
                        break;
                    }
                case Ude.Charsets.UTF8:
                    {
                        result = 65001;
                        break;
                    }
                case Ude.Charsets.UTF16_LE:
                    {
                        result = 1201;
                        break;
                    }
                case Ude.Charsets.UTF16_BE:
                    {
                        result = 1200;
                        break;
                    }
                case Ude.Charsets.UTF32_BE:
                    {
                        result = 12001;
                        break;
                    }
                case Ude.Charsets.UTF32_LE:
                    {
                        result = 12000;
                        break;
                    }
                case Ude.Charsets.EUCKR:
                    {
                        result = 51949;
                        break;
                    }
                case Ude.Charsets.EUCJP:
                    {
                        result = 20932;
                        break;
                    }
                case Ude.Charsets.GB18030:
                    {
                        result = 54936;
                        break;
                    }
                case Ude.Charsets.ISO2022_JP:
                    {
                        result = 50222;
                        break;
                    }
                case Ude.Charsets.ISO2022_CN:
                    {
                        result = 50227;
                        break;
                    }
                case Ude.Charsets.ISO2022_KR:
                    {
                        result = 50225;
                        break;
                    }
                case Ude.Charsets.HZ_GB_2312:
                    {
                        result = 52936;
                        break;
                    }
                case Ude.Charsets.SHIFT_JIS:
                    {
                        result = 932;
                        break;
                    }
                case Ude.Charsets.KOI8R:
                    {
                        result = 20866;
                        break;
                    }
                case Ude.Charsets.ISO8859_2:
                    {
                        result = 28592;
                        break;
                    }
                case Ude.Charsets.ISO8859_5:
                    {
                        result = 28595;
                        break;
                    }
                case Ude.Charsets.ISO_8859_7:
                    {
                        result = 28597;
                        break;
                    }
                case Ude.Charsets.ISO8859_8:
                    {
                        result = 28598;
                        break;
                    }
                case Ude.Charsets.MAC_CYRILLIC:
                    {
                        result = 10007;
                        break;
                    }
                case Ude.Charsets.WIN1251:
                    {
                        result = 1251;
                        break;
                    }
                case Ude.Charsets.WIN1252:
                    {
                        result = 1252;
                        break;
                    }
                case Ude.Charsets.WIN1253:
                    {
                        result = 1253;
                        break;
                    }
                case Ude.Charsets.WIN1255:
                    {
                        result = 1255;
                        break;
                    }
                case Ude.Charsets.IBM855:
                    {
                        result = 855;
                        break;
                    }
                case Ude.Charsets.IBM866:
                    {
                        result = 866;
                        break;
                    }
                default:
                    {
                        result = 65001;
                        break;
                    }
            }

            return result;
        }

        /// <summary>
        /// 檢查Url的正確性
        /// </summary>
        /// <param name="url">URL</param>
        /// <returns>正確與否</returns>
        public static bool ExaminedUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }

        /// <summary>
        /// 用第三方元件"Ude"去解讀該文件的正確編碼方式
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private static Encoding GetUdeEncode(Stream context)
        {
            Encoding result = null;

            var detector = new Ude.CharsetDetector();
            detector.Feed(context);
            detector.DataEnd();
            if (detector.Charset != null)
            {
                result = Encoding.GetEncoding(ConvertToCorrectEncodingString(detector.Charset));
            }
            else
            {
                //沒有值視為沒取到
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="highPriorityEncode"></param>
        /// <param name="url"></param>
        /// <param name="miniSecondTimeout"></param>
        /// <param name="errorCode"></param>
        /// <param name="errorMessage"></param>
        /// <param name="redirectUrl"></param>
        /// <returns></returns>
        public static string GetHtmlContent(
            Dictionary<int, Encoding> highPriorityEncode,
            string url,
            int miniSecondTimeout,
            out int errorCode,
            out string errorMessage,
            out string redirectUrl)
        {
            //初始
            redirectUrl = string.Empty;
            errorMessage = string.Empty;
            errorCode = 0;
            var result = string.Empty;

            //檢查URL正確性
            if (!ExaminedUrl(url))
            {
                //URI不正確 擲回Exception
                errorMessage = "URL格式不正確";
                errorCode = 800000;
                return result;
            }

            //初始Http請求物件
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Timeout = miniSecondTimeout;
            httpWebRequest.ReadWriteTimeout = miniSecondTimeout;
            httpWebRequest.Method = "get";
            httpWebRequest.ContentType = "text/xml"; //"application/x-www-form-urlencoded"; <= post

            //模擬有cookie的抓取
            var cookie = new CookieContainer();
            httpWebRequest.CookieContainer = cookie;

            //假裝我是以下瀏覽器，這個網站除了瀏覽器 還包含一堆RSS的reader header
            //UserAgent List可參考http://www.useragentstring.com/pages/useragentstring.php
            //HttpWReq.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
            httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.15 (KHTML, like Gecko) Chrome/24.0.1295.0 Safari/537.15";
            //HttpWReq.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:25.0) Gecko/20100101 Firefox/25.0";

            //因為有的Server會擋 HttpWebRequest，因為會被認為是BOT機器人，所以必須加上這個屬性才可以過關
            httpWebRequest.Accept = "text/html";

            //把這個值設為false，測試看看被擋的機率會不會下降
            httpWebRequest.KeepAlive = false;

            try
            {
                using (var myHttpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    //取得跳轉網址
                    redirectUrl = myHttpWebResponse.ResponseUri.OriginalString;

                    //取得header的編碼
                    var defaultEncodingString = string.Empty;
                    if (!string.IsNullOrEmpty(myHttpWebResponse.CharacterSet))
                    {
                        //因為會有"UTF-8,text/html"的CharacterSet 所以要.Split(',')[0]
                        defaultEncodingString = myHttpWebResponse.CharacterSet
                            .Split(',')[0].Trim().Replace("\'", string.Empty)
                            .Replace("\"", string.Empty);
                    }
                    else
                    {
                        //如果取不到header中的合法 encode，就先放空
                    }

                    //網頁回傳結果使用 stream去接
                    using (var myStream = myHttpWebResponse.GetResponseStream())
                    {
                        //pump ping方式接收
                        var buf = new byte[256];
                        using (var ms = new MemoryStream())
                        {
                            int size;
                            do
                            {
                                size = myStream?.Read(buf, 0, buf.Length) ?? 0;
                                ms.Write(buf, 0, size);

                            } while (size > 0);

                            #region 判斷編碼

                            //重置MemoryStream的位置，不然Ude會取不到串流
                            ms.Position = 0;
                            Encoding targetEncode;

                            var defaultEncode = !string.IsNullOrEmpty(defaultEncodingString) ?
                                Encoding.GetEncoding(defaultEncodingString) :
                                Encoding.ASCII;

                            var udeEncode = GetUdeEncode(ms) ?? Encoding.ASCII;

                            //決定編碼
                            if (udeEncode.CodePage == defaultEncode.CodePage)
                            {
                                //debugMessage.Add("Server與Ude編碼一致");
                                //如果兩個編碼一致，則默認為正確
                                targetEncode = udeEncode;
                            }
                            else
                            {
                                //如果不一致，則去檢查兩個編碼是否存在於高優先權編碼字典
                                if (highPriorityEncode.ContainsKey(defaultEncode.CodePage))
                                {
                                    //"Server給的charset是存在於優先字元集"
                                    targetEncode = defaultEncode;
                                }
                                else if (highPriorityEncode.ContainsKey(udeEncode.CodePage))
                                {
                                    //"Ude給的charset是存在於優先字元集"
                                    targetEncode = udeEncode;
                                }
                                else
                                {
                                    //如果沒有則用Ude編碼
                                    targetEncode = udeEncode;
                                }
                            }

                            #endregion

                            //如果編碼還是不合適，就會出現亂碼
                            result = targetEncode.GetString(ms.ToArray());
                        }
                    }

                    return result;
                }
            }
            catch (WebException ex)
            {
                //網頁讀取錯誤
                //錯誤要導出WebException
                errorMessage = ex.ToString();
                if (ex.Response != null)
                {
                    //若能導出404 等status code 就要導出
                    errorCode = (int)((HttpWebResponse)ex.Response).StatusCode;
                }
                else
                {
                    errorCode = 800001;
                }
                return result;
            }
            catch (Exception ex)
            {
                //重大錯誤
                errorMessage = ex.ToString();
                errorCode = 800800;
                return result;
            }
        }

        #endregion
    }
}

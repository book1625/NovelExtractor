using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ContentExtractor
{
    /// <summary>
    /// 一個目標網頁的捉取
    /// </summary>
    public class FetchItem
    {
        public FetchItem()
        {
            //以下初始化優先處理的編碼

            //big5
            //fHighPriorityEncode.Add(950, Encoding.GetEncoding(950));

            //GB18030 簡體中文
            //fHighPriorityEncode.Add(54936, Encoding.GetEncoding(54936));

            //UTF 8
            fHighPriorityEncode.Add(65001, Encoding.GetEncoding(65001));
        }

        /// <summary>
        /// 優先編碼字典
        /// </summary>
        private readonly Dictionary<int, Encoding> fHighPriorityEncode = new Dictionary<int, Encoding>();

        /// <summary>
        /// 當前捉取的資料
        /// </summary>
        private readonly List<HtmlNode> fCurrNodes = new List<HtmlNode>();

        /// <summary>
        /// 目標 url
        /// </summary>
        public string Url
        {
            get;
            set;
        }

        /// <summary>
        /// 解網頁的關鍵字
        /// </summary>
        public string Keyword
        { 
            get; 
            set; 
        }

        /// <summary>
        /// 是否成功取得
        /// </summary>
        public bool IsFetched => fCurrNodes.Count > 0;

        /// <summary>
        /// 記下取得結果
        /// </summary>
        private string fFetchedString = "";

        /// <summary>
        /// 取得輸出用字串
        /// </summary>
        /// <returns></returns>
        public string GetFilterString()
        {
            //過濾 HTML 專屬字元
            var result = fFetchedString.Replace("&nbsp;", " ").Replace("&quot;", @"""").Replace("&gt;", ">").Replace("@lt;", "<");

            //過濾 不處理的 HTML 專屬字元
            Regex regx = new Regex("&[a-z]+;");
            result = regx.Replace(result, "");
            
            //過濾不支援的語言編碼
            regx = new Regex("&#[0-9]+;");
            result = regx.Replace(result, "#");

            return result;
        }

        /// <summary>
        /// 運行工作項目
        /// </summary>
        /// <param name="status">回傳輸出用狀態</param>
        /// <param name="fetchString">回傳輸出用的取得結果</param>
        public void Process( out string status, out string fetchString)
        {
            var context = HttpGetHtmlContent(fHighPriorityEncode, this.Url, 20000, out int errorCode, out string errorMessage, out string redirectUrl);

            var dTree = new DomTree(context, "", "");

            //抽出指定的 dom tree node
            var temp = dTree.ExtractTargetNodes(Keyword);
            fCurrNodes.AddRange(temp);

            //組建一個 node 所有文字，包含頭尾追加空白行
            var sb = new StringBuilder();
            foreach (var node in temp)
            {   
                sb.Append(Environment.NewLine);
            
                //合併目標文字
                sb.Append(node.InnerText);
            }

            sb.Append(Environment.NewLine);

            fetchString = sb.ToString();
            this.fFetchedString = fetchString;
            status = $"取得{temp.Count}個節點 Url={this.Url}";
        }

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
        /// <param name="Url">URL</param>
        /// <returns>正確與否</returns>
        public static bool ExaminedUrl(string Url)
        {
            Uri examinedURL;
            return Uri.TryCreate(Url, UriKind.Absolute, out examinedURL);
        }

        /// <summary>
        /// 用第三方元件"Ude"去解讀該文件的正確編碼方式
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private static Encoding GetUdeEncode(Stream context)
        {
            Encoding result = null;

            Ude.CharsetDetector detector = new Ude.CharsetDetector();
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
        public static string HttpGetHtmlContent(
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
            string result = string.Empty;

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
                            Encoding targetEncode = null;
                            Encoding defaultEncode = null;

                            defaultEncode = !string.IsNullOrEmpty(defaultEncodingString) ? 
                                Encoding.GetEncoding(defaultEncodingString) : Encoding.ASCII;

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

using ContentExtractor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace ContentExtractorTests
{
    [TestClass]
    public class FetchItemTests
    {
        [TestMethod]

        //這些目前已捉的到了 宙斯，黃金屋，找書苑
        [DataRow("http://tw.zhsxs.com/zhsread/62696_23506224.html", "第八十九章 癡情人都不得好死！", "青冥大尊咬咬牙，心中喃喃道；「老友，對不住了！＂", true)]
        [DataRow("https://tw.hjwzw.com/Book/Read/1644,409280", "第一章 山邊小村", "走出了自己的修仙之路")]
        [DataRow("https://tw.zhaoshuyuan.com/b/86613/298521.html#gsc.tab=0", "第1793章 重生唐3（下）", "后面還有后記，大家記得看。之后就要正式開啟咱們的《斗羅大陸V重生唐三》了！", true)]

        //不確定為何一直逾時，頁面是可以開的
        [DataRow("https://tw.hjwzw.com/Book/Read/37865,22703659", "第430章 非南之行(109)", "葉凌月和辛霖陷入了沉默，這一點，她們也想到了", false)]

        //這個，需要調整找內文的算法，因為它找錯節點
        [DataRow("https://www.ptwxz.com/html/3/3259/4046016.html", "第四十五卷 第十七章 梅花香（大结局下）", "再次见面就是新书的世界了，6月15号再见喽", false)]

        //這個會把一章又切兩頁，目前沒有處理它的功能
        [DataRow("https://m.wfxs.tw/xs-32407/du-9205524/", "第1章 王者醒來", "蕭峰冷淡的叫了一句", false)]

        public void FetchValidContext(string url, string title, string excepted, bool exceptedResult)
        {
            var item = new PageFetchItem()
            {
                Url = url,
                Title = title
            };

            item.ParseTextContext();

            var result = item.GetContext();

            Assert.IsTrue(item.IsFetched);
            Assert.IsTrue(result.Count(s => s.Contains(excepted)) > 0);
        }

        [TestMethod]
        [DataRow("https://tw.hjwzw.com/Book/Read/34810,13311088","章一一八 明珠蒙塵", "會議結束，千夜剛剛回到書房", true)]

        public void CheckContextForHtml(string url, string title, string excepted, bool exceptedResult)
        {
            var item = new PageFetchItem()
            {
                Url = url,
                Title = title
            };

            item.ParseTextContext();

            var result = item.GetContext();

            Assert.IsTrue(item.IsFetched);
            Assert.IsTrue(result.Count(s => s.StartsWith(excepted)) > 0);
            Assert.IsTrue(result.Count(s => s.Contains("&nbsp")) == 0);
            Assert.IsTrue(result.Count(s => s.Contains("&quot;")) == 0);
            Assert.IsTrue(result.Count(s => s.Contains("&gt;")) == 0);
            Assert.IsTrue(result.Count(s => s.Contains("@lt;")) == 0);
            Assert.IsTrue(result.Count(s => s.Contains("&nbsp;")) == 0);
        }

        [TestMethod]
        [DataRow("http://tw.zhsxs.com/zhschapter/35209.html",5,false)]
        [DataRow("https://tw.hjwzw.com/Book/Chapter/1661",5,false)]
        [DataRow("https://tw.uukanshu.com/b/33933/",5,true)]
        public void FetchValidDownloadList(string url, int limitChildDepth, bool isReversed)
        {
            var item = new PageFetchItem()
            {
                Url = url
            };

            item.ParseDownloadList(limitChildDepth);
            var list = item.GetDownloadList(isReversed);
            Assert.IsTrue(list.Count > 1000);
        }
    }
}

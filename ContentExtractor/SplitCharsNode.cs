using HtmlAgilityPack;

namespace ContentExtractor
{
    /// <summary>
    /// 有分割字元的節點物件
    /// </summary>
    public class SplitCharsNode
    {
        /// <summary>
        /// 節點
        /// </summary>
        public HtmlNode Node { get; set; }

        /// <summary>
        /// 內文斷句數
        /// </summary>
        public int SplitCount { get; set; }

        /// <summary>
        /// 內文斷句在innerLength的比例
        /// </summary>
        public double SentenceProportion { get; set; }
    }
}

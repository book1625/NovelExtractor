using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ContentExtractor
{
    public class DomTree
    {
        #region 常數

        /// <summary>
        /// 用來分割內文的字元組
        /// </summary>
        private readonly char[] _splitChars = new char[] { '，', '。', ',', '.', '?', '!' };

        /// <summary>
        /// 用來分割內文的字串組
        /// </summary>
        private readonly string[] _splitStrings = new string[] { "<p>", "<br>", "</p>", "</br>" };

        #endregion

        #region 變數

        /// <summary>
        /// 建立HtmlAgaility的html物件 ( Dom Tree )
        /// </summary>
        public HtmlDocument HtmlDoc
        {
            get;
            private set;
        }

        ///// <summary>
        ///// 儲存有內文的節點 (儲存Div, Table, Section節點)
        ///// </summary>
        //public List<HtmlNode> TarTagDataList
        //{
        //    get;
        //    private set;
        //} = new List<HtmlNode>();

        /// <summary>
        /// 儲存所有有分割字元的節點(',' '.' etc...)
        /// </summary>
        public List<SplitCharsNode> HasSplitCharsNodeList
        {
            get;
            private set;
        }

        /// <summary>
        /// 儲存最大內文節點 (OuterHtml)
        /// </summary>
        public HtmlNode MaxOuterHtmlNode
        {
            get;
            private set;
        }

        /// <summary>
        /// 儲存最大內文節點 (InnerText)
        /// </summary>
        public HtmlNode MaxInnerTextNode
        {
            get;
            private set;
        }

        /// <summary>
        /// 傳入的原始資料
        /// </summary>
        public string InputHtmlData
        {
            get;
            private set;
        }

        #endregion

        /// <summary>
        /// 存所有的FormNode
        /// </summary>
        private readonly List<HtmlNode> _formNodeList = new List<HtmlNode>();

        #region 建構子

        /// <summary>
        /// 以原始資料建立DomTree
        /// </summary>
        /// <param name="inputHtmlData">傳入的Html資料</param>
        public DomTree(string inputHtmlData)
        {
            InputHtmlData = inputHtmlData;

            //建立HtmlAgaility的html物件
            HtmlDoc = new HtmlDocument();

            //載入資料，成為Dom Tree
            HtmlDoc.LoadHtml(InputHtmlData);
        }

        /// <summary>
        /// 刪除指定資料後 建立DomTree
        /// </summary>
        /// <param name="inputHtmlData">傳入的Html資料</param>
        /// <param name="removeTagString">移除特定的Tag清單</param>
        /// <param name="removePatternString">移除特定的Pattern</param>
        public DomTree(string inputHtmlData, string removeTagString = "", string removePatternString = "")
        {
            InputHtmlData = inputHtmlData;

            #region 移除特定的Pattern

            var fAfterRemoveHtmlData = removePattern(InputHtmlData, removePatternString);

            #endregion

            #region 以移除特定Pattern後的資料建立 Dom Tree
            HtmlDoc = new HtmlDocument();
            HtmlDoc.LoadHtml(fAfterRemoveHtmlData);
            #endregion

            #region  移除註解

            removeComments();

            #endregion

            #region  移除特定的Tag清單

            removeTags(removeTagString.Replace(" ", "").ToLower().Split(',').ToList());

            #endregion
        }

        #endregion       

        #region 建構物件時 移除傳入資料中指定的Tag , Pattern...etc

        /// <summary>
        /// 移除註解 (HtmlDocument)
        /// </summary>
        /// <returns></returns>
        private HtmlDocument removeComments()
        {
            if (HtmlDoc == null) return HtmlDoc;

            var selectNode = HtmlDoc.DocumentNode.SelectNodes("//comment()");
            if (selectNode == null) return HtmlDoc;

            foreach (var comment in selectNode)
            {
                comment.ParentNode.RemoveChild(comment);
            }

            return HtmlDoc;
        }

        /// <summary>
        /// 移除指定的Tag列表 (HtmlDocument)    (將formNode存到List  所有node被濾掉時使用(暫時測試))
        /// </summary>
        /// <param name="tagList"></param>
        /// <returns></returns>
        private void removeTags(List<string> tagList)
        {
            if (HtmlDoc == null) return;

            var x = HtmlDoc.DocumentNode.DescendantsAndSelf().ToList();

            //重要---必須用此方式改變參數設定  form才會有childNode
            //HtmlAgilityPack是針對HTML 3.2的規範，而HTML 3.2就是規定。 對於option，form等tag，其默認處理的结果是：其下的子節點，會變成sibling   2013/8/8  Jerry
            HtmlNode.ElementsFlags.Remove("form");

            foreach (var tag in x)
            {
                if (tag.NodeType != HtmlNodeType.Element) continue;

                if (!tagList.Contains(tag.Name)) continue;

                //如果是formNode的話存到list  所有node被濾掉時使用
                if (tag.Name == "form")
                {
                    _formNodeList.Add(tag);
                }

                tag.Remove();
            }

        }

        /// <summary>
        /// 移除特定的Pattern
        /// </summary>
        /// <param name="inputString"></param>
        /// <param name="rePa"></param>
        /// <returns></returns>
        private string removePattern(string inputString, string rePa)
        {
            var pa = rePa.Split(',').ToList();

            foreach (var s in pa)
            {
                if (s.Length > 0)
                {
                    inputString = inputString.Replace(s, "");
                }
            }
            return inputString;
        }

        #endregion

        #region 解析下載清單

        /// <summary>
        /// 更通用的取得「擁有最多有效連結(a[href])」的容器下所有連結節點。
        /// 可客製容器標籤、深度限制與連結過濾。
        /// </summary>
        /// <param name="maxContainerDepth">候選容器的最大深度 (root 為 0)</param>
        /// <param name="candidateContainerTags">可視為容器的標籤集合 (預設: div, table, section, ul, ol, article, main)</param>
        /// <param name="anchorFilter">連結過濾條件 (預設: 有非空 href 的 a)</param>
        /// <returns>最佳容器底下的連結節點集合；若無合適容器，回傳整體文件連結或空集合</returns>
        public List<HtmlNode> ExtractContainerLinkNodes(
            int maxContainerDepth = 10,
            IEnumerable<string> candidateContainerTags = null,
            Func<HtmlNode, bool> anchorFilter = null)
        {
            if (HtmlDoc?.DocumentNode == null) return new List<HtmlNode>();

            var tags = new HashSet<string>(
               (candidateContainerTags ?? new[] { "div", "table", "section", "ul", "ol", "article", "main" })
               .Select(t => t.ToLowerInvariant())
           );

            anchorFilter = anchorFilter ?? (a =>
                a.Name.Equals("a", StringComparison.OrdinalIgnoreCase) &&
                a.Attributes["href"] != null &&
                !string.IsNullOrWhiteSpace(a.GetAttributeValue("href", "").Trim()));

            var stack = new Stack<(HtmlNode node, int depth)>();
            stack.Push((HtmlDoc.DocumentNode, 0));

            // 暫存所有候選容器評分
            var candidates = new List<ContainerScore>();

            while (stack.Count > 0)
            {
                var (node, depth) = stack.Pop();

                // 深度優先 (可改為廣度，但結果無影響評分)
                foreach (var child in node.ChildNodes.Where(c => c.NodeType == HtmlNodeType.Element))
                {
                    stack.Push((child, depth + 1));
                }

                if (node.NodeType != HtmlNodeType.Element) continue;
                if (depth > maxContainerDepth) continue;

                var name = node.Name.ToLower();
                if (!tags.Contains(name)) continue;

                // 取得連結
                var anchorNodes = node.Descendants("a").Where(anchorFilter).ToList();
                var anchorCount = anchorNodes.Count;
                if (anchorCount == 0) continue;

                // 取得容器規模與密度
                var totalDescendantsElementCount =
                    node.Descendants().Count(d => d.NodeType == HtmlNodeType.Element);

                double density = totalDescendantsElementCount > 0
                    ? (double)anchorCount / totalDescendantsElementCount
                    : 0.0;

                candidates.Add(new ContainerScore
                {
                    Container = node,
                    Anchors = anchorNodes,
                    AnchorCount = anchorCount,
                    Density = density,
                    Depth = depth,
                    OuterLength = node.OuterHtml?.Length ?? 0
                });
            }

            if (candidates.Count == 0)
            {
                // 沒有找到合適容器，回傳整體的連結 (仍套用 anchorFilter)
                return HtmlDoc.DocumentNode
                              .Descendants("a")
                              .Where(anchorFilter)
                              .ToList();
            }

            // 排序規則：
            // 1. 連結數最多
            // 2. 密度最高
            // 3. OuterHtml 長度較大 (避免極小但密度高的容器)
            // 4. 深度較淺 (靠近上層)
            var best = candidates
                .OrderByDescending(c => c.AnchorCount)
                .ThenByDescending(c => c.Density)
                .ThenByDescending(c => c.OuterLength)
                .ThenBy(c => c.Depth)
                .First();

            return best.Anchors;
        }

        /// <summary>
        /// 內部暫存容器評分資料結構
        /// </summary>
        private class ContainerScore
        {
            public HtmlNode Container { get; set; }
            public List<HtmlNode> Anchors { get; set; }
            public int AnchorCount { get; set; }
            public double Density { get; set; }
            public int Depth { get; set; }
            public int OuterLength { get; set; }
        }

        #endregion

        #region 解析最大本文

        /// <summary>
        /// 初始物件資料 
        /// </summary>
        public void InitMaxOuterHtmlAndMaxInnerTextNodeV2()
        {
            MaxOuterHtmlNode = null;
            MaxInnerTextNode = null;

            HasSplitCharsNodeList = new List<SplitCharsNode>();

            //取得有分割字元結點的List
            GetSplitCharsNodeList(HtmlDoc.DocumentNode.DescendantsAndSelf().ToList());

            if (HasSplitCharsNodeList.Count > 0)
            {
                GetMainTextNodeByRealSplitCount();
            }
            else
            {
                //看有沒有被濾掉的FormNode
                if (_formNodeList != null && _formNodeList.Count > 0)
                {
                    var mainFormNode = (from n in _formNodeList
                                        orderby n.XPath.Length ascending
                                        select n).ToList().FirstOrDefault();

                    var formTagNodeList = mainFormNode.DescendantsAndSelf().ToList();
                    GetSplitCharsNodeList(formTagNodeList);

                    if (HasSplitCharsNodeList.Count > 0)
                    {
                        GetMainTextNodeByRealSplitCount();
                    }
                }
            }
        }

        /// <summary>
        /// 取得有分割字元節點的List
        /// </summary>
        /// <param name="tempAllTagNodeList"></param>
        private void GetSplitCharsNodeList(List<HtmlNode> tempAllTagNodeList)
        {
            foreach (var tagNode in tempAllTagNodeList)
            {
                //只需要作Element型態的即可
                if (tagNode.NodeType != HtmlNodeType.Element) continue;

                var tempTagName = tagNode.Name.ToLower();

                if ((tempTagName != "table") && (tempTagName != "div") && (tempTagName != "section")) continue;

                //找出最大內文節點
                if (MaxOuterHtmlNode == null || tagNode.OuterHtml.Length > MaxOuterHtmlNode.OuterHtml.Length)
                {
                    MaxOuterHtmlNode = tagNode;
                }

                var inn = tagNode.InnerText;

                //Html的段落字元數 計算<p> <br> 的次數
                var htmlPartStringCount = _splitStrings
                    .Select(s => new Regex(s))
                    .Select(r => r.Matches(tagNode.InnerHtml).Count)
                    .Sum();

                //計算有幾個分割字元
                var splitCount = inn.Split(_splitChars).Length;

                //分割字元與Html的段落字元數相加
                splitCount += htmlPartStringCount;

                //如果有分割字元
                if (splitCount <= 1) continue;

                //內文斷句在innerLength的比例
                var tempSentenceProportion = ((double)splitCount / inn.Length);

                //存入List
                HasSplitCharsNodeList.Add(new SplitCharsNode() { Node = tagNode, SentenceProportion = tempSentenceProportion, SplitCount = splitCount });
            }
        }

        /// <summary>
        /// 每個節點去除子樹的斷句字元數 取得屬於本身的斷句字元數 選出斷句數最多的
        /// </summary>
        private void GetMainTextNodeByRealSplitCount()
        {

            //排序 從上層節點開始
            var realNode =
                from n in HasSplitCharsNodeList
                orderby n.Node.XPath.Length ascending
                select n;

            foreach (var spNode in realNode)
            {
                //選出同棵樹中最接近的父代節點
                var checkLastNode = (
                    from n in HasSplitCharsNodeList
                    where n.Node != spNode.Node && spNode.Node.XPath.Contains(n.Node.XPath)
                    orderby n.Node.XPath.Length descending
                    select n).FirstOrDefault();

                if (checkLastNode != null)
                {
                    checkLastNode.SplitCount -= spNode.SplitCount;
                }
            }

            MaxInnerTextNode = (
                from n in HasSplitCharsNodeList
                orderby n.Node.InnerText.Length descending
                select n.Node).FirstOrDefault();
        }

        #endregion        
    }
}

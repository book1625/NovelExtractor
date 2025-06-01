using HtmlAgilityPack;
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

        /// <summary>
        /// 試圖取得 id 包含指定關鍵字的節點
        /// </summary>
        /// <param name="tarKeyWord">指定關鍵字</param>
        /// <returns>找到的節點</returns>
        public List<HtmlNode> ExtractTargetNodes(string tarKeyWord)
        {
            var x = HtmlDoc.DocumentNode.DescendantsAndSelf().Where(n => !string.IsNullOrEmpty(n.Id));
            return x.Where(node => node.Id.Contains(tarKeyWord)).ToList();
        }

        /// <summary>
        /// 抽取最大張表格的所有連接
        /// </summary>
        /// <returns></returns>
        public List<HtmlNode> ExtractBigTableLinkNodes(int limitChildDepth)
        {
            var maxCount = 0;
            HtmlNode maxNode = null;

            foreach (var tagNode in HtmlDoc.DocumentNode.Descendants())
            {
                if (tagNode.NodeType != HtmlNodeType.Element) continue;
                var tempTagName = tagNode.Name.ToLower();
                if (!(tempTagName == "table" || tempTagName == "div")) continue;

                if (GetChildDepth(tagNode, 1) > limitChildDepth) continue;

                if (maxNode is null)
                {
                    maxNode = tagNode;
                    maxCount = tagNode.Descendants().Count();
                }
                else
                {
                    if (maxCount >= tagNode.Descendants().Count()) continue;
                    maxNode = tagNode;
                    maxCount = tagNode.Descendants().Count();
                }
            }

            return maxNode?.Descendants()
                .Where(n => n.NodeType == HtmlNodeType.Element && n.Name.ToLower() == "a")
                .ToList();
        }

        private static int GetChildDepth(HtmlNode node, int maxDepth)
        {
            if (node.ChildNodes.Count == 0) return maxDepth;

            var max = maxDepth;

            foreach (var nodeChildNode in node.ChildNodes)
            {
                var temp = GetChildDepth(nodeChildNode, maxDepth + 1);
                if (temp > max) max = temp;
            }

            return max;
        }

        #region v2 輸入資料轉成Dom Tree後，針對特定Tag節點作處理，取內文分割字元最多的節點

        /// <summary>
        /// 初始物件資料 
        /// </summary>
        public void InitMaxOuterHtmlAndMaxInnerTextNodeV2(string tarId=null)
        {
            MaxOuterHtmlNode = null;
            MaxInnerTextNode = null;

            if (string.IsNullOrEmpty(tarId))
            {
                HasSplitCharsNodeList = new List<SplitCharsNode>();

                //取得有分割字元結點的List
                GetSplitCharsNodeList(HtmlDoc.DocumentNode.DescendantsAndSelf().ToList());

                if (HasSplitCharsNodeList.Count > 0)
                {
                    getMainTextNodeByRealSplitCount();
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
                            getMainTextNodeByRealSplitCount();
                        }
                    }
                }
            }
            else
            {
                //有指定名字，直接靠名字找到節點
                MaxInnerTextNode = HtmlDoc.DocumentNode.DescendantsAndSelf().ToList().FirstOrDefault(n => n.Id == tarId);
            }
        }

        #endregion

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

        #region 選取內文結點的所有方法模組

        /// <summary>
        /// 以每個node中選出斷句長度在內文長度中比例最高的
        /// </summary>
        private void getMainTextNodeBySentenceProportion()
        {
            var tempSplitCount = (from n in HasSplitCharsNodeList
                                  orderby n.SplitCount descending
                                  select n.SplitCount).FirstOrDefault();

            //內文斷句在innerLength的比例
            double sentenceProportion = 0;

            foreach (var spNode in HasSplitCharsNodeList)
            {
                //將最多分割字元的節點存入   (斷句長度在最大長度的1/3)
                if (spNode.SplitCount > tempSplitCount / 3)
                {

                    if (spNode.SentenceProportion > sentenceProportion)
                    {
                        sentenceProportion = spNode.SentenceProportion;
                        MaxInnerTextNode = spNode.Node;
                    }
                }
            }
        }

        /// <summary>
        /// 在有分割字元的nodeList中選出child node不再List裡的做斷句長度比較
        /// </summary>
        private void getMainTextNodeByNoChildNode()
        {
            var tempSplitCount = 0;
            //內文斷句在innerLength的比例
            //double sentenceProportion = 0;

            foreach (var spNode in HasSplitCharsNodeList)
            {
                var checkParentNode = (
                    from n in HasSplitCharsNodeList
                    where n.Node != spNode.Node && n.Node.XPath.Contains(spNode.Node.XPath)
                    select n).FirstOrDefault();


                if (checkParentNode != null) continue;
                if (spNode.SplitCount <= tempSplitCount) continue;

                tempSplitCount = spNode.SplitCount;
                MaxInnerTextNode = spNode.Node;
            }
        }

        /// <summary>
        /// 每個節點去除子樹的斷句字元數 取得屬於本身的斷句字元數 選出斷句數最多的
        /// </summary>
        private void getMainTextNodeByRealSplitCount()
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

        #region 建構物件時 移除傳入資料中指定的Tag , Pattern...etc

        #region 移除註解

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

        #endregion

        #region 移除指定的Tag列表

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

        #endregion

        #region 移除特定的Pattern

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

        #endregion

        #region 取出指定Tag底下的所有節點(沒用到，先保留)

        /// <summary>
        /// 取出指定Tag底下的所有節點
        /// </summary>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public List<HtmlNode> GetTagWithName(string tagName)
        {
            var resultAll = HtmlDoc.DocumentNode
                                    .Descendants(tagName)
                                    .Select(n => n).ToList();

            return resultAll;
        }

        #endregion

        #region 取出Head第一層指定Tag的所有節點

        /// <summary>
        /// 取出Head第一層指定Tag的所有節點
        /// </summary>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public List<HtmlNode> GetTagListFromHead(string tagName, List<string> attribute, List<string> value)
        {

            var resultAll = new List<HtmlNode>();

            for (var a = 1; a < 20; a++)
            {
                var s = string.Format("//head/{0}[{1}]", tagName, a);

                if (HtmlDoc.DocumentNode.SelectNodes(s) != null)
                {
                    resultAll.Add(HtmlDoc.DocumentNode.SelectNodes(s)[0]);
                }
                else
                {
                    break;
                }
            }

            var result = new List<HtmlNode>();

            if ((attribute.Count > 0) && (attribute[0].Length > 0))
            {
                foreach (var x in resultAll)
                {
                    foreach (var y in attribute)
                    {
                        if (x.Attributes[y] != null)
                        {
                            var esp = false;

                            if ((value.Count > 0) && (value[0].Length > 0))
                            {
                                foreach (var z in value)
                                {
                                    if (x.Attributes[y].Value == z)
                                    {
                                        result.Add(x);

                                        esp = true;

                                        break;
                                    }
                                }
                            }
                            else
                            {
                                result.Add(x);

                                esp = true;
                            }

                            if (esp == true)
                            {
                                break;
                            }

                        }
                    }
                }
            }
            else
            {
                foreach (var x in resultAll)
                {
                    result.Add(x);
                }
            }

            return result;
        }

        #endregion

        #region 取出body第一層指定Tag的所有節點(沒用到，先保留)

        /// <summary>
        /// 取出body第一層指定Tag的所有節點
        /// </summary>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public List<HtmlNode> GetTagList(string tagName, List<string> attribute, List<string> value)
        {

            var resultAll = new List<HtmlNode>();

            for (var a = 1; a < 20; a++)
            {
                var s = string.Format("//body/{0}[{1}]", tagName, a);

                if (HtmlDoc.DocumentNode.SelectNodes(s) != null)
                {

                    resultAll.Add(HtmlDoc.DocumentNode.SelectNodes(s)[0]);
                }
                else
                {
                    break;
                }
            }

            var result = new List<HtmlNode>();

            if ((attribute.Count > 0) && (attribute[0].Length > 0))
            {
                foreach (var x in resultAll)
                {
                    foreach (var y in attribute)
                    {
                        if (x.Attributes[y] != null)
                        {
                            var esp = false;

                            if ((value.Count > 0) && (value[0].Length > 0))
                            {
                                foreach (var z in value)
                                {
                                    if (x.Attributes[y].Value == z)
                                    {
                                        result.Add(x);

                                        esp = true;

                                        break;
                                    }
                                }
                            }
                            else
                            {
                                result.Add(x);

                                esp = true;
                            }

                            if (esp == true)
                            {
                                break;
                            }

                        }
                    }
                }
            }
            else
            {
                foreach (var x in resultAll)
                {
                    result.Add(x);
                }
            }


            return result;
        }

        #endregion
    }
}

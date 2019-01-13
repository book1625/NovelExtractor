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
        private char[] SPLIT_CHARS = new char[] { '，', '。', ',', '.', '?', '!' };

        /// <summary>
        /// 用來分割內文的字串組
        /// </summary>
        private string[] SPLIT_STRINGS = new string[] { "<p>", "<br>", "</p>", "</br>" };

        #endregion

        #region 變數

        /// <summary>
        /// 建立HtmlAgaility的html物件 ( Dom Tree )
        /// </summary>
        private HtmlAgilityPack.HtmlDocument fHtmlDoc;

        /// <summary>
        /// 建立HtmlAgaility的html物件 ( Dom Tree )
        /// </summary>
        public HtmlAgilityPack.HtmlDocument HtmlDoc
        {
            get
            {
                return fHtmlDoc;
            }
        }

        /// <summary>
        /// 儲存所有有圖的節點
        /// </summary>
        private List<HtmlNode> fImgFileCandidateList = new List<HtmlNode>();

        /// <summary>
        /// 儲存所有有圖的節點
        /// </summary>
        public List<HtmlNode> ImgFileCandidateList
        {
            get
            {
                return fImgFileCandidateList;
            }
        }

        /// <summary>
        /// 儲存有內文的節點 (儲存Div, Table, Section節點)
        /// </summary>
        private List<HtmlAgilityPack.HtmlNode> fTarTagDataList = new List<HtmlAgilityPack.HtmlNode>();

        /// <summary>
        /// 儲存有內文的節點 (儲存Div, Table, Section節點)
        /// </summary>
        public List<HtmlAgilityPack.HtmlNode> TarTagDataList
        {
            get
            {
                return fTarTagDataList;
            }
        }

        /// <summary>
        /// 儲存所有有分割字元的節點(',' '.' etc...)
        /// </summary>
        private List<SplitCharsNode> fHasSplitCharsNodeList = new List<SplitCharsNode>();

        /// <summary>
        /// 儲存所有有分割字元的節點(',' '.' etc...)
        /// </summary>
        public List<SplitCharsNode> HasSplitCharsNodeList
        {
            get
            {
                return fHasSplitCharsNodeList;
            }
        }

        /// <summary>
        /// 儲存最大內文節點 (OuterHtml)
        /// </summary>
        private HtmlNode fMaxOuterHtmlNode = null;

        /// <summary>
        /// 儲存最大內文節點 (OuterHtml)
        /// </summary>
        public HtmlNode MaxOuterHtmlNode
        {
            get
            {
                return fMaxOuterHtmlNode;
            }
            set
            {
                fMaxOuterHtmlNode = value;
            }
        }

        /// <summary>
        /// 儲存最大內文節點 (InnerText)
        /// </summary>
        private HtmlNode fMaxInnerTextNode = null;

        /// <summary>
        /// 儲存最大內文節點 (InnerText)
        /// </summary>
        public HtmlNode MaxInnerTextNode
        {
            get
            {
                return fMaxInnerTextNode;
            }
            set
            {
                fMaxInnerTextNode = value;
            }
        }

        /// <summary>
        /// 傳入的原始資料
        /// </summary>
        private string fInputHtmlData;

        /// <summary>
        /// 傳入的原始資料
        /// </summary>
        public string InputHtmlData
        {
            get
            {
                return fInputHtmlData;
            }
        }     

        /// <summary>
        /// 存所有的FormNode
        /// </summary>
        private List<HtmlNode> fFormNodeList = new List<HtmlNode>();

        #endregion

        #region 建構子

        /// <summary>
        /// 以原始資料建立DomTree
        /// </summary>
        /// <param name="inputHtmlData">傳入的Html資料</param>
        public DomTree(string inputHtmlData)
        {
            this.fInputHtmlData = inputHtmlData;

            //建立HtmlAgaility的html物件
            fHtmlDoc = new HtmlAgilityPack.HtmlDocument();

            //載入資料，成為Dom Tree
            fHtmlDoc.LoadHtml(fInputHtmlData);
        }

        /// <summary>
        /// 刪除指定資料後 建立DomTree
        /// </summary>
        /// <param name="inputHtmlData">傳入的Html資料</param>
        /// <param name="removeTagString">移除特定的Tag清單</param>
        /// <param name="removePatternString">移除特定的Pattern</param>
        public DomTree(string inputHtmlData, string removeTagString, string removePatternString)
        {
            #region 舊版
            //this.fInputHtmlData = inputHtmlData;

            //fHtmlDoc = new HtmlAgilityPack.HtmlDocument();
            //fHtmlDoc.LoadHtml(fInputHtmlData);

            //#region  移除註解

            //fRemoveComments();

            //#endregion

            //#region  移除特定的Tag清單

            //fRemoveTags(removeTagString.Replace(" ", "").ToLower().Split(',').ToList());

            //#endregion

            //#region 移除特定的Pattern
            //fAfterRemoveHtmlData = fRemovePattern(fHtmlDoc.DocumentNode.OuterHtml,removePatternString);
            //#endregion

            //#region 以新的資料建立 Dom Tree
            //fHtmlDoc = new HtmlAgilityPack.HtmlDocument();
            //fHtmlDoc.LoadHtml(fAfterRemoveHtmlData);
            //#endregion
            #endregion

            this.fInputHtmlData = inputHtmlData;

            #region 移除特定的Pattern

            string fAfterRemoveHtmlData = removePattern(fInputHtmlData, removePatternString);

            #endregion

            #region 以移除特定Pattern後的資料建立 Dom Tree
            fHtmlDoc = new HtmlAgilityPack.HtmlDocument();
            fHtmlDoc.LoadHtml(fAfterRemoveHtmlData);
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
            List<HtmlNode> myTarget = new List<HtmlNode>();

            var x = fHtmlDoc.DocumentNode.DescendantsAndSelf().Where(n => !string.IsNullOrEmpty(n.Id));

            foreach (var node in x)
            {
                if(node.Id.Contains(tarKeyWord))
                {
                    myTarget.Add(node);
                }
            }

            return myTarget;
        }

        #region v1 輸入資料轉成Dom Tree後，針對特定Tag節點作處理，取內文最多的節點(沒在用)

        ///// <summary>
        ///// 初始物件資料 
        ///// </summary>
        ///// <param name="inputHtmlData"></param>
        //public void InitMaxOuterHtmlAndMaxInnerTextNode()
        //{
        //    fTarTagDataList = new List<HtmlNode>();
        //    fImgFileCandidateList = new List<HtmlNode>();
        //    fMaxOuterHtmlNode = null;
        //    fMaxInnerTextNode = null;

        //    //儲存所有的節點
        //    List<HtmlAgilityPack.HtmlNode> tempAllTagNodeList = fHtmlDoc.DocumentNode.DescendantsAndSelf().ToList();

        //    //儲存所有有圖的節點
        //    //fImgFileCandidateList = fHtmlDoc.DocumentNode.Descendants("img").ToList();

        //    fImgFileCandidateList = (from n in fHtmlDoc.DocumentNode.Descendants("img")
        //                                 where n.Attributes["src"] != null
        //                                 select n).ToList();

        //    double innerLength = 0;

        //    foreach (var tagNode in tempAllTagNodeList)
        //    {
        //        //只需要作Element型態的即可
        //        if (tagNode.NodeType == HtmlAgilityPack.HtmlNodeType.Element)
        //        {
        //            string tempTagName = tagNode.Name.ToLower();



        //            if ((tempTagName == "table") || (tempTagName == "div") || (tempTagName == "section"))
        //            {
        //                //有內文，才作儲存，找圖用
        //                if (!string.IsNullOrEmpty(tagNode.InnerText.Trim()))
        //                {
        //                    //儲存有內文的節點
        //                    fTarTagDataList.Add(tagNode);
        //                }

        //                ////找出最大內文節點
        //                //if (tagNode.OuterHtml.Length > MaxOuterHtml.Length)
        //                //{
        //                //    fMaxOuterHtml = tagNode.OuterHtml;
        //                //}

        //                //找出最大內文節點
        //                if (fMaxOuterHtmlNode == null || tagNode.OuterHtml.Length > fMaxOuterHtmlNode.OuterHtml.Length)
        //                {
        //                    fMaxOuterHtmlNode = tagNode;
        //                }

        //                string inn = tagNode.InnerText;
        //                inn = inn.Replace("\n", "");
        //                inn = inn.Replace("\t", "");

        //                if (inn.Length > innerLength)
        //                {
        //                    fMaxInnerTextNode = tagNode;

        //                    innerLength = inn.Length;
        //                }
        //            }
        //        }
        //    }

        //}

        #endregion

        #region v2 輸入資料轉成Dom Tree後，針對特定Tag節點作處理，取內文分割字元最多的節點

        /// <summary>
        /// 初始物件資料 
        /// </summary>
        /// <param name="inputHtmlData"></param>
        public void InitMaxOuterHtmlAndMaxInnerTextNodeV2()
        {
            fTarTagDataList = new List<HtmlNode>();
            fImgFileCandidateList = new List<HtmlNode>();
            fHasSplitCharsNodeList = new List<SplitCharsNode>();
            fMaxOuterHtmlNode = null;
            fMaxInnerTextNode = null;
            //int innerLength = 0;

            //儲存所有有圖的節點
            //fImgFileCandidateList = fHtmlDoc.DocumentNode.Descendants("img").ToList();

            fImgFileCandidateList = (from n in fHtmlDoc.DocumentNode.Descendants("img")
                                     where n.Attributes["src"] != null
                                     select n).ToList();

            //儲存所有的節點
            List<HtmlNode> tempAllTagNodeList = fHtmlDoc.DocumentNode.DescendantsAndSelf().ToList();

            #region 舊版
            //如果有article節點就挑他來找內文
            //HtmlNode articleNode = fHtmlDoc.DocumentNode.Descendants("article").FirstOrDefault();
            //if (articleNode != null)
            //{
            //    List<HtmlNode> tempAllTagNodeForArticleList = new List<HtmlNode>();
            //    tempAllTagNodeForArticleList = articleNode.Descendants("article").ToList();
            //    if (tempAllTagNodeForArticleList != null || tempAllTagNodeForArticleList.Count > 0)
            //    {
            //        getSplitCharsNodeList(tempAllTagNodeForArticleList);
            //    }
            //    else
            //    {
            //        getSplitCharsNodeList(tempAllTagNodeList);
            //    }        
            //}
            //else
            //{
            //    getSplitCharsNodeList(tempAllTagNodeList);
            //}
            #endregion

            //取得有分割字元結點的List
            GetSplitCharsNodeList(tempAllTagNodeList);

            //為了尋找fb客製化的影片，所以將所有節點都存起來
            fTarTagDataList = tempAllTagNodeList;

            #region 取得有分割字元結點的List(沒用了)
            //foreach (var tagNode in tempAllTagNodeList)
            //{
            //    //只需要作Element型態的即可
            //    if (tagNode.NodeType == HtmlAgilityPack.HtmlNodeType.Element)
            //    {
            //        string tempTagName = tagNode.Name.ToLower();



            //        if ((tempTagName == "table") || (tempTagName == "div") || (tempTagName == "section"))
            //        {
            //            //有內文，才作儲存，找圖用
            //            if (!string.IsNullOrEmpty(tagNode.InnerText.Trim()))
            //            {
            //                //儲存有內文的節點
            //                fTarTagDataList.Add(tagNode);
            //            }

            //            //找出最大內文節點
            //            if (fMaxOuterHtmlNode == null || tagNode.OuterHtml.Length > fMaxOuterHtmlNode.OuterHtml.Length)
            //            {
            //                fMaxOuterHtmlNode = tagNode;
            //            }

            //            string inn = tagNode.InnerText;

            //            //Html的段落字元數
            //            int htmlPartStringCount = 0;


            //            //Regex r = new Regex("<p>");
            //            //htmlPartStringCount = r.Matches(tagNode.InnerHtml).Count;

            //            //計算<p> <br> 的次數
            //            foreach (string s in SPLIT_STRINGS)
            //            {
            //                Regex r = new Regex(s);
            //                htmlPartStringCount += r.Matches(tagNode.InnerHtml).Count;
            //            }


            //            //計算有幾個分割字元
            //            var splitCount = inn.Split(SPLIT_CHARS).Length;

            //            //分割字元與Html的段落字元數相加
            //            splitCount +=  htmlPartStringCount;

            //            //如果有分割字元
            //            if (splitCount > 1)
            //            {
            //                //內文斷句在innerLength的比例
            //                double tempSentenceProportion = ((double)splitCount / inn.Length);  

            //                //存入List
            //                fHasSplitCharsNodeList.Add(new TextImgDomModel_SplitCharsNode() { Node = tagNode, SentenceProportion = tempSentenceProportion, SplitCount = splitCount });

            //            }

            //            //如果沒有分割字元
            //            //if (fHasSplitCharsNodeList.Count == 0)
            //            //{
            //            //    inn = inn.Replace("\n", "");
            //            //    inn = inn.Replace("\t", "");
            //            //    inn = inn.Replace(" ", "");
            //            //    //將長度最長的節點存入
            //            //    if (inn.Length > innerLength)
            //            //    {
            //            //        fMaxInnerTextNode = tagNode;

            //            //        innerLength = inn.Length;
            //            //    }
            //            //}
            //        }
            //    }
            //}
            #endregion

            if (fHasSplitCharsNodeList.Count > 0)
            {
                //getMainTextNodeBySentenceProportion(tempCommaCount);

                //getMainTextNodeByNoChildNode();
                getMainTextNodeByRealSplitCount();

                //var test = (from n in fHasSplitCharsNodeList
                //            where n.SplitCount > 0
                //            orderby n.SplitCount
                //            select n).ToList();
            }
            else
            {
                //看有沒有被濾掉的FormNode
                if (fFormNodeList != null && fFormNodeList.Count > 0)
                {
                    var mainFormNode = (from n in fFormNodeList
                                        orderby n.XPath.Length ascending
                                        select n).ToList().FirstOrDefault();

                    List<HtmlNode> formTagNodeList = mainFormNode.DescendantsAndSelf().ToList();
                    GetSplitCharsNodeList(formTagNodeList);

                    if (fHasSplitCharsNodeList.Count > 0)
                    {
                        getMainTextNodeByRealSplitCount();
                    }
                }
            }

        }


        #endregion

        /// <summary>
        /// 確定是否要用Browser去解析
        /// 注意：這裡是做網頁特例所在，以後如果有要用WebBrowser去做的部分要在這裡加條件
        /// </summary>
        /// <returns>是否要用WebBrowser</returns>
        public bool IdentifyUseBrowserOrNot()
        {
            bool result = false;

            //這個網站的RSS： http://www.thisiscolossal.com/ 
            //使用WordPress的lazy load三方插件，他的特徵是原本Image tag的src是亂碼
            //引用javaScript後把實際圖片網址連結編入src內，而實際圖片網址連結是放在data-lazy-src內
            var dataLazyImg = (from candidateImg in fImgFileCandidateList
                               where candidateImg.Attributes.Contains("data-lazy-src")
                               select candidateImg).FirstOrDefault();

            if (dataLazyImg != null)
            {
                result = true;
            }

            return result;
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
                if (tagNode.NodeType == HtmlAgilityPack.HtmlNodeType.Element)
                {
                    string tempTagName = tagNode.Name.ToLower();
                    //string nodeClassString = "";

                    //if (tagNode.Attributes["class"] != null)
                    //{
                    //    nodeClassString = tagNode.Attributes["class"].Value.ToLower();
                    //}
                    // (濾掉class或是Id包含 "footer" 的測試)
                    //if ((tempTagName == "table") || (tempTagName == "div") || (tempTagName == "section") && !tagNode.Id.Contains("footer") && !nodeClassString.Contains("footer"))
                    if ((tempTagName == "table") || (tempTagName == "div") || (tempTagName == "section"))
                    {
                        //有內文，才作儲存，找圖用
                        if (!string.IsNullOrEmpty(tagNode.InnerText.Trim()))
                        {
                            //儲存有內文的節點
                            fTarTagDataList.Add(tagNode);
                        }

                        //找出最大內文節點
                        if (fMaxOuterHtmlNode == null || tagNode.OuterHtml.Length > fMaxOuterHtmlNode.OuterHtml.Length)
                        {
                            fMaxOuterHtmlNode = tagNode;
                        }

                        string inn = tagNode.InnerText;

                        //Html的段落字元數
                        int htmlPartStringCount = 0;


                        //Regex r = new Regex("<p>");
                        //htmlPartStringCount = r.Matches(tagNode.InnerHtml).Count;

                        //計算<p> <br> 的次數
                        foreach (string s in SPLIT_STRINGS)
                        {
                            Regex r = new Regex(s);
                            htmlPartStringCount += r.Matches(tagNode.InnerHtml).Count;
                        }


                        //計算有幾個分割字元
                        var splitCount = inn.Split(SPLIT_CHARS).Length;

                        //分割字元與Html的段落字元數相加
                        splitCount += htmlPartStringCount;

                        //如果有分割字元
                        if (splitCount > 1)
                        {
                            //內文斷句在innerLength的比例
                            double tempSentenceProportion = ((double)splitCount / inn.Length);

                            //存入List
                            fHasSplitCharsNodeList.Add(new SplitCharsNode() { Node = tagNode, SentenceProportion = tempSentenceProportion, SplitCount = splitCount });

                        }

                    }
                }
            }
        }

        #region 選取內文結點的所有方法模組

        /// <summary>
        /// 以每個node中選出斷句長度在內文長度中比例最高的
        /// </summary>
        private void getMainTextNodeBySentenceProportion()
        {
            var tempSplitCount = (from n in fHasSplitCharsNodeList
                                  orderby n.SplitCount descending
                                  select n.SplitCount).FirstOrDefault();

            //內文斷句在innerLength的比例
            double sentenceProportion = 0;

            foreach (var spNode in fHasSplitCharsNodeList)
            {
                //將最多分割字元的節點存入   (斷句長度在最大長度的1/3)
                if (spNode.SplitCount > tempSplitCount / 3)
                {

                    if (spNode.SentenceProportion > sentenceProportion)
                    {
                        sentenceProportion = spNode.SentenceProportion;
                        fMaxInnerTextNode = spNode.Node;
                    }
                }
            }
        }

        /// <summary>
        /// 在有分割字元的nodeList中選出child node不再List裡的做斷句長度比較
        /// </summary>
        private void getMainTextNodeByNoChildNode()
        {
            int tempSplitCount = 0;
            //內文斷句在innerLength的比例
            //double sentenceProportion = 0;

            foreach (var spNode in fHasSplitCharsNodeList)
            {
                var checkPrentNode = (from n in fHasSplitCharsNodeList
                                      where n.Node != spNode.Node && n.Node.XPath.Contains(spNode.Node.XPath)
                                      select n).FirstOrDefault();
                //如果再List裡沒有子節點
                if (checkPrentNode == null)
                {
                    //將最多分割字元的節點存入
                    if (spNode.SplitCount > tempSplitCount)
                    {
                        tempSplitCount = spNode.SplitCount;
                        fMaxInnerTextNode = spNode.Node;
                    }
                }
            }
        }

        /// <summary>
        /// 每個節點去除子樹的斷句字元數 取得屬於本身的斷句字元數 選出斷句數最多的
        /// </summary>
        private void getMainTextNodeByRealSplitCount()
        {

            //排序 從上層節點開始
            var realNode = (from n in fHasSplitCharsNodeList
                            orderby n.Node.XPath.Length ascending
                            select n);

            foreach (var spNode in realNode)
            {
                //選出同棵樹中最接近的父代節點
                var checkLastNode = (from n in fHasSplitCharsNodeList
                                     where n.Node != spNode.Node && spNode.Node.XPath.Contains(n.Node.XPath)
                                     orderby n.Node.XPath.Length descending
                                     select n).FirstOrDefault();

                if (checkLastNode != null)
                {
                    checkLastNode.SplitCount -= spNode.SplitCount;

                }


            }
            fMaxInnerTextNode = (from n in fHasSplitCharsNodeList
                                 orderby n.SplitCount descending, n.Node.InnerText.Length descending
                                 select n.Node).FirstOrDefault();

            //fMaxInnerTextNode = (from n in fHasSplitCharsNodeList
            //                     orderby n.SplitCount descending , n.SentenceProportion descending
            //                     select n.Node).FirstOrDefault();
        }
        #endregion

        #region 建構物件時 移除傳入資料中指定的Tag , Pattern...etc

        #region 移除註解

        /// <summary>
        /// 移除註解 (HtmlDocument)
        /// </summary>
        /// <returns></returns>
        private HtmlAgilityPack.HtmlDocument removeComments()
        {
            if (fHtmlDoc != null)
            {
                var selectNode = fHtmlDoc.DocumentNode.SelectNodes("//comment()");
                if (selectNode != null)
                {
                    foreach (HtmlNode comment in selectNode)
                    {
                        comment.ParentNode.RemoveChild(comment);
                    }

                    //HtmlAgilityPack.HtmlDocument fHtmlDocTest = new HtmlAgilityPack.HtmlDocument();
                    //fHtmlDocTest.LoadHtml(fHtmlDoc.DocumentNode.OuterHtml);

                    //var selectNodeTest = fHtmlDocTest.DocumentNode.SelectNodes("//comment()");
                }

            }

            return fHtmlDoc;
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
            if (fHtmlDoc != null)
            {
                var x = fHtmlDoc.DocumentNode.DescendantsAndSelf().ToList();

                //重要---必須用此方式改變參數設定  form才會有childNode
                //HtmlAgilityPack是針對HTML 3.2的規範，而HTML 3.2就是規定。 對於option，form等tag，其默認處理的结果是：其下的子節點，會變成sibling   2013/8/8  Jerry
                HtmlNode.ElementsFlags.Remove("form");

                //var selectTest = fHtmlDoc.DocumentNode.Descendants("form").ToList();
                //if (selectTest != null && selectTest.Count != 0)
                //{
                //}
                foreach (var tag in x)
                {
                    if (tag.NodeType == HtmlNodeType.Element)
                    {
                        if (tagList.Contains(tag.Name))
                        {
                            //如果是formNode的話存到list  所有node被濾掉時使用
                            if (tag.Name == "form")
                            {
                                fFormNodeList.Add(tag);
                            }

                            tag.Remove();

                        }
                    }
                }

                //HtmlAgilityPack.HtmlDocument fHtmlDocTest = new HtmlAgilityPack.HtmlDocument();
                //fHtmlDocTest.LoadHtml(fHtmlDoc.DocumentNode.OuterHtml);

                //var selectNodeTest = fHtmlDocTest.DocumentNode.Descendants("form").ToList();

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
            List<string> pa = rePa.Split(',').ToList();

            foreach (string s in pa)
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
        public List<HtmlAgilityPack.HtmlNode> GetTagWithName(string tagName)
        {
            var resultAll = fHtmlDoc.DocumentNode
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
        public List<HtmlAgilityPack.HtmlNode> GetTagListFromHead(string tagName, List<string> attribute, List<string> value)
        {

            List<HtmlAgilityPack.HtmlNode> resultAll = new List<HtmlAgilityPack.HtmlNode>();

            for (int a = 1; a < 20; a++)
            {
                string s = string.Format("//head/{0}[{1}]", tagName, a);

                if (fHtmlDoc.DocumentNode.SelectNodes(s) != null)
                {
                    resultAll.Add(fHtmlDoc.DocumentNode.SelectNodes(s)[0]);
                }
                else
                {
                    break;
                }
            }

            List<HtmlAgilityPack.HtmlNode> result = new List<HtmlAgilityPack.HtmlNode>();

            if ((attribute.Count > 0) && (attribute[0].Length > 0))
            {
                foreach (var x in resultAll)
                {
                    foreach (var y in attribute)
                    {
                        if (x.Attributes[y] != null)
                        {
                            bool esp = false;

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
        public List<HtmlAgilityPack.HtmlNode> GetTagList(string tagName, List<string> attribute, List<string> value)
        {

            List<HtmlAgilityPack.HtmlNode> resultAll = new List<HtmlAgilityPack.HtmlNode>();

            for (int a = 1; a < 20; a++)
            {
                string s = string.Format("//body/{0}[{1}]", tagName, a);

                if (fHtmlDoc.DocumentNode.SelectNodes(s) != null)
                {

                    resultAll.Add(fHtmlDoc.DocumentNode.SelectNodes(s)[0]);
                }
                else
                {
                    break;
                }
            }

            List<HtmlAgilityPack.HtmlNode> result = new List<HtmlAgilityPack.HtmlNode>();

            if ((attribute.Count > 0) && (attribute[0].Length > 0))
            {
                foreach (var x in resultAll)
                {
                    foreach (var y in attribute)
                    {
                        if (x.Attributes[y] != null)
                        {
                            bool esp = false;

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

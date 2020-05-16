using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    public class XMLBlock : SimpleTree
    {
        public XMLBlock(List<IElement> elems)
            : base(elems, false)
        {
        }

        public override IElement CreateCopy()
        {
            XMLBlock e = new XMLBlock(this.CopyChildren());
            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.XMLComment))
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }

            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            MoveInfo moveInfo = new MoveInfo(parentInfo);
            List<IElement> content = new List<IElement>();

            IElement cur = moveInfo.Current;
            int lastXMLIndex = moveInfo.CurrentIndex;
            do
            {
                if(cur.IsTT(TokenType.XMLComment))
                    lastXMLIndex = moveInfo.CurrentIndex;

                cur = moveInfo.Move(SearchDirection.LeftToRight);
            }
            while (cur != null && (cur.IsTT(TokenType.XMLComment) || cur.IsTT(TokenType.WhiteSpace)));

            int startIndex = parentInfo.CurrentIndex;
            int length = (lastXMLIndex + 1) - startIndex;
            // add tokens between first and last xml with its
            content.AddRange(moveInfo.CurrentElements.GetRange(startIndex, length));

            IElement xmlBlock = new XMLBlock(content);
            parentInfo.Replace(length, xmlBlock);
        }

        /// <summary>
        /// Vráti XMLBlock, kt. sa nachádza pred pozíciou moveInfo
        /// </summary>
        /// <param name="moveInfo"></param>
        /// <returns></returns>
        public static XMLBlock GetXMLSummary(MoveInfo moveInfo)
        {
            IElement cur = moveInfo.Move(SearchDirection.RightToLeft);
            while (cur != null && !cur.Visible)
            {
                if (cur is XMLBlock)
                    return (XMLBlock)cur;

                cur = moveInfo.Move(SearchDirection.RightToLeft);
            }
            return null;
        }

        public string GetStringContent()
        {
            StringBuilder sb = new StringBuilder();
            foreach (IElement e in this.children)
            {
                if (e.IsTT(TokenType.XMLComment))
                    sb.Append(((Token)e).StringContent.Trim());
                else if (e.IsTT(TokenType.WhiteSpace))
                    sb.Append(((Token)e).StringContent);
            }

            string str = sb.ToString();
            str = str.Trim();
            return str;
        }
    }
}

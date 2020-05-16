using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace CODSCRIPT.Content
{
    public class Path : SimpleTree
    {
        public string OriginalPath { get; private set; }

        private Path(List<IElement> elems)
            : base(elems)
        {
            Token t = new Token(this.CharIndex, this.CharLength, this.LineIndex, TokenType.Word, this.ToString());
            ClearChildren();
            AddChildren(t);

            OriginalPath = t.StringContent;
        }

        // for replacing or inserting path
        public Path(string originalPath, string overwritePath)
            : base(new List<IElement>() { new Token(TokenType.Word, overwritePath) })
        {
            OriginalPath = originalPath;
        }

        public void Update(string overwritePath)
        {
            ClearChildren();
            AddChildren(new Token(TokenType.Word, overwritePath));
        }

        public override IElement CreateCopy()
        {
            Path e = new Path(this.CopyChildren());
            e.OriginalPath = OriginalPath;
            return e;
        }

        public XmlElement ToXML(XmlDocument doc, XmlElement elem, ScriptInfo si)
        {
            elem.SetAttribute("originalPath", OriginalPath);
            elem.InnerText = ToString();
            return elem;
        }

        public static Path FromXML(XmlElement elem)
        {
            string originalPath = elem.GetAttribute("originalPath");
            string overwritePath = elem.InnerText;
            return new Path(originalPath, overwritePath);
        }

        /// <summary>
        /// Vráti path a pridá ho do stromu
        /// Mal by fungovať oboma smermi.
        /// </summary>
        /// <param name="moveInfo"></param>
        /// <param name="dir"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Path Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo) // TODO: otestovať oboma smermi!
        {
            // TODO: funguješ správne? skontrolovať!
            MoveInfo moveInfo = new MoveInfo(parentInfo);
            IElement next = moveInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
            if (next == null)
                return null;

            int startIndex = moveInfo.CurrentIndex;
            MoveToEnd(moveInfo, SearchDirection.LeftToRight);

            int length = moveInfo.CurrentIndex - startIndex;

            if (length == 0)
                return null;

            Path path = new Path(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.MoveToIndex(startIndex);
            parentInfo.Replace(length, path);
            return path;
        }

        /// <summary>
        /// Posunie MoveInfo na koniec pathu.
        /// Ukazuje na nasledujúci elem
        /// </summary>
        /// <param name="moveInfo"></param>
        public static void MoveToEnd(MoveInfo moveInfo, SearchDirection dir)
        {
            while (moveInfo.Current != null 
                && (moveInfo.Current.IsTT(TokenType.Word) || moveInfo.Current.IsTT(TokenType.BackSlash)))
                moveInfo.Move(dir);
        }
    }
}

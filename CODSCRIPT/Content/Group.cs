using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class Group : SimpleTree
    {
        protected int startElemCount;
        protected int endElemCount;

        protected Group(List<IElement> elems, int startElemCount, int endElemCount)
            : base(elems)
        {
            lastChildren = children.ToList();

            content = children.GetRange(startElemCount, children.Count - (startElemCount + endElemCount));
            lastContent = content.ToList();

            this.startElemCount = startElemCount;
            this.endElemCount = endElemCount;
        }

        private List<IElement> lastContent;
        private List<IElement> content;

        private List<IElement> lastChildren;

        public override IElement CreateCopy()
        {
            Group g = new Group(this.CopyChildren(), startElemCount, endElemCount);
            return g;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.ScopeOpen))
            {
                ScopeGroup.Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            else if (parentInfo.Current.IsTT(TokenType.ParenthesesOpen))
            {
                ParenthesesGroup.Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            else if (parentInfo.Current.IsTT(TokenType.SQBracketOpen))
            {
                SQBracketGroup.Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        public override List<IElement> GetChildren()
        {
            CheckUpdate();
            return children;
        }

        public override List<IElement> GetContent()
        {
            CheckUpdate();
            return content;
        }

        private void CheckUpdate()
        {
            if (IsContentUpdate())
            {
                List<IElement> opens = children.GetRange(0, startElemCount);
                List<IElement> closes = children.GetRange(children.Count - endElemCount, endElemCount);
                children = content.ToList();
                children.InsertRange(0, opens);
                children.AddRange(closes);
                lastChildren = children.ToList();

                lastContent = content.ToList();
            }
            else if (IsChildrenUpdate())
            {
                if (children.Count < (startElemCount + endElemCount))
                    throw new InvalidOperationException("Missing open/close elem.");

                content = children.GetRange(startElemCount, children.Count - (startElemCount + endElemCount));
                lastContent = content.ToList();

                lastChildren = children.ToList();
            }
        }

        private bool IsContentUpdate()
        {
            if (content.Count != lastContent.Count)
                return true;

            for (int i = 0; i < content.Count; i++)
                if (content[i] != lastContent[i])
                    return true;

            return false;
        }

        private bool IsChildrenUpdate()
        {
            if (children.Count != lastChildren.Count)
                return true;

            for (int i = 0; i < children.Count; i++)
                if (children[i] != lastChildren[i])
                    return true;

            return false;        
        }
    }

    class ScopeGroup : Group
    {
        private ScopeGroup(List<IElement> elems)
            : base(elems, 1, 1)
        {
        }

        public override IElement CreateCopy()
        {
            ScopeGroup g = new ScopeGroup(this.CopyChildren());
            return g;
        }

        /// <summary>
        /// Vytvorí grupu s obsahom { } a prepíše ju v strome.
        /// </summary>
        /// <param name="moveInfo"></param>
        /// <param name="dir"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static void Parse(MoveInfo moveInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            int startIndex;
            int length;
            moveInfo.GetGroupCorners(TokenType.ScopeOpen, TokenType.ScopeClose, 
                SearchVisibility.Visible, out startIndex, out length);

            ScopeGroup group = new ScopeGroup(moveInfo.CurrentElements.GetRange(startIndex, length));
            moveInfo.Replace(length, group);
        }
    }

    class ParenthesesGroup : Group
    {
        private ParenthesesGroup(List<IElement> elems)
            : base(elems, 1, 1)
        { 
        }

        public override IElement CreateCopy()
        {
            ParenthesesGroup g = new ParenthesesGroup(this.CopyChildren());
            return g;
        }

        /// <summary>
        /// Vytvorí grupu s obsahom ( ) a prepíše ju v strome.
        /// </summary>
        /// <param name="moveInfo"></param>
        /// <param name="dir"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static void Parse(MoveInfo moveInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            int startIndex;
            int length;
            moveInfo.GetGroupCorners(TokenType.ParenthesesOpen, TokenType.ParenthesesClose,
                SearchVisibility.Visible, out startIndex, out length);

            ParenthesesGroup group = new ParenthesesGroup(moveInfo.CurrentElements.GetRange(startIndex, length));
            moveInfo.Replace(length, group);
        }
    }

    class SQBracketGroup : Group
    {
        private SQBracketGroup(List<IElement> elems)
            : base(elems, 1, 1)
        { 
        }

        public override IElement CreateCopy()
        {
            SQBracketGroup g = new SQBracketGroup(this.CopyChildren());
            return g;
        }

        /// <summary>
        /// Vytvorí grupu s obsahom [ ] a prepíše ju v strome.
        /// </summary>
        /// <param name="moveInfo"></param>
        /// <param name="dir"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static void Parse(MoveInfo moveInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            int startIndex;
            int length;
            moveInfo.GetGroupCorners(TokenType.SQBracketOpen, TokenType.SQBracketClose,
                SearchVisibility.Visible, out startIndex, out length);

            SQBracketGroup group = new SQBracketGroup(moveInfo.CurrentElements.GetRange(startIndex, length));
            moveInfo.Replace(length, group);
        }
    }

    class DelegateCallGroup : Group
    {
        private DelegateCallGroup(List<IElement> elems, int startElemsCount, int endElemsCount)
            : base(elems, startElemsCount, endElemsCount)
        {
        }

        public override IElement CreateCopy()
        {
            DelegateCallGroup g = new DelegateCallGroup(this.CopyChildren(), this.startElemCount, this.endElemCount);
            return g;
        }

        public static new bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current is SQBracketGroup)
            {
                MoveInfo groupInfo = new MoveInfo((SQBracketGroup)parentInfo.Current, SearchTree.ContentBlock, 0, parentInfo);
                IElement first = groupInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
                if (first is SQBracketGroup && groupInfo.FindNextBlack(SearchDirection.LeftToRight) == null)
                {
                    Parse(parentInfo, parsingInfo, scriptInfo);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Vytvorí grupu s obsahom [[ ]] a prepíše ju v strome.
        /// </summary>
        /// <param name="moveInfo"></param>
        /// <param name="dir"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            SQBracketGroup outerGroup = (SQBracketGroup)parentInfo.Current;
            List<IElement> outerGroupChildren = outerGroup.GetChildren();

            MoveInfo outerGroupInfo = new MoveInfo(outerGroup, SearchTree.ContentBlock, 0, parentInfo);
            SQBracketGroup innerGroup = (SQBracketGroup)outerGroupInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
            List<IElement> innerGroupChildren = innerGroup.GetChildren();
            int startElemsCount = outerGroupChildren.IndexOf(innerGroup) + 1;
            int endElemsCount = outerGroupChildren.Count - startElemsCount + 1;

            outerGroupInfo.Replace(1, innerGroupChildren);

            List<IElement> children = outerGroup.GetChildren();

            DelegateCallGroup delegateCall = new DelegateCallGroup(children, startElemsCount, endElemsCount);
            parentInfo.Replace(1, delegateCall);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class Vector : ExpressionOperand
    {
        private Vector()
            : base()
        {
        }

        public override IElement CreateCopy()
        {
            Vector e = new Vector();
            e.AddChildren(this.CopyChildren());
            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current is ParenthesesGroup)
            {
                ParenthesesGroup group = (ParenthesesGroup)parentInfo.Current;
                MoveInfo moveInfo = new MoveInfo(group, SearchTree.ContentBlock, 0, parentInfo);

                int i;
                for (i = 0; i < 2; i++)
                {
                    IElement next = moveInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible, a => a.IsTT(TokenType.Comma));
                    if (next == null)
                        break;

                    moveInfo.Move(SearchDirection.LeftToRight);
                }

                if (i == 0)
                    return false;
                else if (i == 2)
                {
                    Parse(parentInfo, parsingInfo, scriptInfo);
                    return true;
                }
                else
                    throw new SyntaxException("Only 3D vector is allowed", parentInfo.GetErrorInfo());
            }
            return false;
        }

        public static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            Vector vec = new Vector();

            ParenthesesGroup group = (ParenthesesGroup)parentInfo.Current;
            MoveInfo groupInfo = new MoveInfo(group, SearchTree.ContentBlock, 0, parentInfo);

            for (int i = 0; i < 3; i++)
            {
                Expression exp = Expression.Parse(groupInfo, parsingInfo, scriptInfo);
                IElement next = groupInfo.FindNextBlack(SearchDirection.LeftToRight);
                if ((i < 2 && !next.IsTT(TokenType.Comma))
                    || (i == 2 && next != null))
                    throw new SyntaxException("Could not parse vector " + (i + 1) + " expression", parentInfo.GetErrorInfo());

                if (i != 2) // move behind ,
                    groupInfo.Move(SearchDirection.LeftToRight);
            }

            int startIndex = parentInfo.CurrentIndex;
            int length = 1;

            MoveInfo moveInfo = new MoveInfo(parentInfo);
            moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (moveInfo.Current != null && ArrayIndexer.Check(moveInfo, parsingInfo, scriptInfo))
                length = (moveInfo.CurrentIndex + 1) - startIndex;

            vec.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.Replace(length, vec);
        }
    }
}

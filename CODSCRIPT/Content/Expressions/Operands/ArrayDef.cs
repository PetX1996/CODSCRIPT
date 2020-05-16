using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class ArrayDef : ExpressionOperand
    {
        private ArrayDef()
            : base()
        {
        }

        public override IElement CreateCopy()
        {
            ArrayDef e = new ArrayDef();
            e.AddChildren(this.CopyChildren());
            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current is SQBracketGroup)
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            SQBracketGroup group = (SQBracketGroup)parentInfo.Current;
            MoveInfo moveInfo = new MoveInfo(group, SearchTree.ContentBlock, 0, parentInfo);
            if (moveInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible) != null)
                throw new SyntaxException("Unknown tokens in ArrayDef", parentInfo.GetErrorInfo());

            ArrayDef arrayDef = new ArrayDef();
            arrayDef.AddChildren(group);
            parentInfo.Replace(1, arrayDef);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class ArrayIndexer : ExpressionOperand
    {
        private ArrayIndexer()
            : base()
        {
        }

        public override IElement CreateCopy()
        {
            ArrayIndexer e = new ArrayIndexer();
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
            ArrayIndexer aI = new ArrayIndexer();

            SQBracketGroup SQGroup = (SQBracketGroup)parentInfo.Current;
            MoveInfo SQGroupInfo = new MoveInfo(SQGroup, SearchTree.ContentBlock, 0, parentInfo);
            Expression exp = Expression.Parse(SQGroupInfo, parsingInfo, scriptInfo);

            if (exp == null || SQGroupInfo.FindNextBlack(SearchDirection.LeftToRight) != null)
                throw new SyntaxException("Could not parse array index", parentInfo.GetErrorInfo());

            aI.AddChildren(SQGroup);
            parentInfo.Replace(1, aI);
        }
    }
}

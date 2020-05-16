using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    abstract class Statement : SimpleTree
    {
        protected Statement(List<IElement> elems)
            : base(elems)
        { 
        }

        protected Statement()
            : this(null)
        { 
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            IElement next = parentInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
            if (next == null)
                return false;

            if (DevCode.Check(parentInfo, parsingInfo, scriptInfo)
                || PreProcessorRegion.Check(parentInfo, parsingInfo, scriptInfo))
                return true;

            if (EmptyStatement.Check(parentInfo, parsingInfo, scriptInfo)
                || BlockStatement.Check(parentInfo, parsingInfo, scriptInfo)
                )
                return true;

            if (IfElseStatement.Check(parentInfo, parsingInfo, scriptInfo)
                || SwitchStatement.Check(parentInfo, parsingInfo, scriptInfo)

                || WhileStatement.Check(parentInfo, parsingInfo, scriptInfo)
                || DoWhileStatement.Check(parentInfo, parsingInfo, scriptInfo)
                || ForStatement.Check(parentInfo, parsingInfo, scriptInfo)
                || ForEachStatement.Check(parentInfo, parsingInfo, scriptInfo)

                || ReturnStatement.Check(parentInfo, parsingInfo, scriptInfo)
                || BreakStatement.Check(parentInfo, parsingInfo, scriptInfo)
                || ContinueStatement.Check(parentInfo, parsingInfo, scriptInfo)

                || WaitStatement.Check(parentInfo, parsingInfo, scriptInfo)
                || WaittillFrameEndStatement.Check(parentInfo, parsingInfo, scriptInfo)
                )
                return true;

            if (ExpressionStatement.Check(parentInfo, parsingInfo, scriptInfo, true)) // nikdy nevráti false?
                return true;

            return false;
        }

        public static void ParseStatementList(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            while (Check(parentInfo, parsingInfo, scriptInfo))
                parentInfo.Move(SearchDirection.LeftToRight);
        }
    }
}

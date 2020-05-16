using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class BlockStatement : Statement
    {
        private BlockStatement()
            : base()
        {
        }

        public override IElement CreateCopy()
        {
            BlockStatement e = new BlockStatement();
            e.AddChildren(this.CopyChildren());
            return e;
        }

        public static new bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current is ScopeGroup)
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            BlockStatement bS = new BlockStatement();
            ScopeGroup statementGroup = (ScopeGroup)parentInfo.Current;

            MoveInfo groupInfo = new MoveInfo(statementGroup, SearchTree.ContentBlock, 0, parentInfo);
            Statement.ParseStatementList(groupInfo, parsingInfo, scriptInfo);

            bS.AddChildren(statementGroup);
            parentInfo.Replace(1, bS);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class ExpressionStatement : Statement
    {
        private Expression _exp;

        private ExpressionStatement()
            : base()
        { 
        }

        public override IElement CreateCopy()
        {
            ExpressionStatement e = new ExpressionStatement();
            e.AddChildren(this.CopyChildren());

            e._exp = (Expression)e.children[this.children.IndexOf(_exp)];

            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo, bool isTerminated)
        {
            ExpressionStatement expStatement = new ExpressionStatement();

            Expression exp = Expression.Parse(parentInfo, parsingInfo, scriptInfo, true, false, false);
            if (exp == null)
                return false;

            expStatement._exp = exp;

            // exp content
            string error = "Only Assign, FuncCall, DelegateCall and PostfixIncDec is allowed as statement";
            MoveInfo expInfo = new MoveInfo(exp, SearchTree.ChildrenBlock, 0, parentInfo);
            if (!((expInfo.Current is Assign
                || expInfo.Current is FuncCall
                || expInfo.Current is DelegateCall
                || expInfo.Current is PostfixIncDec
                ) && expInfo.FindNextBlack(SearchDirection.LeftToRight) == null))
                throw new SyntaxException(error, parentInfo.GetErrorInfo());

            // next token
            MoveInfo moveInfo = new MoveInfo(parentInfo);
            if (isTerminated)
            {
                IElement terminal = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
                if (terminal == null || !terminal.IsTT(TokenType.SemiColon))
                    throw new SyntaxException("Missing directive ';'?", parentInfo.GetErrorInfo());
            }

            // build
            int startIndex = parentInfo.CurrentIndex;
            int length = (moveInfo.CurrentIndex + 1) - startIndex;

            expStatement.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.Replace(length, expStatement);

            return true;
        }

        public static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        { 
        
        }

        public override void Compile(MoveInfo treeInfo, ScriptInfo scriptInfo, CompilingInfo compilingInfo)
        {
            Assign tryAssign = _exp.GetChildren()[0] as Assign;
            if (tryAssign != null)
            {
                ArrayContentDef tryArrayContentDef = tryAssign.Exp.GetChildren()[0] as ArrayContentDef;
                if (tryArrayContentDef != null)
                {
                    IElement compiledStatements = tryArrayContentDef.GetCompiledStatements(tryAssign.VarName);
                    treeInfo.ReplaceCurrent(compiledStatements);
                }
            }
        }
    }
}

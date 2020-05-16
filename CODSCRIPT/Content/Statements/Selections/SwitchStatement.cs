using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class SwitchStatement : Statement
    {
        protected SwitchStatement()
            : base()
        { 
        }

        public override IElement CreateCopy()
        {
            SwitchStatement e = new SwitchStatement();
            e.AddChildren(this.CopyChildren());
            return e;
        }

        public static new bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.Word) && parentInfo.Current.ToString().EqualCode("switch"))
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            SwitchStatement switchStatement = new SwitchStatement();
            MoveInfo moveInfo = new MoveInfo(parentInfo);

            // expression
            IElement tryExpGroup = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (!(tryExpGroup is ParenthesesGroup))
                throw new SyntaxException("Could not find switch expression", parentInfo.GetErrorInfo());

            ParenthesesGroup expGroup = (ParenthesesGroup)tryExpGroup;

            MoveInfo expGroupInfo = new MoveInfo(expGroup, SearchTree.ContentBlock, 0, parentInfo);
            Expression exp = Expression.Parse(expGroupInfo, parsingInfo, scriptInfo);

            if (exp == null || expGroupInfo.FindNextBlack(SearchDirection.LeftToRight) != null)
                throw new SyntaxException("Could not parse switch expression", parentInfo.GetErrorInfo());

            // scope group
            IElement tryScopeGroup = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (!(tryScopeGroup is ScopeGroup))
                throw new SyntaxException("Could not find switch ScopeGroup", parentInfo.GetErrorInfo());

            ScopeGroup scopeGroup = (ScopeGroup)tryScopeGroup;

            MoveInfo scopeGroupInfo = new MoveInfo(scopeGroup, SearchTree.ContentBlock, 0, parentInfo);
            IElement nextCase = scopeGroupInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);

            if (nextCase == null)
                throw new SyntaxException("Could not find any switch case", parentInfo.GetErrorInfo());

            while (nextCase != null)
            {
                if (!(
                    DefaultSwitchStatement.Check(scopeGroupInfo, parsingInfo, scriptInfo, true)
                    || CaseSwitchStatement.Check(scopeGroupInfo, parsingInfo, scriptInfo, true)
                    ))
                    throw new SyntaxException("Could not parse switch case/default", parentInfo.GetErrorInfo());

                nextCase = scopeGroupInfo.FindNextBlack(SearchDirection.LeftToRight);
            }

            // build
            int startIndex = parentInfo.CurrentIndex;
            int length = (moveInfo.CurrentIndex + 1) - startIndex;

            switchStatement.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.Replace(length, switchStatement);
        }
    }

    class CaseSwitchStatement : SwitchStatement
    {
        private CaseSwitchStatement()
            : base()
        { 
        }

        public override IElement CreateCopy()
        {
            CaseSwitchStatement e = new CaseSwitchStatement();
            e.AddChildren(this.CopyChildren());
            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo, bool parse)
        {
            if (parentInfo.Current.IsTT(TokenType.Word) && parentInfo.Current.ToString().EqualCode("case"))
            {
                if (parse)
                    Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;            
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            CaseSwitchStatement caseSwitch = new CaseSwitchStatement();
            MoveInfo moveInfo = new MoveInfo(parentInfo);

            // expression
            IElement next = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            Expression exp = Expression.Parse(moveInfo, parsingInfo, scriptInfo);
            if (exp == null)
                throw new SyntaxException("Could not parse case expression", parentInfo.GetErrorInfo());

            // terminal
            IElement tryTerminal = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (tryTerminal == null || !tryTerminal.IsTT(TokenType.Colon))
                throw new SyntaxException("Missing directive ':'?", parentInfo.GetErrorInfo());

            // statements
            IElement nextStatement = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            while (nextStatement != null) // end of switch
            {
                if (CaseSwitchStatement.Check(moveInfo, parsingInfo, scriptInfo, false)
                    || DefaultSwitchStatement.Check(moveInfo, parsingInfo, scriptInfo, false))
                    break;

                Statement.Check(moveInfo, parsingInfo, scriptInfo);

                nextStatement = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            }

            // build
            int startIndex = parentInfo.CurrentIndex;
            int length = (moveInfo.CurrentIndex) - startIndex;

            caseSwitch.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.Replace(length, caseSwitch);
        }
    }

    class DefaultSwitchStatement : SwitchStatement
    {
        private DefaultSwitchStatement()
            : base()
        {
        }

        public override IElement CreateCopy()
        {
            DefaultSwitchStatement e = new DefaultSwitchStatement();
            e.AddChildren(this.CopyChildren());
            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo, bool parse)
        {
            if (parentInfo.Current.IsTT(TokenType.Word) && parentInfo.Current.ToString().EqualCode("default"))
            {
                if (parse)
                    Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            DefaultSwitchStatement defaultSwitch = new DefaultSwitchStatement();
            MoveInfo moveInfo = new MoveInfo(parentInfo);

            // terminal
            IElement tryTerminal = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (tryTerminal == null || !tryTerminal.IsTT(TokenType.Colon))
                throw new SyntaxException("Missing directive ':'?", parentInfo.GetErrorInfo());

            // statements
            IElement nextStatement = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            while (nextStatement != null) // end of switch
            {
                if (CaseSwitchStatement.Check(moveInfo, parsingInfo, scriptInfo, false)
                    || DefaultSwitchStatement.Check(moveInfo, parsingInfo, scriptInfo, false))
                    break;

                Statement.Check(moveInfo, parsingInfo, scriptInfo);

                nextStatement = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            }

            // build
            int startIndex = parentInfo.CurrentIndex;
            int length = (moveInfo.CurrentIndex) - startIndex;

            defaultSwitch.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.Replace(length, defaultSwitch);
        }
    }
}

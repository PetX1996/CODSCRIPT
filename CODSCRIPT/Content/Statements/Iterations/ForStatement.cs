using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class ForStatement : IterationStatement
    {
        private ForStatement()
            : base()
        { 
        }

        public override IElement CreateCopy()
        {
            ForStatement e = new ForStatement();
            e.AddChildren(this.CopyChildren());
            return e;
        }

        public static new bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.Word) && parentInfo.Current.ToString().EqualCode("for"))
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;  
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            ForStatement forStatement = new ForStatement();
            MoveInfo moveInfo = new MoveInfo(parentInfo);

            // expGroup
            IElement tryExpGroup = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (tryExpGroup == null || !(tryExpGroup is ParenthesesGroup))
                throw new SyntaxException("Could not find for expressions", parentInfo.GetErrorInfo());

            ParenthesesGroup expGroup = (ParenthesesGroup)tryExpGroup;
            MoveInfo expGroupInfo = new MoveInfo(expGroup, SearchTree.ContentBlock, 0, parentInfo);

            // initializer
            IElement tryInitializer = expGroupInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
            if (tryInitializer == null)
                throw new SyntaxException("Could not parse for initializer", parentInfo.GetErrorInfo());

            if (!tryInitializer.IsTT(TokenType.SemiColon))
            {
                if (!ExpressionStatement.Check(expGroupInfo, parsingInfo, scriptInfo, false))
                    throw new SyntaxException("Could not parse for initializer", parentInfo.GetErrorInfo());

                expGroupInfo.FindNextBlack(SearchDirection.LeftToRight);
            }

            if (expGroupInfo.Current == null || !expGroupInfo.Current.IsTT(TokenType.SemiColon))
                throw new SyntaxException("Missing for first directive ';'?", parentInfo.GetErrorInfo());

            // expression
            IElement tryExpression = expGroupInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (tryExpression == null)
                throw new SyntaxException("Could not parse for expression", parentInfo.GetErrorInfo());

            if (!tryExpression.IsTT(TokenType.SemiColon))
            {
                Expression exp = Expression.Parse(expGroupInfo, parsingInfo, scriptInfo);
                if (exp == null)
                    throw new SyntaxException("Could not parse for expression", parentInfo.GetErrorInfo());

                expGroupInfo.FindNextBlack(SearchDirection.LeftToRight);
            }

            if (expGroupInfo.Current == null || !expGroupInfo.Current.IsTT(TokenType.SemiColon))
                throw new SyntaxException("Missing for second directive ';'?", parentInfo.GetErrorInfo());

            // iterator
            IElement tryIterator = expGroupInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (tryIterator != null)
            {
                if (!ExpressionStatement.Check(expGroupInfo, parsingInfo, scriptInfo, false))
                    throw new SyntaxException("Could not parse for iterator", parentInfo.GetErrorInfo());

                IElement end = expGroupInfo.FindNextBlack(SearchDirection.LeftToRight);
                if (end != null)
                    throw new SyntaxException("Could not parse for iterator", parentInfo.GetErrorInfo());
            }

            // statement
            moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (!Statement.Check(moveInfo, parsingInfo, scriptInfo))
                throw new SyntaxException("Could not parse for statement", parentInfo.GetErrorInfo());

            // build
            int startIndex = parentInfo.CurrentIndex;
            int length = (moveInfo.CurrentIndex + 1) - startIndex;

            forStatement.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.Replace(length, forStatement);
        }
    }
}

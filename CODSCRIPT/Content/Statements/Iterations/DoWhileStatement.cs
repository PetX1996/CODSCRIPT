using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class DoWhileStatement : IterationStatement
    {
        private Token _doKeyword;
        private Statement _statement;
        private Token _whileKeyword;

        private ParenthesesGroup _expParentGroup;

        private DoWhileStatement()
            : base()
        { 
        }

        public override IElement CreateCopy()
        {
            DoWhileStatement e = new DoWhileStatement();
            e.AddChildren(this.CopyChildren());

            e._doKeyword = (Token)e.children[this.children.IndexOf(_doKeyword)];
            e._statement = (Statement)e.children[this.children.IndexOf(_statement)];
            e._whileKeyword = (Token)e.children[this.children.IndexOf(_whileKeyword)];

            e._expParentGroup = (ParenthesesGroup)e.children[this.children.IndexOf(_expParentGroup)];

            return e;
        }

        public static new bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.Word) && parentInfo.Current.ToString().EqualCode("do"))
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;  
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            DoWhileStatement doWhileStatement = new DoWhileStatement();
            MoveInfo moveInfo = new MoveInfo(parentInfo);

            doWhileStatement._doKeyword = (Token)moveInfo.Current;

            // statement
            moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (!Statement.Check(moveInfo, parsingInfo, scriptInfo))
                throw new SyntaxException("Could not parse do-while statement", parentInfo.GetErrorInfo());

            doWhileStatement._statement = (Statement)moveInfo.Current;

            // while
            IElement tryWhile = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (tryWhile == null || !tryWhile.IsTT(TokenType.Word) || !tryWhile.ToString().EqualCode("while"))
                throw new SyntaxException("Could not find do-while while part", parentInfo.GetErrorInfo());

            doWhileStatement._whileKeyword = (Token)tryWhile;

            // expression
            IElement tryExpGroup = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (tryExpGroup == null || !(tryExpGroup is ParenthesesGroup))
                throw new SyntaxException("Could not find do-while expression", parentInfo.GetErrorInfo());

            ParenthesesGroup expGroup = (ParenthesesGroup)tryExpGroup;
            MoveInfo expGroupInfo = new MoveInfo(expGroup, SearchTree.ContentBlock, 0, parentInfo);
            Expression exp = Expression.Parse(expGroupInfo, parsingInfo, scriptInfo);

            if (exp == null || expGroupInfo.FindNextBlack(SearchDirection.LeftToRight) != null)
                throw new SyntaxException("Could not parse do-while expression", parentInfo.GetErrorInfo());

            doWhileStatement._expParentGroup = expGroup;

            // terminal
            IElement tryTerminal = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (tryTerminal == null || !tryTerminal.IsTT(TokenType.SemiColon))
                throw new SyntaxException("Missing directive ';'?", parentInfo.GetErrorInfo());

            // build
            int startIndex = parentInfo.CurrentIndex;
            int length = (moveInfo.CurrentIndex + 1) - startIndex;

            doWhileStatement.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.Replace(length, doWhileStatement);
        }

        public override void Compile(MoveInfo treeInfo, ScriptInfo scriptInfo, CompilingInfo compilingInfo)
        {
            /*
            _statement
             * !BlockStatement
            =================================
            do
                statement;
            while (exp);
            =================================
            
            _statement
             * BlockStatement
               * ScopeGroup
                 * 
            =================================
            var = true; while (var || exp)
            { var = false; statement; }
            
            =================================
            
            =================================
            do
            {
                statement;
            }
            while (exp);
            =================================            
            
            =================================
            var = true; while (var || exp)
            { var = false; 
                statement;
            }
            
            =================================
            */

            Token firstVar = new Token(TokenType.Word, "doWhileJHS8G8AW9_" + compilingInfo.IteratorsCount++);
            Expression exp = (Expression)_expParentGroup.GetContent().Find(a => a is Expression);

            // "doWhileControlVar = true;"
            BallOfMud firstVarInit = new BallOfMud(
                new List<IElement>() { 
                    firstVar, 
                    Token.Space, 
                    Token.Assign, 
                    Token.Space, 
                    Token.True, 
                    Token.SemiColon });

            // "doWhileControlVar = false;"
            BallOfMud firstVarCancel = new BallOfMud(
                new List<IElement>() { 
                    firstVar, 
                    Token.Space, 
                    Token.Assign, 
                    Token.Space, 
                    Token.False, 
                    Token.SemiColon });

            BallOfMud blockVarInsert = new BallOfMud(
                    new List<IElement> { Token.Space, firstVarCancel });

            #region Replace "do" with "doWhileControlVar = true; while (doWhileControlVar || exp)"
            // "(doWhileControlVar || exp)"
            BallOfMud newExpression = new BallOfMud(
                new List<IElement> { 
                    Token.ParenthesesOpen, 
                    firstVar, 
                    Token.Space, 
                    Token.LogicOR, 
                    Token.Space,
                    exp,
                    Token.ParenthesesClose});

            // "doWhileControlVar = true; while (doWhileControlVar || exp)"
            BallOfMud doReplace = new BallOfMud(
                new List<IElement> { 
                    firstVarInit, 
                    Token.Space, 
                    WhileKeyword, 
                    Token.Space, 
                    newExpression });

            int doIndex = this.children.IndexOf(_doKeyword);
            this.children[doIndex] = doReplace;
            #endregion

            #region Add fisrtVarCancel & add block
            if (_statement is BlockStatement) // it is already block ...
            {
                ScopeGroup blockGroup = (ScopeGroup)_statement.GetChildren().Find(a => a is ScopeGroup);
                blockGroup.GetContent().Insert(0, blockVarInsert);
            }
            else // create block
            {
                // "{ doWhileControlVar = false; statement; }"
                BallOfMud blockForInsert = new BallOfMud(
                    new List<IElement> { 
                        Token.ScopeOpen,
                        Token.Space,
                        firstVarCancel,
                        Token.Space,
                        _statement,
                        Token.Space,
                        Token.ScopeClose });

                int statementI = this.children.IndexOf(_statement);
                int tryTabIndex = statementI - 1;
                if (tryTabIndex >= 0
                    && this.children[tryTabIndex].IsTT(TokenType.WhiteSpace)
                    && this.children[tryTabIndex].ToString() == "\t")
                {
                    this.children.RemoveAt(tryTabIndex);
                    statementI--;
                }
                this.children[statementI] = blockForInsert;
            }
            #endregion

            // delete "while (exp);"
            int whileKeywordI = this.children.IndexOf(_whileKeyword);
            this.children.RemoveRange(whileKeywordI, this.children.Count - whileKeywordI);
        }

        static Token WhileKeyword = new Token(TokenType.Word, "while");
    }
}

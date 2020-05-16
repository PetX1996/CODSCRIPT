using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class ForEachStatement : IterationStatement
    {
        private Token _foreachKeyword;
        private ParenthesesGroup _foreachGroup;
            // in the _foreachGroup
            private VarName _currentVar;
            private Expression _array;

        private Statement _statement;

        private ForEachStatement()
            : base()
        {
        }

        public override IElement CreateCopy()
        {
            ForEachStatement e = new ForEachStatement();
            e.AddChildren(this.CopyChildren());

            e._foreachKeyword = (Token)e.children[this.children.IndexOf(_foreachKeyword)];
            e._foreachGroup = (ParenthesesGroup)e.children[this.children.IndexOf(_foreachGroup)];
                e._currentVar = (VarName)e._foreachGroup.GetChildren()[this._foreachGroup.GetChildren().IndexOf(_currentVar)];
                e._array = (Expression)e._foreachGroup.GetChildren()[this._foreachGroup.GetChildren().IndexOf(_array)];

            e._statement = (Statement)e.children[this.children.IndexOf(_statement)];

            return e;
        }

        public static new bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.Word) && parentInfo.Current.ToString().EqualCode("foreach"))
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;   
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            ForEachStatement foreachStatement = new ForEachStatement();
            MoveInfo moveInfo = new MoveInfo(parentInfo);

            foreachStatement._foreachKeyword = (Token)moveInfo.Current;

            // expression
            IElement expGroupTry = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (expGroupTry == null || !(expGroupTry is ParenthesesGroup))
                throw new SyntaxException("Could not find foreach expression", parentInfo.GetErrorInfo());

            ParenthesesGroup expGroup = (ParenthesesGroup)expGroupTry;
            MoveInfo expGroupInfo = new MoveInfo(expGroup, SearchTree.ContentBlock, 0, parentInfo);

            foreachStatement._foreachGroup = expGroup;

                // var define
                IElement tryVar = expGroupInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
                if (tryVar == null || !tryVar.IsTT(TokenType.Word))
                    throw new SyntaxException("Could not parse foreach var", parentInfo.GetErrorInfo());

                VarName.Parse(expGroupInfo, parsingInfo, scriptInfo);
                VarName varName = (VarName)expGroupInfo.Current;

                LocalVarInfo localVar = parsingInfo.CurrentFunc.LocalVars.Find(a => a.Name.EqualCode(tryVar.ToString())); // there is already var with this name...
                if (localVar == null)
                    parsingInfo.CurrentFunc.LocalVars.Add(new LocalVarInfo(scriptInfo.SF, tryVar.ToString(), tryVar.CharIndex, tryVar.CharLength, null, varName));

                foreachStatement._currentVar = varName;

                // in keyword
                IElement tryIn = expGroupInfo.FindNextBlack(SearchDirection.LeftToRight);
                if (tryIn == null || !tryIn.IsTT(TokenType.Word) || !tryIn.ToString().EqualCode("in"))
                    throw new SyntaxException("Could not find foreach 'in'", parentInfo.GetErrorInfo());

                // array
                IElement tryArray = expGroupInfo.FindNextBlack(SearchDirection.LeftToRight);
                Expression tryArrayExp = Expression.Parse(expGroupInfo, parsingInfo, scriptInfo);
                if (tryArrayExp == null || expGroupInfo.FindNextBlack(SearchDirection.LeftToRight) != null)
                    throw new SyntaxException("Could not parse foreach array", parentInfo.GetErrorInfo());

                foreachStatement._array = tryArrayExp;

            // statement
            moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (!Statement.Check(moveInfo, parsingInfo, scriptInfo))
                throw new SyntaxException("Could not parse foreach statement", parentInfo.GetErrorInfo());

            foreachStatement._statement = (Statement)moveInfo.Current;

            // build
            int startIndex = parentInfo.CurrentIndex;
            int length = (moveInfo.CurrentIndex + 1) - startIndex;

            foreachStatement.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.Replace(length, foreachStatement);
        }

        public override void Compile(MoveInfo treeInfo, ScriptInfo scriptInfo, CompilingInfo compilingInfo)
        {
            /*
            =================================
            foreach (var in array)
                statement;
            =================================

            =================================
            for (i = 0; i < array.size; i++)
            { var = array[i]; statement; }
            =================================
            
            =================================
            foreach (var in array)
            {
                statement;
            }
            =================================            
            
            =================================
            for (i = 0; i < array.size; i++)
            { var = array[i];
                statement; 
            }
            =================================
            */

            Token indexer = new Token(TokenType.Word, "foreachg45e74f_" + compilingInfo.IteratorsCount++);

            #region Replace "foreach (var in array)" with "for (i = 0; i < array.size; i++)"
            // "i = 0"
            BallOfMud indexerInit = new BallOfMud(
                new List<IElement>() { 
                    indexer, 
                    Token.Space, 
                    Token.Assign, 
                    Token.Space, 
                    NumberZero });

            // "i < array.size"
            BallOfMud indexerCondition = new BallOfMud(
                new List<IElement>() { 
                    indexer, 
                    Token.Space, 
                    Token.LogicLess, 
                    Token.Space, 
                    _array,
                    Token.Ref, 
                    KeywordSize });

            // "i++"
            BallOfMud indexerStatement = new BallOfMud(
                new List<IElement>() { 
                    indexer, 
                    Token.INC });

            // "(i = 0; i < array.size; i++)"
            BallOfMud foreachGroupReplace = new BallOfMud(
                new List<IElement> { 
                    Token.ParenthesesOpen,
                    indexerInit,
                    Token.SemiColon,
                    Token.Space,
                    indexerCondition,
                    Token.SemiColon,
                    Token.Space,
                    indexerStatement,
                    Token.ParenthesesClose });

            int foreachI = this.children.IndexOf(_foreachKeyword);
            this.children[foreachI] = KeywordFor;

            int foreachGroupI = this.children.IndexOf(_foreachGroup);
            this.children[foreachGroupI] = foreachGroupReplace;
            #endregion

            #region Add var define & add block
            // " var = array[i];"
            BallOfMud varDefine = new BallOfMud(
                new List<IElement> { 
                    Token.Space,
                    _currentVar,
                    Token.Space,
                    Token.Assign,
                    Token.Space,
                    _array,
                    Token.SQBracketOpen,
                    indexer,
                    Token.SQBracketClose,
                    Token.SemiColon });

            if (_statement is BlockStatement) // it is already block ...
            {
                ScopeGroup blockGroup = (ScopeGroup)_statement.GetChildren().Find(a => a is ScopeGroup);
                blockGroup.GetContent().Insert(0, varDefine);
            }
            else // create block
            {
                // "{ var = array[i]; statement; }"
                BallOfMud blockForInsert = new BallOfMud(
                    new List<IElement> { 
                        Token.ScopeOpen,
                        varDefine,
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
        }

        static Token KeywordFor = new Token(TokenType.Word, "for");
        static Token KeywordSize = new Token(TokenType.Word, "size");
        static Token NumberZero = new Token(TokenType.Number, 0);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class DelegateCall : ExpressionOperand
    {
        public List<Expression> Arguments { get; private set; }

        private DelegateCall()
            : base()
        {
            Arguments = new List<Expression>();
        }

        public override IElement CreateCopy()
        {
            DelegateCall e = new DelegateCall();
            e.Arguments = Arguments;
            e.AddChildren(this.CopyChildren());
            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current is DelegateCallGroup)
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            DelegateCall delegCall = new DelegateCall();
            MoveInfo moveInfo = new MoveInfo(parentInfo);

            DelegateCallGroup group = (DelegateCallGroup)parentInfo.Current;
            MoveInfo groupInfo = new MoveInfo(group, SearchTree.ContentBlock, 0, parentInfo);

            Expression deleg = Expression.Parse(groupInfo, parsingInfo, scriptInfo);
            if (deleg == null || groupInfo.FindNextBlack(SearchDirection.LeftToRight) != null)
                throw new SyntaxException("Could not parse delegate", parentInfo.GetErrorInfo());

            #region ParsingInfo Args
            object lastCall = parsingInfo.CurrentCall;
            int? lastCallArgIndex = parsingInfo.CurrentCallArgIndex;
            parsingInfo.CurrentCall = delegCall;
            parsingInfo.CurrentCallArgIndex = 0;
            #endregion

            // find args
            moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (!FuncArgs.Check(moveInfo, parsingInfo, scriptInfo))
                throw new SyntaxException("Could not find delegate funcArgs", parentInfo.GetErrorInfo());

            int startIndex = parentInfo.CurrentIndex;
            int length;

            #region ParsingInfo Args
            parsingInfo.CurrentCall = lastCall;
            parsingInfo.CurrentCallArgIndex = lastCallArgIndex;
            #endregion

            // find self and modifiers
            MoveInfo selfInfo = new MoveInfo(parentInfo);
            IElement self = selfInfo.FindNextBlack(SearchDirection.RightToLeft);
            if (self != null && self is ExpressionOperand) // self is self or FuncCallModifier
            {
                startIndex = selfInfo.CurrentIndex;
                if (self is FuncCallModifier) // self is FuncCallModifier -> find self
                {
                    self = selfInfo.FindNextBlack(SearchDirection.RightToLeft);
                    if (self != null && self is ExpressionOperand)
                        startIndex = selfInfo.CurrentIndex;
                }
            }

            // find members and arrayIndexers
            IElement next = null;
            do
            {
                length = (moveInfo.CurrentIndex + 1) - startIndex;
                next = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            }
            while (next != null &&
                (ArrayIndexer.Check(moveInfo, parsingInfo, scriptInfo)
                || DataMember.Check(moveInfo, parsingInfo, scriptInfo)));

            // build
            delegCall.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.MoveToIndex(startIndex);
            parentInfo.Replace(length, delegCall);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class UsingName : ExpressionOperand
    {
        UsingInfo _usingInfo;
        public UsingInfo UsingInfo { get { return _usingInfo; } }

        public UsingName(List<IElement> elems, UsingInfo usingInfo)
            : base(elems)
        {
            this._usingInfo = usingInfo;
        }

        public override IElement CreateCopy()
        {
            UsingName e = new UsingName(this.CopyChildren(), _usingInfo);
            return e;
        }

        public static UsingName ConvertToMe(IBlock parentBlock, Path path, UsingInfo usingInfo)
        {
            UsingName usingName = new UsingName(path.GetChildren(), usingInfo);
            List<IElement> defChildren = parentBlock.GetChildren();
            int index = defChildren.IndexOf(path);
            defChildren.RemoveAt(index);
            defChildren.Insert(index, usingName);
            return usingName;
        }
    }
}

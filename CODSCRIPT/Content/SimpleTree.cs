using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    public abstract class SimpleTree : IBlock
    {
        public int CharIndex { get; private set; }
        public int CharLength { get; private set; }
        public int LineIndex { get; private set; }

        public int? ImportantCharIndex { get; private set; }
        public int? ImportantCharLength { get; private set; }
        public int? ImportantLineIndex { get; private set; }

        public bool Visible { get; protected set; }

        protected List<IElement> children;

        public virtual IElement CreateCopy()
        {
            throw new InvalidOperationException();
        }

        public List<IElement> CopyChildren()
        {
            List<IElement> newChildren = new List<IElement>(children.Count);
            foreach (IElement e in children)
                newChildren.Add(e.CreateCopy());

            return newChildren;
        }

        protected SimpleTree(List<IElement> children, bool visible, int? importantCharIndex, int? importantCharLength, int? importantLineIndex)
        {
            ImportantCharIndex = importantCharIndex;
            ImportantCharLength = importantCharLength;
            ImportantLineIndex = importantLineIndex;

            Visible = visible;
            AddChildren(children);
        }

        protected SimpleTree(List<IElement> children, bool visible)
            : this(children, visible, null, null, null)
        {
        }

        protected SimpleTree(List<IElement> children)
            : this(children, true)
        { 
        }

        protected SimpleTree()
            : this(null)
        {
        }

        public virtual List<IElement> GetChildren()
        {
            return children;
        }

        public virtual List<IElement> GetContent()
        {
            return children;
        }

        public virtual void AddChildren(List<IElement> elems)
        {
            if (elems == null || elems.Count == 0)
                return;

            if (this.children == null)
            {
                this.children = elems;

                this.CharIndex = children[0].CharIndex;
                this.LineIndex = children[0].LineIndex;
            }
            else
                this.children.AddRange(elems);

            this.CharLength = 0;
            if (this.children != null)
                foreach (IElement elem in this.children)
                    this.CharLength += elem.CharLength;
        }

        public virtual void ClearChildren()
        {
            this.children = null;
        }

        public void AddChildren(IElement elem)
        {
            AddChildren(new List<IElement> { elem });
        }

        public virtual void CheckSemantic(MoveInfo treeInfo, ScriptInfo scriptInfo, CheckingInfo checkingInfo)
        {
        }

        public virtual void Compile(MoveInfo treeInfo, ScriptInfo scriptInfo, CompilingInfo compilingInfo)
        {
        }

        public override string ToString()
        { 
            StringBuilder sb = new StringBuilder();
            foreach (IElement e in GetChildren())
                sb.Append(e.ToString());

            return sb.ToString();
        }
    }
}

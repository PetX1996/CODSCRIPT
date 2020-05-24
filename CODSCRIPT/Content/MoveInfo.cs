using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    /// <summary>
    /// Hýbanie dozadu funguje len v móde bloku!
    /// </summary>
    public enum SearchDirection
    {
        LeftToRight = 1,
        RightToLeft = -1
    };

    public enum SearchVisibility
    {
        All,
        Unvisible,
        Visible
    };

    public enum SearchTree
    {
        ContentBlock = 1,
        ContentTree = 2,
        ChildrenBlock = 4,
        ChildrenTree = 8
    };

    /// <summary>
    /// Trieda zabezpečujúca pohyb v strome.
    /// </summary>
    public class MoveInfo
    {
        #region MoveInfo
        public IBlock CurrentBlock { get; private set; }
        public List<IElement> CurrentElements { get; private set; }
        public int CurrentIndex { get; private set; }
        public IElement Current { 
            get { return (CurrentIndex >= 0 && CurrentIndex < CurrentElements.Count ) ? CurrentElements[CurrentIndex] : null; } }

        private ErrorInfo errorInfo;

        public ErrorInfo GetErrorInfo()
        {
            this.errorInfo.CurCharIndex = Current.CharIndex;
            this.errorInfo.CurLineIndex = Current.LineIndex;
            this.errorInfo.CurCharLength = this.errorInfo.GetCharLengthToEOL();
            return this.errorInfo;            
        }

        public ErrorInfo GetErrorInfo(IElement elem)
        {
            this.errorInfo.CurCharIndex = Current.CharIndex;
            this.errorInfo.CurLineIndex = Current.LineIndex;
            this.errorInfo.CurCharLength = elem.CharLength;
            return this.errorInfo;            
        }

        public SearchTree TreeMode { get; private set; }

        private Stack<TreeInfo> treeInfoStack;
        public int Level { get { return treeInfoStack.Count; } }

        public MoveInfo(IBlock curBlock, SearchTree tree, int curIndex, ScriptFile sf)
        {
            CurrentBlock = curBlock;
            CurrentIndex = curIndex;
            TreeMode = tree;
            this.errorInfo = new ErrorInfo(sf);
            CurrentElements = GetBlockElems(curBlock);
            treeInfoStack = new Stack<TreeInfo>();
        }

        public MoveInfo(IBlock curBlock, SearchTree tree, int curIndex, MoveInfo moveInfo)
            : this(curBlock, tree, curIndex, moveInfo.errorInfo.SF)
        {
        }

        public MoveInfo(MoveInfo info)
            : this(info.CurrentBlock, info.TreeMode, info.CurrentIndex, info)
        {
        }

        private void Restore(MoveInfo info)
        {
            CurrentBlock = info.CurrentBlock;
            CurrentElements = info.CurrentElements;
            CurrentIndex = info.CurrentIndex;
            TreeMode = info.TreeMode;
            this.errorInfo = info.errorInfo;           
        }
        #endregion

        #region Moving
        /// <summary>
        /// Získa obsah bloku podľa toho, či sa prehľadáva len obsah alebo celý blok.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private List<IElement> GetBlockElems(IBlock block)
        {
            if (TreeMode == SearchTree.ContentBlock || TreeMode == SearchTree.ContentTree)
                return block.GetContent();
            else
                return block.GetChildren();
        }

        /*/// <summary>
        /// Vráti index elementu v jeho rodičovi.
        /// </summary>
        /// <param name="elem"></param>
        /// <returns></returns>
        private int GetIndexInParent(IElement elem)
        {
            List<IElement> children = GetBlockElems(elem.Parent);
            int index = children.FindIndex(a => a == elem);
            if (index == -1)
                throw new ArgumentException("Element could not be find in the Parent.");

            return index;
        }*/

        /// <summary>
        /// Presunie sa na daný index v súčasnom bloku.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IElement MoveToIndex(int index)
        {
            //if (index < 0 || index >= CurrentElements.Count)
                //throw new ArgumentException("index");

            CurrentIndex = index;
            return Current;
        }

        /// <summary>
        /// Vráti element, ktorý nasleduje ďalej v strome/sekvencii, prípadne vráti null.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="tree"></param>
        /// <returns></returns>
        public IElement Move(SearchDirection dir, bool goIntoCurrent)
        {
            return dir == SearchDirection.LeftToRight ? MoveNext(goIntoCurrent) : MovePrev();
        }

        /// <summary>
        /// Vráti element, ktorý nasleduje ďalej v strome/sekvencii, prípadne vráti null.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="tree"></param>
        /// <returns></returns>
        public IElement Move(SearchDirection dir)
        {
            return Move(dir, true);
        }

        /// <summary>
        /// Vráti element, ktorý nasleduje ďalej v strome/sekvencii, prípadne vráti null.
        /// </summary>
        /// <returns></returns>
        private IElement MoveNext(bool goIntoCurrent)
        {
            /*if ((tree & SearchTree.GoIn) == SearchTree.GoIn && Current is IBlock && MoveIn((IBlock)Current))
            {
                // skoč o úroveň nižšie do stromu
                return Current;
            }
            else
            {*/
            // prejdi na ďalší elem alebo vyskoč a prejdi na ďalší v rodičovi
            //CurrentIndex++;
            /*while (CurrentIndex >= CurrentElements.Count)
            {
                if ((tree & SearchTree.GoOut) != SearchTree.GoOut)
                    return null;

                if (!MoveOut()) // parent is BaseTree
                    return null;

                CurrentIndex++;
            }*/
            //return Current;
            //}

            if (TreeMode == SearchTree.ContentBlock || TreeMode == SearchTree.ChildrenBlock)
            {
                // posunie sa v súčasnom bloku ďalej
                CurrentIndex++;
            }
            else
            {
                // skočí do súčasného elemu a vráti jeho prvého potomka
                if (goIntoCurrent && Current is IBlock && MoveIn((IBlock)Current))
                    return Current;
                else // posunie sa ďalej v súčasnom bloku, ak je na konci, skočí do rodiča a na ďalší elem.
                {
                    CurrentIndex++;
                    while (CurrentIndex >= CurrentElements.Count)
                    {
                        if (!MoveOut()) // parent is BaseTree
                            return null;

                        CurrentIndex++;
                    }
                }
            }

            return Current;
        }

        /// <summary>
        /// Vráti predchádzajúci element v strome/sekvencii, prípadne vráti null.
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        private IElement MovePrev()
        {
            CurrentIndex--;

            /*// skoč o úroveň vyššie v strome
            if (CurrentIndex <= 0)
            {
                if ((tree & SearchTree.GoOut) != SearchTree.GoOut || !MoveOut())
                    return null;

                return Current;
            }*/

            /*while ((tree & SearchTree.GoIn) == SearchTree.GoIn && Current is IBlock && MoveIn((IBlock)Current))
            {
                // skoč čo najhlbšie do stromu a označ posledný elem
                CurrentIndex = CurrentElements.Count - 1;
            }*/
            return Current;
        }
        
        /// <summary>
        /// Vyskočí z bloku do jeho rodiča a vyberie blok.
        /// </summary>
        /// <returns></returns>
        private bool MoveOut()
        {
            if (treeInfoStack.Count == 0)
                return false;

            TreeInfo parent = treeInfoStack.Pop();

            CurrentIndex = parent.Index;
            CurrentBlock = parent.Block;
            CurrentElements = GetBlockElems(CurrentBlock);
            return true;
        }
        
        /// <summary>
        /// Skočí do zadaného bloku a vyberie prvý elem.
        /// </summary>
        /// <param name="block"></param>
        private bool MoveIn(IBlock block)
        {
            List<IElement> elements = GetBlockElems(block);

            if (elements.Count <= 0)
                return false;

            treeInfoStack.Push(new TreeInfo(CurrentBlock, CurrentIndex));

            CurrentBlock = block;
            CurrentElements = elements;
            CurrentIndex = 0;
            return true;
        }

        class TreeInfo
        {
            public IBlock Block { get; set; }
            public int Index { get; set; }

            public TreeInfo(IBlock block, int index)
            {
                Block = block;
                Index = index;
            }
        }
        #endregion

        #region Changing
        /// <summary>
        /// Zkontroluje, či viditeľnosť elemu zodpovedá zadaným kritériám.
        /// </summary>
        /// <param name="elem"></param>
        /// <param name="vis"></param>
        /// <returns></returns>
        private static bool CheckVisible(IElement elem, SearchVisibility vis)
        {
            return !((vis == SearchVisibility.Unvisible && elem.Visible)
                    || (vis == SearchVisibility.Visible && !elem.Visible));
        }

        /// <summary>
        /// Nahradí list elementov(začiatok na CurrentIndex) jediným elemom.
        /// </summary>
        /// <param name="length"></param>
        /// <param name="elem"></param>
        public void Replace(int length, IElement elem)
        {
            CurrentElements.RemoveRange(CurrentIndex, length);
            CurrentElements.Insert(CurrentIndex, elem);
        }

        /// <summary>
        /// Nahradí list elementov(začiatok na CurrentIndex) listom elemov.
        /// </summary>
        /// <param name="length"></param>
        /// <param name="elems"></param>
        public void Replace(int length, List<IElement> elems)
        {
            CurrentElements.RemoveRange(CurrentIndex, length);
            CurrentElements.InsertRange(CurrentIndex, elems);
        }
        #endregion

        #region CurrentState
        /// <summary>
        /// Nachádzam sa vo zvolenom type?
        /// </summary>
        /// <returns></returns>
        public bool IsIn<TBlock>() where TBlock : IBlock
        {
            return IsIn<TBlock>(false);
        }

        /// <summary>
        /// Nachádzam sa vo zvolenom type?
        /// </summary>
        /// <returns></returns>
        public bool IsIn<TBlock>(bool includeCurrentBlock) where TBlock : IBlock
        {
            if (this.TreeMode == SearchTree.ContentBlock || this.TreeMode == SearchTree.ChildrenBlock)
                throw new InvalidOperationException("TreeMode is not a Tree type.");

            foreach (TreeInfo info in treeInfoStack)
            {
                if (info.Block is TBlock)
                    return true;
            }

            if (includeCurrentBlock && CurrentBlock is TBlock)
                return true;

            return false;
        }

        public bool IsInBlock(IBlock block)
        {
            return IsInBlock(block, false);
        }

        public bool IsInBlock(IBlock block, bool includeCurrentBlock)
        {
            if (this.TreeMode == SearchTree.ContentBlock || this.TreeMode == SearchTree.ChildrenBlock)
                throw new InvalidOperationException("TreeMode is not a Tree type.");

            foreach (TreeInfo info in treeInfoStack)
            {
                if (info.Block == block)
                    return true;
            }

            if (includeCurrentBlock && CurrentBlock == block)
                return true;

            return false;
        }

        public TBlock FindBlockInStack<TBlock>(bool includeCurrentBlock)
          where TBlock : IBlock
        {
          if (this.TreeMode == SearchTree.ContentBlock || this.TreeMode == SearchTree.ChildrenBlock)
            throw new InvalidOperationException("TreeMode is not a Tree type.");

          foreach (TreeInfo info in treeInfoStack)
          {
            if (info.Block is TBlock)
              return (TBlock)info.Block;
          }

          if (includeCurrentBlock && CurrentBlock is TBlock)
            return (TBlock)CurrentBlock;

          return default (TBlock);
        }
        #endregion

        #region Searching
        /// <summary>
        /// Nájde nasledujúci elem vyhovujúci všetkým podmienkam.
        /// Do hľadania zahrnuje aj Current elem!
        /// Ukazuje na tento elem.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="tree"></param>
        /// <param name="vis"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public IElement Find(SearchDirection dir, SearchVisibility vis, Predicate<IElement> predicate)
        {
            IElement cur = Current;
            while(true)
            {
                if (cur == null)
                    break;

                if (!CheckVisible(cur, vis)
                    || (predicate != null && !predicate.Invoke(cur)))
                    cur = Move(dir);
                else
                    break;
            }
            return cur;
        }

        /// <summary>
        /// Nájde nasledujúci elem vyhovujúci všetkým podmienkam.
        /// Ukazuje na tento elem.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="tree"></param>
        /// <param name="vis"></param>
        /// <returns></returns>
        public IElement Find(SearchDirection dir, SearchVisibility vis)
        {
            return Find(dir, vis, null);
        }

        public IElement FindNextBlack(SearchDirection dir)
        {
            Move(dir);
            return Find(dir, SearchVisibility.Visible);
        }

        /// <summary>
        /// Vráti hranice grupy.
        /// Ukazuje na začiatočný znak grupy ( { [.
        /// //TODO: otestovať!!!!
        /// </summary>
        /// <param name="parser"></param>
        /// <param name="openElem"></param>
        /// <param name="closeElem"></param>
        /// <param name="dir"></param>
        /// <param name="tree"></param>
        /// <param name="vis"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        public void GetGroupCorners(TokenType openElem, TokenType closeElem, 
            SearchVisibility vis, out int startIndex, out int length)
        {
            MoveInfo oldMoveInfo = new MoveInfo(this);

            IElement start = Find(SearchDirection.LeftToRight, vis, a => a.IsTT(openElem));
            if (start == null)
                throw new SyntaxException("Could not find start of block '" + openElem.ToString() + "'", oldMoveInfo.GetErrorInfo());

            MoveInfo startMoveInfo = new MoveInfo(this);

            int level = 0;
            startIndex = CurrentIndex;
            IElement cur = Current;
            do
            {
                if (cur == null)
                    throw new SyntaxException("Could not find end of block '" + closeElem.ToString() + "'", oldMoveInfo.GetErrorInfo());

                if (CheckVisible(cur, vis))
                {
                    if (cur.IsTT(openElem))
                        level++;
                    else if (cur.IsTT(closeElem))
                        level--;
                }

                if (level != 0)
                    cur = Move(SearchDirection.LeftToRight);
            }
            while (level != 0);

            length = (CurrentIndex + 1) - startIndex;
            Restore(startMoveInfo);
        }
        #endregion

        #region Editing
        public void ReplaceCurrent(IElement newElem)
        {
            if (Current == null)
                throw new InvalidOperationException("Current is null");

            CurrentElements[CurrentIndex] = newElem;
        }
        #endregion
    }
}

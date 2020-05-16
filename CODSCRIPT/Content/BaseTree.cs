using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using CODSCRIPT.Content;

namespace CODSCRIPT
{
    /// <summary>
    /// Reprezentuje koreň stromu.
    /// </summary>
    public class BaseTree : IBlock
    {
        private ScriptFile _sf;
        public ScriptFile SF { get { return _sf; } }

        public int CharIndex { get { throw new InvalidOperationException("BaseTree does not have any CharIndex!"); } }
        public int CharLength { get { throw new InvalidOperationException("BaseTree does not have any CharLength"); } }
        public int LineIndex { get { throw new InvalidOperationException("BaseTree does not have any LineIndex!"); } }

        public int? ImportantCharIndex { get { throw new InvalidOperationException("BaseTree does not have any ImportantCharIndex!"); } }
        public int? ImportantCharLength { get { throw new InvalidOperationException("BaseTree does not have any ImportantCharLength!"); } }
        public int? ImportantLineIndex { get { throw new InvalidOperationException("BaseTree does not have any ImportantLineIndex!"); } }
        public bool Visible { get { throw new InvalidOperationException("BaseTree does not have any Visible!"); } }

        private List<IElement> children;

        internal List<PreProcessorInclude> IncludeDefList { get; private set; }
        internal List<FuncDef> FuncDefList { get; private set; }
        internal List<ConstDef> ConstDefList { get; private set; }
        internal List<UsingDef> UsingDefList { get; private set; }
        internal List<OverwriteConstDef> OverwriteConstDefList { get; private set; }

        private Dictionary<ScriptFile, ScriptFile> _overwriteSFList;

        public BaseTree(List<IElement> elems)
        {
            children = elems;
        }

        public IElement CreateCopy()
        {
            throw new InvalidOperationException("WTF!!!");
        }

        /// <summary>
        /// Create a deep copy of all children and attributes.
        /// </summary>
        /// <param name="sf"></param>
        /// <returns></returns>
        public BaseTree CreateCopy(ScriptFile sf)
        {
            List<IElement> newChildren = new List<IElement>(children.Count);
            foreach (IElement e in children)
                newChildren.Add(e.CreateCopy());

            BaseTree bt = new BaseTree(newChildren);
            bt._sf = sf;
            bt.IncludeDefList = new List<PreProcessorInclude>();
            bt.FuncDefList = new List<FuncDef>();
            bt.ConstDefList = new List<ConstDef>();
            bt.UsingDefList = new List<UsingDef>();
            bt.OverwriteConstDefList = new List<OverwriteConstDef>();

            // find defs...
            MoveInfo btInfo = new MoveInfo(bt, SearchTree.ChildrenBlock, 0, sf);
            IElement curElem = btInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
            while (curElem != null)
            {
                if (curElem is PreProcessorInclude)
                    bt.IncludeDefList.Add((PreProcessorInclude)curElem);
                if (curElem is FuncDef)
                    bt.FuncDefList.Add((FuncDef)curElem);
                else if (curElem is ConstDef)
                {
                    bt.ConstDefList.Add((ConstDef)curElem);
                    // update SI def
                    ConstInfo cInfo = sf.SI.FindLocalConst(((ConstDef)curElem).ConstInfo.Name);
                    cInfo.ConstDef = (ConstDef)curElem;
                }
                else if (curElem is UsingDef)
                    bt.UsingDefList.Add((UsingDef)curElem);
                else if (curElem is OverwriteConstDef)
                    bt.OverwriteConstDefList.Add((OverwriteConstDef)curElem);

                curElem = btInfo.FindNextBlack(SearchDirection.LeftToRight);
            }
            return bt;
        }

        public List<IElement> GetChildren()
        {
            return children;
        }

        public List<IElement> GetContent()
        {
            return children;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (IElement e in GetChildren())
                sb.Append(e.ToString());

            return sb.ToString();
        }

        #region Reading
        public static BaseTree Parse(List<IElement> sourceTokens, ScriptFile sf, ScriptInfo scriptInfo)
        {
            BaseTree tree = new BaseTree(sourceTokens);
            tree._sf = sf;

            MoveInfo parser = new MoveInfo(tree, SearchTree.ChildrenBlock, 0, sf);

            ParsingInfo parsingInfo = new ParsingInfo(sf);

            // parse all groups
            MoveInfo groupInfo = new MoveInfo(tree, SearchTree.ContentTree, 0, sf);
            IElement cur = groupInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
            while (cur != null)
            {
                Group.Check(groupInfo, parsingInfo, scriptInfo);
                cur = groupInfo.FindNextBlack(SearchDirection.LeftToRight);
            }

            // parse delegate call [[delegate]]
            MoveInfo delegateInfo = new MoveInfo(tree, SearchTree.ContentTree, 0, sf);
            cur = delegateInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
            while (cur != null)
            {
                DelegateCallGroup.Check(delegateInfo, parsingInfo, scriptInfo);
                cur = delegateInfo.FindNextBlack(SearchDirection.LeftToRight);
            }

            // create parse tree
            MoveInfo contentInfo = new MoveInfo(parser);
            cur = contentInfo.Current;
            while (cur != null)
            {
                if (!cur.Visible)
                {
                    XMLBlock.Check(contentInfo, parsingInfo, scriptInfo);

                    cur = contentInfo.Move(SearchDirection.LeftToRight);
                }
                else
                {
                    if (DevCode.Check(contentInfo, parsingInfo, scriptInfo)
                        || PreProcessorInclude.Check(contentInfo, parsingInfo, scriptInfo)
                        || PreProcessorRegion.Check(contentInfo, parsingInfo, scriptInfo)
                        || AccessModifier.Check(contentInfo, parsingInfo, scriptInfo)
                        || SealedModifier.Check(contentInfo, parsingInfo, scriptInfo)
                        || UsingDef.Check(contentInfo, parsingInfo, scriptInfo)
                        || OverwriteConstDef.Check(contentInfo, parsingInfo, scriptInfo)
                        || FuncDef.Check(contentInfo, parsingInfo, scriptInfo)
                        || ConstDef.Check(contentInfo, parsingInfo, scriptInfo)
                        )
                        cur = contentInfo.Move(SearchDirection.LeftToRight);
                    else
                        throw new SyntaxException("Unknown token", contentInfo.GetErrorInfo());
                }
            }

            tree.IncludeDefList = parsingInfo.IncludeDefList;
            tree.FuncDefList = parsingInfo.FuncDefList;
            tree.ConstDefList = parsingInfo.ConstDefList;
            tree.UsingDefList = parsingInfo.UsingDefList;
            tree.OverwriteConstDefList = parsingInfo.OverwriteConstDefList;

            return tree;
        }

        public void CheckSemantic(MoveInfo treeInfo, ScriptInfo scriptInfo, CheckingInfo checkingInfo)
        {
            // funkcia by sa teoreticky nikdy nemala zavolať!
            throw new InvalidOperationException("WTF?!");
        }

        public void CheckSemantic(string sourceCode, ScriptInfo scriptInfo)
        {
            CheckingInfo checkingInfo = new CheckingInfo(_sf.SC);
            MoveInfo treeInfo = new MoveInfo(this, SearchTree.ContentTree, 0, this._sf);
            IElement cur = treeInfo.Current;

            while (cur != null)
            {
                cur.CheckSemantic(treeInfo, scriptInfo, checkingInfo);
                cur = treeInfo.Move(SearchDirection.LeftToRight);
                if (treeInfo.Level == 0)
                {
                    if (checkingInfo.CurrentFunc != null)
                        checkingInfo.CurrentFunc.CheckSemantic_OnLeaveFuncDef(scriptInfo);

                    checkingInfo.CurrentFunc = null;
                }
            }
        }
        #endregion

        #region Compiling
        public bool FindOverwrites()
        {
            if (_sf.OriginalOverwriteFile != null) // if _sf is also overwrite
            {
                // get overwrites from original file
                Dictionary<ScriptFile, ScriptFile> origList = _sf.OriginalOverwriteFile.SC.CompileTree._overwriteSFList;
                _overwriteSFList = new Dictionary<ScriptFile, ScriptFile>();
                foreach (ScriptFile sf in origList.Keys)
                    _overwriteSFList.Add(sf, origList[sf]);

                // add this file..
                _overwriteSFList.Add(_sf.OriginalOverwriteFile, _sf);
            }
            else
            {
                // find & copy overwritten SFs
                _overwriteSFList = new Dictionary<ScriptFile, ScriptFile>();
                foreach (OverwriteConstDef o in OverwriteConstDefList)
                {
                    ScriptFile sf = o.ConstInfo.SF;
                    if (!_overwriteSFList.ContainsKey(sf))
                    {
                        if (!sf.PrepareCompileSC())
                            return false;

                        _overwriteSFList.Add(sf, sf.CreateCopyForOverwrite(_sf));
                    }
                }
            }
            return true;
        }

        #region CompileMembers
        public bool CompileMembers()
        {
            // replace usings & paths & constants & specify paths in constants
            foreach (ConstDef c in ConstDefList)
                c.ReplaceMembersInContent(this);

            foreach (OverwriteConstDef c in OverwriteConstDefList)
                c.ReplaceMembersInContent(this);

            // replace path in includes
            foreach (PreProcessorInclude i in IncludeDefList)
            {
                string originalPath = i.Path.OriginalPath;
                string finalPath = GetActualSFPath(originalPath);
                i.Path.Update(finalPath);
            }

            // update constants in overwritten SFs
            foreach (OverwriteConstDef c in OverwriteConstDefList)
            {
                ScriptFile overwriteSF = _overwriteSFList[c.ConstInfo.SF];
                ConstInfo cInOverwriteSF = overwriteSF.SI.FindLocalConst(c.ConstInfo.Name);
                cInOverwriteSF.ConstDef.OriginalContent = c.CompiledContent;
            }

            // compile overwritten SFs
            foreach (ScriptFile originalSF in _overwriteSFList.Keys)
            {
                ScriptFile sf = _overwriteSFList[originalSF];
                if (sf == _sf) // ignore itself
                    continue;

                if (!sf.PrepareCompileSC())
                    return false;

                if (!sf.CompileMembersSC())
                    return false;

                sf.CompileCodeSC();
                sf.CompileOutputSC();
            }

            // replace usings & paths & constants in code
            MoveInfo treeInfo = new MoveInfo(this, SearchTree.ChildrenTree, 0, this._sf);
            IElement curElem = treeInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
            while (curElem != null)
            {
                if (curElem is UsingName)
                {
                    UsingName usingName = (UsingName)curElem;
                    string finalSFPath = GetActualSFPath(usingName.UsingInfo.SFPath);
                    treeInfo.ReplaceCurrent(new Path(usingName.UsingInfo.SFPath, finalSFPath));
                }
                else if (curElem is Path)
                {
                    string originalPath = ((Path)curElem).OriginalPath;
                    string finalSFPath = GetActualSFPath(originalPath);
                    ((Path)curElem).Update(finalSFPath);
                }
                else if (curElem is ConstName)
                {
                    ConstName constName = (ConstName)curElem;
                    IElement finalExp = null;

                    OverwriteConstDef overwriteConst = OverwriteConstDefList.Find(a => a.ConstInfo.Compare(constName.ConstInfo));
                    if (overwriteConst != null)
                        finalExp = overwriteConst.OriginalContent;
                    else
                    {
                        ConstInfo targetConstInfo = constName.ConstInfo;
                        if (targetConstInfo.SF.SI.IsGlobal)
                            finalExp = GetGlobalConstContent(constName);
                        else
                            finalExp = GetCompiledConstValue(targetConstInfo);
                    }

                    treeInfo.ReplaceCurrent(finalExp);
                }

                curElem = treeInfo.FindNextBlack(SearchDirection.LeftToRight);
            }
            return true;
        }

        private IElement GetGlobalConstContent(ConstName constName)
        {
            if (constName.ConstInfo.SF.SFPath == "compiler")
                return constName;
            else
                return constName.ConstInfo.Value;
        }

        public string GetActualSFPath(string sfPath)
        {
            ScriptFile overwriteSF = _overwriteSFList.Find(a => a.SFPath.EqualCode(sfPath));
            if (overwriteSF != null)
                sfPath = overwriteSF.SFPath;

            return sfPath;
        }

        /// <summary>
        /// Returns DEEP COPY of a 'real' compiled constant.
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public Expression GetCompiledConstValue(Expression exp)
        {
            Expression content = (Expression)exp.CreateCopy();
            MoveInfo expTree = new MoveInfo(content, SearchTree.ContentTree, 0, _sf);
            IElement curElem = expTree.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
            while (curElem != null)
            {
                if (curElem is FuncCall)
                    ((FuncCall)curElem).Compile_SpecifyPath(this);
                else if (curElem is DelegateDef)
                    ((DelegateDef)curElem).Compile_SpecifyPath(this);
                else if (curElem is ConstName)
                {
                    ConstName constName = (ConstName)curElem;
                    IElement finalExp = null;

                    OverwriteConstDef overwriteConst = OverwriteConstDefList.Find(a => a.ConstInfo.Compare(constName.ConstInfo));
                    if (overwriteConst != null)
                        finalExp = overwriteConst.OriginalContent;
                    else
                    {
                        ConstInfo targetConstInfo = constName.ConstInfo;
                        if (targetConstInfo.SF.SI.IsGlobal)
                            finalExp = GetGlobalConstContent(constName);
                        else
                            finalExp = GetCompiledConstValue(targetConstInfo);
                    }

                    expTree.ReplaceCurrent(finalExp);
                }
                else if (curElem is UsingName)
                {
                    UsingName usingName = (UsingName)curElem;
                    string finalSFPath = GetActualSFPath(usingName.UsingInfo.SFPath);
                    expTree.ReplaceCurrent(new Path(usingName.UsingInfo.SFPath, finalSFPath));
                }
                else if (curElem is Path)
                {
                    string originalPath = ((Path)curElem).OriginalPath;
                    string finalSFPath = GetActualSFPath(originalPath);
                    ((Path)curElem).Update(finalSFPath);
                }

                curElem = expTree.FindNextBlack(SearchDirection.LeftToRight);
            }
            return content;
        }

        /// <summary>
        /// Returns DEEP COPY of a 'real' compiled constant.
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public Expression GetCompiledConstValue(ConstInfo constant)
        {
            ConstInfo targetConstInfo = constant.SF.SI.FindLocalConst(constant.Name); // find actual instance!
            ScriptFile targetSF = targetConstInfo.SF; // SF in ConstInfo is not valid!! It has always reference to original SF!!

            ScriptFile overwriteSF = _overwriteSFList.Find(a => a == targetConstInfo.SF); // ConstInfo have always reference to original SF
            if (overwriteSF != null)
            {
                targetConstInfo = overwriteSF.SI.FindLocalConst(targetConstInfo.Name);
                targetSF = overwriteSF;
            }

            if (targetSF.SI.IsCompiled)
                return targetConstInfo.Value;
            else if (targetSF != _sf)
            {
                if (!targetSF.PrepareCompileSC())
                    throw new ApplicationException("Could not compile " + targetSF.ToString());

                return targetSF.SC.CompileTree.GetCompiledConstValue(targetConstInfo);
            }
            else // ConstInfo is in current SI
            {            
                return GetCompiledConstValue(targetConstInfo.ConstDef.OriginalContent);
            }
        }
        #endregion

        public void CompileTree(ScriptInfo scriptInfo)
        {
            CompilingInfo compilingInfo = new CompilingInfo();
            MoveInfo treeInfo = new MoveInfo(this, SearchTree.ChildrenTree, 0, this._sf);
            IElement cur = treeInfo.Current;
            while (cur != null)
            {
                cur.Compile(treeInfo, scriptInfo, compilingInfo);
                cur = treeInfo.Move(SearchDirection.LeftToRight);
            }
        }

        public string CompileOutput(OutputSetting outputSetting)
        {
            StringBuilder sb = new StringBuilder();
            MoveInfo treeInfo = new MoveInfo(this, SearchTree.ChildrenTree, 0, _sf);
            IElement cur = treeInfo.Current;

            string curStr = null;

            Queue<IElement> tokenQueue = new Queue<IElement>();

            Random rnd = new Random();

            int curLineLength = 0;

            bool goIntoCurrent = true;
            while (cur != null)
            {
                while (true) // "goto" -> always broken
                {
                    // defs
                    if (outputSetting.DeleteDefs)
                    {
                        if (cur is ConstDef || cur is OverwriteConstDef || cur is UsingDef || cur is PreProcessorRegion || cur is AccessModifier)
                        {
                            goIntoCurrent = false; // ignore whole block
                            break;
                        }
                        else if (cur is FuncDefParam)
                        {
                            tokenQueue.Enqueue(((FuncDefParam)cur).VarName.GetChildren()[0]);
                            goIntoCurrent = false;
                            break;
                        }
                    }

                    if (cur is Token)
                    {
                        // comments
                        if (outputSetting.DeleteComments)
                        {
                            if (cur.IsTT(TokenType.LineComment) || cur.IsTT(TokenType.XMLComment))
                                break;
                            else if (cur.IsTT(TokenType.BlockComment)) // fix for line numbers
                            {
                                string commentContent = ((Token)cur).StringContent;
                                int eolCount = commentContent.Count(a => a == '\n');
                                for (int i = 0; i < eolCount; i++)
                                    tokenQueue.Enqueue(Token.EOL);

                                break;
                            }
                        }

                        // white space
                        if (outputSetting.DeleteWhite 
                            && cur.IsTT(TokenType.WhiteSpace))
                            break;

                        if (outputSetting.RandomCase 
                            && cur.IsTT(TokenType.Word))
                        {
                            // random case
                            StringBuilder curRndStr = new StringBuilder(cur.ToString());
                            for (int i = 0; i < curRndStr.Length; i++)
                            {
                                if (rnd.Next() % 2 == 0)
                                    curRndStr[i] = Char.ToLowerInvariant(curRndStr[i]);
                                else
                                    curRndStr[i] = Char.ToUpperInvariant(curRndStr[i]);
                            }
                            curStr = curRndStr.ToString();
                        }
                        else
                            curStr = cur.ToString();

                        if (curStr == "\r\n" || curStr == "\r" || curStr == "\n")
                        {
                            // line length
                            if (outputSetting.LineLengthMin != 0 && outputSetting.LineLengthMax != 0
                                && curLineLength < outputSetting.LineLengthMin) // do not add EOL
                                curStr = "";
                            else
                                curLineLength = 0;
                        }
                        else
                            curLineLength += curStr.Length;

                        // line length
                        if (outputSetting.LineLengthMin != 0 && outputSetting.LineLengthMax != 0)
                        {
                            if (curLineLength > outputSetting.LineLengthMax)
                            {
                                curLineLength = curStr.Length;
                                curStr = curStr.Insert(0, "\r\n");
                            }
                        }

                        sb.Append(curStr);
                    }

                    break;
                }

                if (tokenQueue.Count != 0)
                    cur = tokenQueue.Dequeue();
                else
                {
                    cur = treeInfo.Move(SearchDirection.LeftToRight, goIntoCurrent);
                    goIntoCurrent = true;
                }
            }

            return sb.ToString();
        }

        public virtual void Compile(MoveInfo treeInfo, ScriptInfo scriptInfo, CompilingInfo compilingInfo)
        {
            throw new InvalidOperationException("WTF?!");
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CODSCRIPT.Content;

namespace CODSCRIPT
{
    public class ScriptCode
    {
        private ScriptFile _sf;
        public ScriptFile SF { get { return _sf; } }

        public DateTime CreateTime { get; private set; }
        public BaseTree CodeTree { get; private set; }

        public string SourceCode { get { return sourceCode; } }
        private string sourceCode;

        public BaseTree CompileTree { get; private set; }

        public string TEST_CodeTree { get { return CodeTree.ToString(); } }
        public string TEST_CompileTree { get { return CompileTree.ToString(); } }

        private ScriptCode()
        { }

        public ScriptCode CreateCopyForOverwrite(ScriptFile sf)
        {
            ScriptCode sc = new ScriptCode();
            sc._sf = sf;
            sc.CreateTime = CreateTime;
            sc.CodeTree = CodeTree.CreateCopy(sf);
            sc.sourceCode = sourceCode;
            sc.CompileTree = sc.CodeTree;
            return sc;
        }

        public static ScriptCode Create(ScriptFile sf, string SCFullPath, DateTime createTime)
        {
            ScriptCode sc = new ScriptCode();
            sc._sf = sf;
            sc.sourceCode = File.ReadAllText(SCFullPath, Encoding.GetEncoding(1250));
            sc.CreateTime = createTime;
                return sc;
        }

        /// <summary>
        /// Prečíta kód a vráti jeho abstraktnú reprezentáciu.
        /// </summary>
        /// <param name="sourceCode"></param>
        /// <returns></returns>
        public bool ReadCode()
        {
            try
            {
                // tokenize input string
                List<IElement> tokens = Scanner.Tokenize(this.SF, this.sourceCode);

                // create parse tree
                // and create ScriptInfo
                // and get include files info
                CodeTree = BaseTree.Parse(tokens, _sf, _sf.SI);
                return true;
            }
            catch (SyntaxException synEx)
            {
                SF.Errors.Add(new SyntaxError(synEx.Message, synEx.ErrorInfo));
                return false;
            }
        }

        /// <summary>
        /// Zkontroluje sémantiku kódu a iné veci...
        /// </summary>
        /// <param name="sourceCode"></param>
        public void CheckCode()
        {
            if (CodeTree == null)
                throw new InvalidOperationException("CodeTree");

            try
            {
                // check semantic
                CodeTree.CheckSemantic(sourceCode, this.SF.SI);

            }
            catch (SyntaxException synEx)
            {
                SF.Errors.Add(new SyntaxError(synEx.Message, synEx.ErrorInfo));
            }

            //WriteDebug(CodeTree.ToString());
        }

        #region Compiling
        public bool PrepareCompile()
        {
            if (CodeTree == null)
                throw new InvalidOperationException("CodeTree");

            if (CompileTree == null) // may have been created in CreateCopyForOverwrite
                CompileTree = CodeTree.CreateCopy(_sf);

            return CompileTree.FindOverwrites();
        }

        public bool CompileMembers()
        {
            // process usings & overwrites & constants
            return CompileTree.CompileMembers();
        }

        public void CompileCode()
        {
            // process foreach, delete regions, replace compiler constants, etc.
            CompileTree.CompileTree(this.SF.SI);
        }

        public string CompileOutput(OutputSetting outputSetting)
        {
            string outputStr = CompileTree.CompileOutput(outputSetting);
            //CompileTree = null;
            return outputStr;
        }
        #endregion

        #region SC Tools
        public int GetMemberStartPos(IMemberInfo member, ref int length)
        {
            if (member is FuncInfo)
            {
                foreach (FuncDef funcDef in CodeTree.FuncDefList)
                    if (funcDef.FuncInfo.Name.EqualCode(member.Name))
                    {
                        length = funcDef.CharLength;
                        return (int)funcDef.ImportantCharIndex;
                    }
            }
            else if (member is ConstInfo)
            {
                foreach (ConstDef constDef in CodeTree.ConstDefList)
                    if (constDef.ConstInfo.Name.EqualCode(member.Name))
                    {
                        length = constDef.CharLength;
                        return (int)constDef.ImportantCharIndex;
                    }
            }
            else if (member is UsingInfo)
            {
                foreach (UsingDef usingDef in CodeTree.UsingDefList)
                    if (usingDef.UsingInfo.Name.EqualCode(member.Name))
                    {
                        length = usingDef.CharLength;
                        return (int)usingDef.ImportantCharIndex;
                    }                
            }
            else
                throw new ArgumentException("member");

            return -1;
        }

        public IMemberInfo GetMemberForXMLDoc(int pos)
        {
            MoveInfo baseTreeInfo = new MoveInfo(this.CodeTree, SearchTree.ChildrenBlock, 0, this.SF);
            IElement found = baseTreeInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
            while (found != null)
            {
                if (pos > found.CharIndex && pos < (found.CharIndex + found.CharLength)) // in function, constant, etc.
                {
                    return null;
                }

                if (found.CharIndex > pos)
                {
                    if (found is FuncDef)
                    {
                        FuncDef funcDef = (FuncDef)found;
                        if (funcDef.XMLBlock == null)
                            return funcDef.FuncInfo;
                    }
                    else if (found is ConstDef)
                    {
                        ConstDef constDef = (ConstDef)found;
                        if (constDef.XMLBlock == null)
                            return constDef.ConstInfo;
                    }
                    return null;
                }

                found = baseTreeInfo.FindNextBlack(SearchDirection.LeftToRight);
            }
            return null;
        }
        #endregion

        /*private static void WriteDebug(string output)
        {
            string fullPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "output");
            using (FileStream fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Delete))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(output);
                }
            }
        }*/
    }
}

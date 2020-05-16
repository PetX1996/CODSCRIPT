using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class ConstName : ExpressionOperand
    {
        ConstInfo _constInfo;
        public ConstInfo ConstInfo { get { return _constInfo; } }

        public ConstName(List<IElement> elems, ConstInfo constInfo)
            : base(elems)
        {
            this._constInfo = constInfo;
        }

        public override IElement CreateCopy()
        {
            ConstName e = new ConstName(this.CopyChildren(), _constInfo);
            return e;
        }

        public override void Compile(MoveInfo treeInfo, ScriptInfo scriptInfo, CompilingInfo compilingInfo)
        {
            if (this._constInfo.SF.SFPath == "compiler")
            {
                IElement e = null;
                switch (this._constInfo.Name)
                { 
                    case "Time":
                        e = new Token(TokenType.String, DateTime.Now.ToShortTimeString());
                        break;
                    case "Date":
                        e = new Token(TokenType.String, DateTime.Now.ToShortDateString());
                        break;
                    case "DateTime":
                        e = new Token(TokenType.String, DateTime.Now.ToString());
                        break;
                    case "Line":
                        e = new Token(TokenType.Number, treeInfo.Current.LineIndex + 1);
                        break;
                    case "VersionInt":
                        e = new Token(TokenType.Number, scriptInfo.SF.Manager.Settings.VersionInt);
                        break;
                    case "VersionStr":
                        e = new Token(TokenType.String, scriptInfo.SF.Manager.Settings.VersionStr);
                        break;
                    case "TargetPlatform":
                        e = new Token(TokenType.Number, (int)scriptInfo.SF.Manager.Settings.TargetPlatform);
                        break;
                    case "TargetPlatform_Windows":
                        e = new Token(TokenType.Number, (int)TargetPlatform.Windows);
                        break;
                    case "TargetPlatform_Linux":
                        e = new Token(TokenType.Number, (int)TargetPlatform.Linux);
                        break;
                    case "TargetPlatform_LinuxNinja":
                        e = new Token(TokenType.Number, (int)TargetPlatform.LinuxNinja);
                        break;
                    case "TargetConfiguration":
                        e = new Token(TokenType.Number, (int)scriptInfo.SF.Manager.Settings.TargetConfiguration);
                        break;
                    case "TargetConfiguration_Debug":
                        e = new Token(TokenType.Number, (int)TargetConfiguration.Debug);
                        break;
                    case "TargetConfiguration_Release":
                        e = new Token(TokenType.Number, (int)TargetConfiguration.Release);
                        break;
                    case "TargetConfiguration_FinalRelease":
                        e = new Token(TokenType.Number, (int)TargetConfiguration.FinalRelease);
                        break;
                    default:
                        throw new ArgumentException("Unknown compiler constant '" + _constInfo.Name + "', " + scriptInfo.SF.ToString());
                }
                treeInfo.ReplaceCurrent(e);
            }
        }
    }
}

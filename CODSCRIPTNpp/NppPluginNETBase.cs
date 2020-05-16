using System;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace NppPluginNET
{
    class PluginBase
    {
        #region " Fields "
        internal static NppData nppData;
        internal static FuncItems _funcItems = new FuncItems();
        #endregion

        #region " Helper "
        internal static void SetCommand(int index, string commandName, NppFuncItemDelegate functionPointer)
        {
            SetCommand(index, commandName, functionPointer, new ShortcutKey(), false);
        }
        internal static void SetCommand(int index, string commandName, NppFuncItemDelegate functionPointer, ShortcutKey shortcut)
        {
            SetCommand(index, commandName, functionPointer, shortcut, false);
        }
        internal static void SetCommand(int index, string commandName, NppFuncItemDelegate functionPointer, bool checkOnInit)
        {
            SetCommand(index, commandName, functionPointer, new ShortcutKey(), checkOnInit);
        }
        internal static void SetCommand(int index, string commandName, NppFuncItemDelegate functionPointer, ShortcutKey shortcut, bool checkOnInit)
        {
            FuncItem funcItem = new FuncItem();
            funcItem._cmdID = index;
            funcItem._itemName = commandName;
            if (functionPointer != null)
                funcItem._pFunc = new NppFuncItemDelegate(functionPointer);
            if (shortcut._key != 0)
                funcItem._pShKey = shortcut;
            funcItem._init2Check = checkOnInit;
            _funcItems.Add(funcItem);
        }

        internal static IntPtr GetCurrentScintilla()
        {
            int curScintilla;
            Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_GETCURRENTSCINTILLA, 0, out curScintilla);
            return (curScintilla == 0) ? nppData._scintillaMainHandle : nppData._scintillaSecondHandle;
        }
        #endregion

        #region My Helpers
        #region XPM IMAGES
        /// <summary>
        /// Konvertuje Bitmapu PNG bitmapu do formátu XPM používaného scintillou. 
        /// </summary>
        /// <param name="png"></param>
        /// <returns></returns>
        internal static string PNG2XPM(Bitmap png)
        {
            StringBuilder sb = new StringBuilder();
            List<XPMColor> xpmColors = new List<XPMColor>();
            string[] availableNames = new string[64];
            int namesCount = 0;
            for (int i = 'a'; i <= 'z'; i++)
                availableNames[namesCount++] = ((char)i).ToString();

            for (int i = 'A'; i <= 'Z'; i++)
                availableNames[namesCount++] = ((char)i).ToString();

            for (int i = '!'; i <= '*'; i++)
                availableNames[namesCount++] = ((char)i).ToString();

            int namesLeft = 0;

            for (int y = 0; y < png.Height; y++)
            {
                for (int x = 0; x < png.Width; x++)
                {
                    Color pixel = png.GetPixel(x, y);
                    if (pixel.A == 0)
                        continue;

                    string pixHexCol = Color2XPM4bitHexSTR(pixel);
                    XPMColor xpmCol = xpmColors.Find(a => a.hexColor == pixHexCol);
                    if (xpmCol == null)
                        xpmColors.Add(new XPMColor(availableNames[namesLeft++], pixHexCol));
                }
            }

            sb.Append("/* XPM */\nstatic char * XFACE[] = {\n");
            sb.Append("\"" + png.Width + " " + png.Height + " " + (xpmColors.Count + 1) + " 1\",\n");

            sb.Append("\"  c None\",\n");
            string colorHexStr;
            for (int i = 0; i < xpmColors.Count; i++)
            {
                sb.Append("\"" + xpmColors[i].name + " c #" 
                    + XPM4bit2XPM8bitHexSTR(xpmColors[i].hexColor) + "\",\n");
            }

            for (int y = 0; y < png.Height; y++)
            {
                sb.Append("\"");
                for (int x = 0; x < png.Width; x++)
                {
                    Color pixel = png.GetPixel(x, y);

                    if (pixel.A == 0)
                    {
                        sb.Append(" ");
                    }
                    else
                    {
                        colorHexStr = Color2XPM4bitHexSTR(pixel);
                        string colorName = xpmColors.Find(a => a.hexColor == colorHexStr).name;
                        sb.Append(colorName);
                    }
                }
                sb.Append("\",\n");
            }

            sb.Remove(sb.Length - 2, 2); // remove ,\n
            sb.Append("\n};");
            return sb.ToString();
        }

        /// <summary>
        /// Konvertuje RGBA farbu na 4-bitovú RGB použiteľnú v XPM.
        /// Séria 3 bajtov zapísaných hexadecimálne.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static string Color2XPM4bitHexSTR(Color c)
        {
            int r = (int)((c.R / 256f) * 4f);
            if (r == 4) r = 3;
            int g = (int)((c.G / 256f) * 4f);
            if (g == 4) g = 3;
            int b = (int)((c.B / 256f) * 4f);
            if (b == 4) b = 3;

            return r.ToString("X2") + g.ToString("X2") + b.ToString("X2");
        }

        private static string XPM4bit2XPM8bitHexSTR(string hex4bit)
        {
            // TODO: parsuje z 10-tkovej sústavy!!! pozor na to! :)
            int r = Int32.Parse(hex4bit.Substring(0, 2));
            int g = Int32.Parse(hex4bit.Substring(2, 2));
            int b = Int32.Parse(hex4bit.Substring(4, 2));

            int newR = (int)((r / 4f) * 256f);
            int newG = (int)((g / 4f) * 256f);
            int newB = (int)((b / 4f) * 256f);

            return newR.ToString("X2") + newG.ToString("X2") + newB.ToString("X2");
        }

        private class XPMColor
        {
            public string name;
            public string hexColor;

            public XPMColor(string name, string color)
            {
                this.name = name;
                this.hexColor = color;
            }
        }
        #endregion

        internal static string GetTextRange(int startIndex, int length)
        {
            if (startIndex < 0 || length < 0)
                throw new ArgumentException("startIndex || length");

            if (length == 0)
                return string.Empty;

            // TODO: zabugovaný shit!!! pretečenie pamäti?
            /*using (Sci_TextRange tr = new Sci_TextRange(startIndex, startIndex + length, length))
            {
                Win32.SendMessage(GetCurrentScintilla(), SciMsg.SCI_GETTEXTRANGE, 0, tr.NativePointer);
                return tr.lpstrText;
            }*/
            IntPtr scintilla = PluginBase.GetCurrentScintilla();
            int docLength = (int)Win32.SendMessage(scintilla, SciMsg.SCI_GETLENGTH, 0, 0) + 1;
            StringBuilder sb = new StringBuilder(docLength);
            Win32.SendMessage(scintilla, SciMsg.SCI_GETTEXT, docLength, sb);

            return sb.ToString(startIndex, length);
        }

        internal static void ResetIndicatorInBuffer(int indicatorIndex)
        {
            IntPtr scintilla = GetCurrentScintilla();
            int length = (int)Win32.SendMessage(scintilla, SciMsg.SCI_GETLENGTH, 0, 0);

            //Win32.SendMessage(scintilla, SciMsg.SCI_CLEARDOCUMENTSTYLE, 0, 0);

            int indicatorNumber = (int)SciMsg.INDIC_CONTAINER + indicatorIndex;
            Win32.SendMessage(scintilla, SciMsg.SCI_SETINDICATORCURRENT, indicatorNumber, 0);
            Win32.SendMessage(scintilla, SciMsg.SCI_INDICATORCLEARRANGE, 0, length);

            //Win32.SendMessage(scintilla, SciMsg.SCI_INDICATORFILLRANGE, 0, 1);
        }

        internal static void SetIndicator(int indicatorIndex, int indicatorStyle, Color color, int alpha)
        {
            int indicatorNumber = (int)SciMsg.INDIC_CONTAINER + indicatorIndex;
            IntPtr scintilla = GetCurrentScintilla();
            Win32.SendMessage(scintilla, SciMsg.SCI_INDICSETSTYLE, indicatorNumber, indicatorStyle);
            Win32.SendMessage(scintilla, SciMsg.SCI_INDICSETFORE, indicatorNumber, GetColorCode(color));
            Win32.SendMessage(scintilla, SciMsg.SCI_INDICSETALPHA, indicatorNumber, alpha);
        }

        internal static void UseIndicator(int indicatorIndex, int startPosition, int length)
        {
            int indicatorNumber = (int)SciMsg.INDIC_CONTAINER + indicatorIndex;
            IntPtr scintilla = GetCurrentScintilla();
            Win32.SendMessage(scintilla, SciMsg.SCI_SETINDICATORCURRENT, indicatorNumber, 0);
            Win32.SendMessage(scintilla, SciMsg.SCI_INDICATORFILLRANGE, startPosition, length);

            //MessageBox.Show("using indicator");
        }

        internal static int GetColorCode(Color color)
        {
            // Blue Green Red
            // byte byte byte
            int c = color.B;
            c <<= 8;
            c |= color.G;
            c <<= 8;
            c |= color.R;
            return c;
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT
{
    public class NppElementInfo
    {
        public int CharIndex { get; set; }
        public int CharLength { get; set; }

        public int? ImportantCharIndex { get; set; }
        public int? ImportantCharLength { get; set; }

        public NppElementInfo(int charIndex, int charLength, int? importantCharIndex, int? importantCharLength)
        {
            CharIndex = charIndex;
            CharLength = charLength;
            ImportantCharIndex = importantCharIndex;
            ImportantCharLength = importantCharLength;
        }

        public NppElementInfo(int charIndex, int charLength)
            : this(charIndex, charLength, null, null)
        {
        }

        public void Inserted(int pos, int length)
        {
            if (pos <= CharIndex) // inserted before this
                CharIndex += length;
            else if (pos > CharIndex && pos < (CharIndex + CharLength)) // inserted in this
                CharLength += length;

            if (ImportantCharIndex != null && ImportantCharLength != null)
            {
                if (pos <= ImportantCharIndex) // inserted before this
                    ImportantCharIndex += length;
                else if (pos > ImportantCharIndex && pos < (ImportantCharIndex + ImportantCharLength)) // inserted in this
                    ImportantCharLength += length;                
            }
        }

        public void Deleted(int pos, int length)
        {
            int lastIndex = pos + length - 1;

            if (lastIndex < CharIndex) // deleted before this
                CharIndex -= length;
            else if (pos >= CharIndex && pos < (CharIndex + CharLength)) // pos is in this -> cut to end
            {
                int cutLength = Math.Min(CharIndex + CharLength, pos + length) - pos;
                CharLength -= cutLength;
            }
            else if (lastIndex >= CharIndex && lastIndex < (CharIndex + CharLength)) // lastIndex is in this -> cut to start
            {
                int cutLength = (lastIndex + 1) - Math.Max(CharIndex, pos);
                CharLength -= cutLength;
            }
            else if (pos < CharIndex && lastIndex >= (CharIndex + CharLength)) // pos is before this and lastIndex is after this -> cut all
            {
                CharLength = 0;
            }

            if (ImportantCharIndex != null && ImportantCharLength != null)
            {
                if (lastIndex < ImportantCharIndex) // deleted before this
                    ImportantCharIndex -= length;
                else if (pos >= ImportantCharIndex && pos < (ImportantCharIndex + ImportantCharLength)) // pos is in this -> cut to end
                {
                    int cutLength = Math.Min((int)ImportantCharIndex + (int)ImportantCharLength, pos + length) - pos;
                    ImportantCharLength -= cutLength;
                }
                else if (lastIndex >= ImportantCharIndex && lastIndex < (ImportantCharIndex + ImportantCharLength)) // lastIndex is in this -> cut to start
                {
                    int cutLength = (lastIndex + 1) - Math.Max((int)ImportantCharIndex, pos);
                    ImportantCharLength -= cutLength;
                }
                else if (pos < ImportantCharIndex && lastIndex >= (ImportantCharIndex + ImportantCharLength)) // pos is before this and lastIndex is after this -> cut all
                {
                    ImportantCharLength = 0;
                }                
            }
        }
    }
}

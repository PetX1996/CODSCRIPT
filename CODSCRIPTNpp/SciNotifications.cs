using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NppPluginNET;

namespace CODSCRIPTNpp
{
    public static class SciNotification
    {
        public static event SciNotificationEvent CharAdded;
        public static event SciNotificationEvent DWellStart;
        public static event SciNotificationEvent DWellEnd;
        public static event SciNotificationEvent Modified;

        internal static void Process(SCNotification nc)
        {
            switch (nc.nmhdr.code)
            {
                case (uint)SciMsg.SCN_CHARADDED:
                    if (CharAdded != null)
                        CharAdded(nc);
                    break;
                case (uint)SciMsg.SCN_DWELLSTART:
                    if (DWellStart != null)
                        DWellStart(nc);
                    break;
                case (uint)SciMsg.SCN_DWELLEND:
                    if (DWellEnd != null)
                        DWellEnd(nc);
                    break;
                case (uint)SciMsg.SCN_MODIFIED:
                    if (Modified != null)
                        Modified(nc);
                    break;
                default:
                    break;
            }
        }
    }
    public delegate void SciNotificationEvent(SCNotification nc);
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NppPluginNET;
using System.Windows.Forms;

namespace CODSCRIPTNpp
{
    public static class NppNotification
    {
        public static event NppNotificationEvent Ready;
        public static event NppNotificationEvent TBModification;
        public static event NppNotificationEvent FileBeforeClose;
        public static event NppNotificationEvent FileClosed;
        public static event NppNotificationEvent FileBeforeOpen;
        public static event NppNotificationEvent FileOpened;
        public static event NppNotificationEvent FileBeforeSave;
        public static event NppNotificationEvent FileSaved;
        public static event NppNotificationEvent Shutdown;
        public static event NppNotificationEvent BufferActivated;
        public static event NppNotificationEvent LangChanged;
        public static event NppNotificationEvent WordStylesUpdated;
        public static event NppNotificationEvent ShortcutRemapped;
        public static event NppNotificationEvent FileBeforeLoad;
        public static event NppNotificationEvent FileLoadFailed;
        public static event NppNotificationEvent DocOrderChanged;

        public static bool IsFileBeforeLoad { get; private set; } 

        internal static void Process(SCNotification nc)
        {
            switch (nc.nmhdr.code)
            {
                case (uint)NppMsg.NPPN_DOCORDERCHANGED:
                    if (DocOrderChanged != null)
                        DocOrderChanged(nc);
                    break;
                case (uint)NppMsg.NPPN_FILEBEFORELOAD:
                    IsFileBeforeLoad = true;
                    if (FileBeforeLoad != null)
                        FileBeforeLoad(nc);
                    break;
                case (uint)NppMsg.NPPN_SHORTCUTREMAPPED:
                    if (ShortcutRemapped != null)
                        ShortcutRemapped(nc);
                    break;
                case (uint)NppMsg.NPPN_WORDSTYLESUPDATED:
                    if (WordStylesUpdated != null)
                        WordStylesUpdated(nc);
                    break;
                case (uint)NppMsg.NPPN_LANGCHANGED:
                    if (LangChanged != null)
                        LangChanged(nc);
                    break;
                case (uint)NppMsg.NPPN_BUFFERACTIVATED:
                    if (BufferActivated != null)
                        BufferActivated(nc);
                    break;
                case (uint)NppMsg.NPPN_FILESAVED:
                    if (FileSaved != null)
                        FileSaved(nc);
                    break;
                case (uint)NppMsg.NPPN_FILEBEFORESAVE:
                    if (FileBeforeSave != null)
                        FileBeforeSave(nc);
                    break;
                case (uint)NppMsg.NPPN_FILEOPENED:
                    IsFileBeforeLoad = false;
                    if (FileOpened != null)
                        FileOpened(nc);
                    break;
                case (uint)NppMsg.NPPN_FILEBEFOREOPEN:
                    if (FileBeforeOpen != null)
                        FileBeforeOpen(nc);
                    break;
                case (uint)NppMsg.NPPN_FILECLOSED:
                    if (FileClosed != null)
                        FileClosed(nc);
                    break;
                case (uint)NppMsg.NPPN_TBMODIFICATION:
                    if (TBModification != null)
                        TBModification(nc);
                    break;
                case (uint)NppMsg.NPPN_READY:
                    if (Ready != null)
                        Ready(nc);
                    break;
                case (uint)NppMsg.NPPN_SHUTDOWN:
                    if (Shutdown != null)
                        Shutdown(nc);
                    break;
                case (uint)NppMsg.NPPN_FILEBEFORECLOSE:
                    if (FileBeforeClose != null)
                        FileBeforeClose(nc);
                    break;
                case (uint)NppMsg.NPPN_FILELOADFAILED:
                    if (FileLoadFailed != null)
                        FileLoadFailed(nc);
                    break;
                default:
                    break;
            }
        }
    }
    public delegate void NppNotificationEvent(SCNotification nc);
}

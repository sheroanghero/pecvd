using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

//using form = System.Windows.Forms;
using Caliburn.Micro;
using MECF.Framework.UI.Client.ClientBase;


namespace OpenSEMI.ClientBase
{
    public class DialogBox
    { 
       #region Folder Browser Dialog

        /// <summary>
        /// Folder Browser Dialog
        /// </summary>
        /// <returns>User Seleted Path</returns>
        //public static String ShowFolderBrowserDialog()
        //{
        //    form.FolderBrowserDialog dialog = new form.FolderBrowserDialog();
        //    dialog.ShowNewFolderButton = false;
        //    dialog.RootFolder = Environment.SpecialFolder.MyComputer;

        //    form.DialogResult result = dialog.ShowDialog();

        //    return result == form.DialogResult.OK ? dialog.SelectedPath : String.Empty;

        //}

        ///// <summary>
        ///// Folder Browser Dialog
        ///// </summary>
        ///// <param name="oldPath">old path</param>
        ///// <param name="rootDialog">default path</param>
        ///// <param name="isShowNewFolderButton">show new folder button</param>
        ///// <returns>User Seleted Path</returns>
        //public static String ShowFolderBrowserDialog(String oldPath, Environment.SpecialFolder rootDialog = Environment.SpecialFolder.MyComputer, Boolean isShowNewFolderButton = false)
        //{
        //    form.FolderBrowserDialog dialog = new form.FolderBrowserDialog();
        //    dialog.ShowNewFolderButton = isShowNewFolderButton;
        //    if (String.IsNullOrWhiteSpace(oldPath))
        //    {
        //        dialog.RootFolder = rootDialog;
        //    }
        //    else
        //    {
        //        dialog.SelectedPath = oldPath;
        //    }

        //    form.DialogResult result = dialog.ShowDialog();

        //    return result == form.DialogResult.OK ? dialog.SelectedPath : String.Empty;

        //}
        #endregion

        #region Simple dialog
        public static DialogButton ShowError(MESSAGE msgEnum, params object[] param)
        {
            string msg = GetMsg(msgEnum);
            return ShowError(msg, param);
        }

        public static DialogButton ShowError(string msg, params object[] param)
        {
            return ShowDialog(DialogButton.OK, DialogType.ERROR, msg, param);
        }

        public static DialogButton ShowWarning(MESSAGE msgEnum, params object[] param)
        {
            string msg = GetMsg(msgEnum);
            return ShowWarning(msg, param);
        }

        public static DialogButton ShowWarning(string msg, params object[] param)
        {
            return ShowDialog(DialogButton.OK, DialogType.WARNING, msg, param);
        }

        public static DialogButton ShowInfo(MESSAGE msgEnum, params object[] param)
        {
            string msg = GetMsg(msgEnum);
            return ShowInfo(msg, param);
        }

        public static DialogButton ShowInfo(string msg, params object[] param)
        {
            return ShowDialog(DialogButton.OK, DialogType.INFO, msg, param);
        }

        public static bool Confirm(MESSAGE msgEnum, params object[] param)
        {
            string msg = GetMsg(msgEnum);
            return Confirm(msg, param);
        }

        public static bool Confirm(string msg, params object[] param)
        {
            DialogButton btn = ShowDialog(DialogButton.Yes | DialogButton.No, DialogType.CONFIRM, msg, param);
            if (btn == DialogButton.Yes)
                return true;
            else
                return false;
        }
        #endregion

        /// <summary>
        /// get message by enmu from resource
        /// </summary>
        /// <param name="msgEnum"></param>
        /// <returns></returns>
        public static string GetMsg(MESSAGE msgEnum)
        {
            //check contain key
            var msg = Application.Current.Resources[msgEnum.ToString()] as string;
            var msgs = msg.Split(new string[] { "\\n" }, StringSplitOptions.RemoveEmptyEntries);
            string msgStr = string.Empty;
            int i = 0;
            foreach (var str in msgs)
            {
                if (i == msgs.Count() - 1)
                    msgStr += str;
                else
                    msgStr += str + Environment.NewLine;

                i++;
            }
            return msgStr;
        }

        public static DialogButton ShowDialog(DialogButton buttons, DialogType type, MESSAGE msgEnum, params object[] param)
        {
            string msg = GetMsg(msgEnum);
            return ShowDialog(buttons, type, msg, param);
        }

        public static DialogButton ShowDialog(DialogButton buttons, DialogType type, string msg, params object[] Params)
        {
            WindowManager wm = new WindowManager();
            MessageDialogViewModel dlg = new MessageDialogViewModel();
            dlg.DialogButton = buttons;
            if (Params != null && Params.Length > 0)
                msg = string.Format(msg, Params);
            dlg.Text = msg;
            dlg.DialogType = type;
            wm.ShowDialogWithNoStyle(dlg);
            return dlg.DialogResult;
        }
    }
}

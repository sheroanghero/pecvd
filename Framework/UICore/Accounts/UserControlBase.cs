using System.Windows.Controls;
using Aitex.Core.Account;
using Aitex.Core.Util;

namespace MECF.Framework.UI.Core.Accounts
{
    public static class UserControlExtender
    {
        /// <summary>
        /// 对UserControl类进行扩展
        /// </summary>
        /// <param name="userControl"></param>
        /// <returns></returns>
        public static ViewPermission GetPermission(this UserControl userControl)
        {
            var account = AccountClient.Instance.CurrentUser;
            if (account == null) return ViewPermission.Invisiable;
            var userControlName = userControl.Name;
            if (!account.Permission.ContainsKey(userControlName)) return ViewPermission.Invisiable;
            return account.Permission[userControlName];
        }
    }
}

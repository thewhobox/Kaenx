using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Helper
{
    public class ViewHelper
    {
        private static ViewHelper _instance;
        public static ViewHelper Instance
        {
            get
            {
                if (_instance == null) _instance = new ViewHelper();
                return _instance;
            }
        }

        // Drag Helper
        public object DragItem { get; set; }


        // Notification Helper
        public delegate void ShowNotificationHandler(string view, string text, int duration, InfoBarSeverity type);
        public event ShowNotificationHandler OnShowNotification;
        public void ShowNotification(string view, string text, int duration = -1, InfoBarSeverity type = InfoBarSeverity.Success) { OnShowNotification?.Invoke(view, text, duration, type); }
    }
}
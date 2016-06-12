using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace Client
{
    public class WPFProperties : MetroWindow
    {
        public static MetroDialogSettings metroDialogSettings = new MetroDialogSettings()
        {
            AffirmativeButtonText = "OK",
            NegativeButtonText = "CANCEL",
            AnimateHide = true,
            AnimateShow = true,
            ColorScheme = MetroDialogColorScheme.Accented
        };
    }
}

using System.Windows;
using System.Windows.Controls;
using WMS_RadiadoresLemos_WPF.src.Views;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public static class UserControlNavigationExtensions
    {
        public static void NavigateTo(this UserControl control, UserControl targetControl, string title, string iconPath)
        {
            var mainWindow = Window.GetWindow(control) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.NavigationService.Navigate(targetControl, title, iconPath);
            }
        }
    }
} 
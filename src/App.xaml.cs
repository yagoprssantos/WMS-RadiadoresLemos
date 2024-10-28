using System;
using System.Windows;

namespace WMS_RadiadoresLemos_WPF.src
{
    public partial class App : Application
    {
        [STAThread]
        public static void Main()
        {
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}

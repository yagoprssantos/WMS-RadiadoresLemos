using System;
using System.IO; // Adicione esta linha
using System.Windows;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class App : Application
    {
        private const string ThemeFilePath = "theme.txt";
        private const string DefaultTheme = "LightTheme";

        protected override void OnStartup(StartupEventArgs e)
        {
            // Adiciona o usuário administrador antes de qualquer outra operação
            AddAdminUser.AddAdmin();
            
            base.OnStartup(e);
            LoadTheme();
        }

        private void LoadTheme()
        {
            string themeName = DefaultTheme;

            if (File.Exists(ThemeFilePath))
            {
                themeName = File.ReadAllText(ThemeFilePath);
            }

            ApplyTheme(themeName);
        }

        public static void ApplyTheme(string themeName)
        {
            var dict = new ResourceDictionary
            {
                // TODO: Comentado para desenvolver melhor o Style da aplicação
                // Source = new Uri($"src/Resources/Themes/{themeName}.xaml", UriKind.Relative)

                Source = new Uri("src/Resources/Style.xaml", UriKind.Relative)
            };

            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }
    }
}

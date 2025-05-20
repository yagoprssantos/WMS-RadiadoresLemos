using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using WMS_RadiadoresLemos_WPF.src.Services;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.Views;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class ConfiguracaoUserControl : UserControl
    {
        private const string ThemeFilePath = "theme.txt";
        private MainWindow _mainWindow;

        public ConfiguracaoUserControl()
        {
            InitializeComponent();
            SetCurrentThemeSelection();
            _mainWindow = Application.Current.MainWindow as MainWindow;
        }

        private void SetCurrentThemeSelection()
        {
            if (File.Exists(ThemeFilePath))
            {
                string currentTheme = File.ReadAllText(ThemeFilePath).Trim();
                switch (currentTheme)
                {
                    case "LightTheme":
                        ThemeSelector.SelectedItem = LightTheme;
                        break;
                    case "DarkTheme":
                        ThemeSelector.SelectedItem = DarkTheme;
                        break;
                    case "MidnightTheme":
                        ThemeSelector.SelectedItem = MidnightTheme;
                        break;
                }
            }
        }

        private void SaveTheme(string themeName)
        {
            File.WriteAllText(ThemeFilePath, themeName);
        }

        private void SwitchToTheme(string themeName)
        {
            var uri = new Uri($"/src/Resources/Themes/{themeName}.xaml", UriKind.Relative);
            var resourceDict = Application.LoadComponent(uri) as ResourceDictionary;
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(resourceDict);
        }

        private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeSelector.SelectedItem is ComboBoxItem selectedItem)
            {
                string themeName = selectedItem.Name;
                SaveTheme(themeName);
                SwitchToTheme(themeName);
            }
        }

        private void BtnUsuarios_Click(object sender, RoutedEventArgs e)
        {
            // Atualiza os ícones usando GetImageName
            IconUsuarios.Source = new BitmapImage(new Uri($"/src/Resources/Icons/Selected/{GetImageName("IconUsuarios", "Selected")}.png", UriKind.Relative));
            IconBancoDados.Source = new BitmapImage(new Uri($"/src/Resources/Icons/NotSelected/{GetImageName("IconBancoDados", "NotSelected")}.png", UriKind.Relative));

            // Atualiza o conteúdo
            ContentArea.Content = new UsuariosUserControl();
        }

        private void BtnBancoDados_Click(object sender, RoutedEventArgs e)
        {
            // Atualiza os ícones usando GetImageName
            IconBancoDados.Source = new BitmapImage(new Uri($"/src/Resources/Icons/Selected/{GetImageName("IconBancoDados", "Selected")}.png", UriKind.Relative));
            IconUsuarios.Source = new BitmapImage(new Uri($"/src/Resources/Icons/NotSelected/{GetImageName("IconUsuarios", "NotSelected")}.png", UriKind.Relative));

            // Atualiza o conteúdo
            ContentArea.Content = new BancoDadosUserControl();
        }

        private string GetImageName(string iconName, string state)
        {
            return iconName switch
            {
                "IconUsuarios" => state == "Selected" ? "UsuárioS" : "UsuárioNS",
                "IconBancoDados" => state == "Selected" ? "DataCenterS" : "DataCenterNS",
                _ => throw new ArgumentException("Nome de ícone desconhecido", nameof(iconName))
            };
        }

        private void BtnUsuarios_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            IconUsuarios.Source = new BitmapImage(new Uri("/src/Resources/Icons/Selected/UsuárioS.png", UriKind.Relative));
        }

        private void BtnUsuarios_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            IconUsuarios.Source = new BitmapImage(new Uri("/src/Resources/Icons/NotSelected/UsuárioNS.png", UriKind.Relative));
        }

        private void BtnBancoDados_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            IconBancoDados.Source = new BitmapImage(new Uri("/src/Resources/Icons/Selected/DataCenterS.png", UriKind.Relative));
        }

        private void BtnBancoDados_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            IconBancoDados.Source = new BitmapImage(new Uri("/src/Resources/Icons/NotSelected/DataCenterNS.png", UriKind.Relative));
        }

        private void BtnSalvarAplicar_Click(object sender, RoutedEventArgs e)
        {
            if (ThemeSelector.SelectedItem is ComboBoxItem selectedItem)
            {
                string themeName = selectedItem.Name;
                SaveTheme(themeName);
                SwitchToTheme(themeName);
                _mainWindow?.Reload();
            }
        }
    }
}

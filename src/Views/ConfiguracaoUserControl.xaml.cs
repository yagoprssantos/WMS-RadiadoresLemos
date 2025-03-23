using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class ConfiguracaoUserControl : UserControl
    {
        private const string ThemeFilePath = "theme.txt";

        public ConfiguracaoUserControl()
        {
            InitializeComponent();
            SetCurrentThemeSelection();
        }

        private void BtnUsuarios_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new UsuariosUserControl();
        }

        private void BtnBancoDados_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new BancoDadosUserControl();
        }

        // Altera tema quando o usuário seleciona um novo tema
        private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeSelector.SelectedItem is ComboBoxItem selectedItem)
            {
                string themeName = selectedItem.Name;
                SwitchToTheme(themeName);
                SaveTheme(themeName);
                ReloadWindow();
            }
        }

        // Troca o tema do aplicativo
        private void SwitchToTheme(string themeName)
        {
            App.ApplyTheme(themeName);
        }

        // Salva o tema selecionado em um arquivo
        private void SaveTheme(string themeName)
        {
            File.WriteAllText(ThemeFilePath, themeName);
        }

        // Define a seleção do ComboBox de acordo com o tema atual
        private void SetCurrentThemeSelection()
        {
            string themeName = "LightTheme"; // Tema padrão

            if (File.Exists(ThemeFilePath))
            {
                themeName = File.ReadAllText(ThemeFilePath);
            }

            foreach (ComboBoxItem item in ThemeSelector.Items)
            {
                if (item.Name == themeName)
                {
                    ThemeSelector.SelectedItem = item;
                    break;
                }
            }
        }

        // Recarrega a MainWindow para aplicar o novo tema
        private void ReloadWindow()
        {
            Window mainWindow = Window.GetWindow(this);
            mainWindow.Content = new MainWindow();
            mainWindow.Show();

            Window.GetWindow(this).Close();
        }
    }
}

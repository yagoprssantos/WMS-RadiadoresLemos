using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
            // Atualiza os ícones usando GetImageName
            IconUsuarios.Source = new BitmapImage(new Uri($"/assets/Icons/Selected/{GetImageName("IconUsuarios", "Selected")}.png", UriKind.Relative));
            IconBancoDados.Source = new BitmapImage(new Uri($"/assets/Icons/NotSelected/{GetImageName("IconBancoDados", "NotSelected")}.png", UriKind.Relative));

            // Atualiza o conteúdo
            ContentArea.Content = new UsuariosUserControl();
        }

        private void BtnBancoDados_Click(object sender, RoutedEventArgs e)
        {
            // Atualiza os ícones usando GetImageName
            IconBancoDados.Source = new BitmapImage(new Uri($"/assets/Icons/Selected/{GetImageName("IconBancoDados", "Selected")}.png", UriKind.Relative));
            IconUsuarios.Source = new BitmapImage(new Uri($"/assets/Icons/NotSelected/{GetImageName("IconUsuarios", "NotSelected")}.png", UriKind.Relative));

            // Atualiza o conteúdo
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

        // Botão Salvar e Aplicar
        private void BtnSalvarAplicar_Click(object sender, RoutedEventArgs e)
        {
            if (ThemeSelector.SelectedItem is ComboBoxItem selectedItem)
            {
                string themeName = selectedItem.Name;
                SaveTheme(themeName);
                SwitchToTheme(themeName);
                MainWindow._instance?.Reload(); // Chama a função para recarregar a janela
            }
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
            IconUsuarios.Source = new BitmapImage(new Uri("/assets/Icons/Selected/UsuárioS.png", UriKind.Relative));
        }

        private void BtnUsuarios_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            IconUsuarios.Source = new BitmapImage(new Uri("/assets/Icons/NotSelected/UsuárioNS.png", UriKind.Relative));
        }

        private void BtnBancoDados_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            IconBancoDados.Source = new BitmapImage(new Uri("/assets/Icons/Selected/DataCenterS.png", UriKind.Relative));
        }

        private void BtnBancoDados_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            IconBancoDados.Source = new BitmapImage(new Uri("/assets/Icons/NotSelected/DataCenterNS.png", UriKind.Relative));
        }
    }
}

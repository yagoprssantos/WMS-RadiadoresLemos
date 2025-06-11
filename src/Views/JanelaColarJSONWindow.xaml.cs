using System.Windows;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class JanelaColarJSONWindow : Window
    {
        public string JSONColado { get; private set; } = "";

        public JanelaColarJSONWindow()
        {
            InitializeComponent();
            TxtJSON.Focus();
        }

        private void ImportarDados_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtJSON.Text))
            {
                MessageBox.Show("Cole o JSON da aplicação web primeiro!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            JSONColado = TxtJSON.Text.Trim();
            DialogResult = true;
            Close();
        }

        private void Limpar_Click(object sender, RoutedEventArgs e)
        {
            TxtJSON.Clear();
            TxtJSON.Focus();
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
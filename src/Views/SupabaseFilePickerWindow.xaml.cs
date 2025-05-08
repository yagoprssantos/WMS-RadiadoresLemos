using System.Collections.Generic;
using System.Windows;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class SupabaseFilePickerWindow : Window
    {
        public SupabaseArquivo? ArquivoSelecionado { get; private set; }

        public SupabaseFilePickerWindow(List<SupabaseArquivo> arquivos)
        {
            InitializeComponent();
            ArquivosDataGrid.ItemsSource = arquivos;
        }

        private void SelecionarButton_Click(object sender, RoutedEventArgs e)
        {
            ArquivoSelecionado = ArquivosDataGrid.SelectedItem as SupabaseArquivo;
            if (ArquivoSelecionado == null)
            {
                MessageBox.Show("Por favor, selecione um arquivo.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

                DialogResult = true;
                Close();
            }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
            {
            DialogResult = false;
            Close();
        }
    }
}

using System.Collections.Generic;
using System.Windows;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class SupabaseFilePickerWindow : Window
    {
        public SupabaseArquivo ArquivoSelecionado { get; private set; }

        public SupabaseFilePickerWindow(List<SupabaseArquivo> arquivos)
        {
            InitializeComponent();
            ArquivosListBox.ItemsSource = arquivos;
        }

        private void ImportarSelecionado_Click(object sender, RoutedEventArgs e)
        {
            ArquivoSelecionado = ArquivosListBox.SelectedItem as SupabaseArquivo;
            if (ArquivoSelecionado != null)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Selecione um arquivo para importar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}

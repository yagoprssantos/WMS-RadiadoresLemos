using System.Collections.Generic;
using System.Windows;
using WMS_RadiadoresLemos_WPF.src.Models;
using System.Windows.Data;
using System.Globalization;

namespace WMS_RadiadoresLemos_WPF
{
    public class BytesToMBConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                if (bytes == 0) return "0 MB";
                
                double mb = bytes / (1024.0 * 1024.0);
                return $"{mb:F2} MB";
            }
            return "0 MB";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

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

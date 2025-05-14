using System.Collections.Generic;
using System.Windows;

namespace WMS_RadiadoresLemos_WPF.Views
{
    public partial class AzureFilePickerWindow : Window
    {
        public string ArquivoSelecionado { get; private set; }

        public AzureFilePickerWindow(List<string> arquivos)
        {
            InitializeComponent();
            FilesListView.ItemsSource = arquivos;
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (FilesListView.SelectedItem != null)
            {
                ArquivoSelecionado = FilesListView.SelectedItem.ToString();
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Por favor, selecione um arquivo.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
} 
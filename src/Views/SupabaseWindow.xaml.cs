using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class SupabaseWindow : Window
    {
        private List<SupabaseArquivo> arquivos;

        public SupabaseWindow()
        {
            InitializeComponent();
            CarregarArquivos();
        }

        private async void CarregarArquivos()
        {
            try
            {
                arquivos = await SupabaseUploader.ListarArquivosAsync();
                ArquivosDataGrid.ItemsSource = arquivos;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar arquivos: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BaixarButton_Click(object sender, RoutedEventArgs e)
        {
            var arquivoSelecionado = ArquivosDataGrid.SelectedItem as SupabaseArquivo;
            if (arquivoSelecionado == null)
            {
                MessageBox.Show("Por favor, selecione um arquivo para baixar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = arquivoSelecionado.name,
                    Filter = "Arquivos de Banco de Dados (*.db)|*.db|Todos os arquivos (*.*)|*.*"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await SupabaseUploader.DownloadFileAsync(arquivoSelecionado.id, saveFileDialog.FileName);
                    MessageBox.Show("Arquivo baixado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao baixar arquivo: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 
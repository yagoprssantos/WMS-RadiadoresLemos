using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Globalization;
using WMS_RadiadoresLemos_WPF.src.Services;
using System.Linq;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public class DateTimeToLocalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.ToLocalTime();
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.ToUniversalTime();
            }
            return value;
        }
    }

    public partial class SupabaseWindow : Window
    {
        private List<SupabaseArquivo> arquivos;

        public SupabaseWindow()
        {
            InitializeComponent();
            _ = CarregarArquivos();
        }

        private async Task CarregarArquivos()
        {
            try
            {
                LoadingProgress.Visibility = Visibility.Visible;
                StatusText.Text = "Carregando arquivos...";

                arquivos = await SupabaseUploader.ListarArquivosAsync();
                
                if (arquivos == null)
                {
                    MessageBox.Show("Erro: A lista de arquivos retornou nula.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (arquivos.Count == 0)
                {
                    MessageBox.Show("Nenhum arquivo encontrado no bucket do Supabase.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ArquivosDataGrid.ItemsSource = arquivos;
                StatusText.Text = $"{arquivos.Count} arquivo(s) encontrado(s)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar arquivos: {ex.Message}\n\nDetalhes: {ex}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Erro ao carregar arquivos";
            }
            finally
            {
                LoadingProgress.Visibility = Visibility.Collapsed;
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
                    Filter = "Arquivos de Banco de Dados (*.db)|*.db|Todos os arquivos (*.*)|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Title = "Salvar arquivo como"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Mostra a barra de progresso
                    var progressBar = new ProgressBar
                    {
                        Height = 20,
                        Width = 300,
                        Minimum = 0,
                        Maximum = 100,
                        Value = 0
                    };

                    var progressWindow = new Window
                    {
                        Title = "Baixando arquivo...",
                        Width = 350,
                        Height = 100,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        Content = new StackPanel
                        {
                            Margin = new Thickness(10),
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = "Baixando arquivo...",
                                    Margin = new Thickness(0, 0, 0, 10)
                                },
                                progressBar
                            }
                        }
                    };

                    progressWindow.Show();

                    try
                    {
                        await SupabaseUploader.DownloadFileAsync(arquivoSelecionado.name, saveFileDialog.FileName);
                        progressWindow.Close();
                        MessageBox.Show("Arquivo baixado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        progressWindow.Close();
                        MessageBox.Show($"Erro ao baixar arquivo: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao baixar arquivo: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeletarButton_Click(object sender, RoutedEventArgs e)
        {
            var arquivoSelecionado = ArquivosDataGrid.SelectedItem as SupabaseArquivo;
            if (arquivoSelecionado == null)
            {
                MessageBox.Show("Por favor, selecione um arquivo para deletar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirmacao = MessageBox.Show(
                $"Tem certeza que deseja deletar o arquivo '{arquivoSelecionado.name}'?\n\n" +
                "Esta ação não pode ser desfeita!",
                "Confirmar exclusão",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmacao == MessageBoxResult.Yes)
            {
                try
                {
                    LoadingProgress.Visibility = Visibility.Visible;
                    StatusText.Text = "Deletando arquivo...";

                    await SupabaseUploader.DeletarArquivoAsync(arquivoSelecionado.fullPath);
                    
                    // Aguarda um momento para garantir que o Supabase processou a deleção
                    await Task.Delay(1000);
                    
                    // Atualiza a lista de arquivos
                    await CarregarArquivos();
                    
                    // Verifica se o arquivo ainda existe
                    var arquivosAtualizados = await SupabaseUploader.ListarArquivosAsync();
                    var arquivoAindaExiste = arquivosAtualizados.Any(a => a.fullPath == arquivoSelecionado.fullPath);
                    
                    if (arquivoAindaExiste)
                    {
                        MessageBox.Show(
                            "O arquivo não foi deletado corretamente. Por favor, tente novamente ou verifique as permissões.",
                            "Erro",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show("Arquivo deletado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao deletar arquivo: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    LoadingProgress.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 
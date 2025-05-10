using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Globalization;
using WMS_RadiadoresLemos_WPF.src.Services;

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

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using WMS_RadiadoresLemos_WPF.src.Models;
using Microsoft.Win32;
using ClosedXML.Excel;
using System.IO;
using System.Windows;
using WMS_RadiadoresLemos_WPF.src.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DocumentFormat.OpenXml.Packaging;
using WMS_RadiadoresLemos_WPF.src.Views;
using System.Windows.Media;
using System.Diagnostics;
using LiteDB;
using System.Net.Http.Headers;
using System.Net.Http;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class BancoDadosUserControl : UserControl
    {
        private List<object> dadosFiltrados = new List<object>();
        private bool dadosCarregados = false;
        private List<string> tabelasSelecionadas = new List<string>();
        private static readonly string[] TabelasDisponiveis = { "usuarios", "produtos", "historico", "movimentacoes" };

        public BancoDadosUserControl()
        {
            InitializeComponent();
            DataContext = this;
            SetupLinks();
        }

        // Método para configurar botões de links (arquivos locais e banco de dados)
        private void SetupLinks()
        {
            // Configura o evento do botão para abrir arquivos locais
            var abrirArquivosLocaisButton = FindName("AbrirArquivosLocaisButton") as Button;
            if (abrirArquivosLocaisButton != null)
            {
                abrirArquivosLocaisButton.Click += AbrirArquivosLocais_Click;
            }

            // Configura o evento do botão para abrir o OneDrive
            var abrirOneDriveButton = FindName("AbrirOneDriveButton") as Button;
            if (abrirOneDriveButton != null)
            {
                abrirOneDriveButton.Click += AbrirOneDrive_Click;
            }

            // Configura o evento do botão para listar arquivos do Supabase
            var listarArquivosSupabaseButton = FindName("ListarArquivosSupabaseButton") as Button;
            if (listarArquivosSupabaseButton != null)
            {
                listarArquivosSupabaseButton.Click += ListarArquivosSupabaseButton_Click;
            }
        }

        private void AbrirArquivosLocais_Click(object sender, RoutedEventArgs e)
        {
            // Abre o diretório de arquivos locais
            Process.Start(new ProcessStartInfo
            {
                // Diretório "DadosBancoDeDadosOffline" dentro do diretório atual do projeto
                FileName = Path.GetDirectoryName(DatabaseConnect.GetDatabasePath()),

                // Abre o diretório no explorador de arquivos
                UseShellExecute = true
            });
        }

        private void AbrirOneDrive_Click(object sender, RoutedEventArgs e)
        {
            // Abre o OneDrive
            Process.Start("explorer.exe", "shell:OneDrive");
        }

        // Evento disparado para visualizar dados em Excel
        private void VisualizarExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var excelWindow = new ExcelWindow();
            excelWindow.ShowDialog();
        }

        // Adiciona o evento de clique ao botão "Abrir Menu Tabelas"
        private void AbrirMenuTabelasButton_Click(object sender, RoutedEventArgs e)
        {
            var menuTabelasWindow = new MenuTabelasWindow();
            menuTabelasWindow.ShowDialog();
        }

        private async void ExportarSupaButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Selecione o arquivo .db para enviar",
                Filter = "Arquivo de Banco de Dados (*.db)|*.db",
                InitialDirectory = @"C:\Users\username\source\repos\WMS-RadiadoresLemos\src\Resources",
                RestoreDirectory = true
            };

            if (dialog.ShowDialog() == true)
            {
                string caminhoOriginal = dialog.FileName;
                string dataHoraFormatada = DateTime.Now.ToString("ddMMyyyy_HHmmss");
                string novoNome = $"Database_{dataHoraFormatada}{Path.GetExtension(caminhoOriginal)}";
                string caminhoTemp = Path.Combine(Path.GetTempPath(), novoNome);
                File.Copy(caminhoOriginal, caminhoTemp, true);

                try
                {
                    ShowProgressBar.Visibility = Visibility.Visible;
                    ProgressBarMessage.Text = "Exportando arquivo .db para Supabase...";

                    await SupabaseUploader.UploadFileAsync(caminhoTemp);

                    ProgressBarMessage.Text = "✅ Arquivo enviado com sucesso!";
                    MessageBox.Show("✅ Arquivo enviado com sucesso para o Supabase!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    ProgressBarMessage.Text = "❌ Erro ao exportar arquivo!";
                    MessageBox.Show($"Erro ao exportar arquivo: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    ShowProgressBar.Visibility = Visibility.Collapsed;
                    if (File.Exists(caminhoTemp))
                    {
                        File.Delete(caminhoTemp);
                    }
                }
            }
        }

        private async void ListarArquivosSupabaseButton_Click(object sender, RoutedEventArgs e)
        {
            string supabaseUrl = "https://knuqicziazoirikljxcg.supabase.co";
            string supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImtudXFpY3ppYXpvaXJpa2xqeGNnIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDQwNDU4MTAsImV4cCI6MjA1OTYyMTgxMH0.9GHg6a_YjO0jN3Mf8Wvjj0aC50j_HeH3LZCw_bKIzqg";
            string bucket = "boletwash";
            
            try
            {
                ShowProgressBar.Visibility = Visibility.Visible;
                ProgressBarMessage.Text = "🔍 Listando arquivos no Supabase...";

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", supabaseKey);

                var requestBody = new
                {
                    prefix = "",
                    limit = 100,
                    offset = 0,
                    sortBy = new { column = "name", order = "asc" }
                };

                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{supabaseUrl}/storage/v1/object/list/{bucket}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var arquivos = System.Text.Json.JsonSerializer.Deserialize<List<SupabaseArquivo>>(responseContent);

                    if (arquivos != null && arquivos.Count > 0)
                    {
                        string nomes = string.Join("\n", arquivos.Select(a => $"📄 {a.name}"));
                        MessageBox.Show($"Arquivos encontrados:\n\n{nomes}", "Arquivos no Supabase", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Nenhum arquivo encontrado no bucket.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    var erro = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Erro ao listar arquivos: {response.StatusCode}\n{erro}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao conectar com Supabase:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private string EncontrarBancoMaisRecente()
        {
            string diretorioBanco = Path.GetDirectoryName(DatabaseConnect.GetDatabasePath());
            if (string.IsNullOrEmpty(diretorioBanco) || !Directory.Exists(diretorioBanco))
            {
                return null;
            }

            var arquivos = Directory.GetFiles(diretorioBanco, "*.db");
            if (arquivos.Length == 0)
            {
                return null;
            }

            return arquivos.OrderByDescending(f => File.GetLastWriteTime(f)).First();
        }

        private async Task FazerBackupAutomatico()
        {
            try
            {
                string arquivoMaisRecente = EncontrarBancoMaisRecente();
                if (string.IsNullOrEmpty(arquivoMaisRecente))
                {
                    Console.WriteLine("❌ Nenhum arquivo de banco de dados encontrado para backup.");
                    return;
                }

                string dataHoraFormatada = DateTime.Now.ToString("ddMMyyyy_HHmmss");
                string novoNome = $"Database_{dataHoraFormatada}{Path.GetExtension(arquivoMaisRecente)}";
                string caminhoTemp = Path.Combine(Path.GetTempPath(), novoNome);

                // Copia o arquivo para uma pasta temporária
                File.Copy(arquivoMaisRecente, caminhoTemp, true);

                // Faz o upload para o Supabase
                await SupabaseUploader.UploadFileAsync(caminhoTemp);
                Console.WriteLine($"✅ Backup automático realizado com sucesso: {Path.GetFileName(arquivoMaisRecente)}");

                // Limpa o arquivo temporário
                if (File.Exists(caminhoTemp))
                {
                    File.Delete(caminhoTemp);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao realizar backup automático: {ex.Message}");
            }
        }

        // Método para ser chamado quando o programa for fechado
        public async Task OnProgramClosing()
        {
            await FazerBackupAutomatico();
        }
    }

    public class SupabaseArquivo
    {
        public string name { get; set; }
        public string id { get; set; }
        public string bucket_id { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? created_at { get; set; }
    }

    public static class SupabaseUploader
    {
        private static readonly string supabaseUrl = "https://knuqicziazoirikljxcg.supabase.co";
        private static readonly string supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImtudXFpY3ppYXpvaXJpa2xqeGNnIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDQwNDU4MTAsImV4cCI6MjA1OTYyMTgxMH0.9GHg6a_YjO0jN3Mf8Wvjj0aC50j_HeH3LZCw_bKIzqg";
        private static readonly string bucket = "boletwash";

        public static async Task UploadFileAsync(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var uploadUrl = $"{supabaseUrl}/storage/v1/object/{bucket}/{fileName}";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", supabaseKey);

            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var content = new ByteArrayContent(fileBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var response = await httpClient.PostAsync(uploadUrl, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"✅ Upload de '{fileName}' feito com sucesso!");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Erro ao fazer upload: {response.StatusCode} - {error}");
                throw new Exception($"Erro ao fazer upload: {response.StatusCode} - {error}");
            }
        }
    }
}


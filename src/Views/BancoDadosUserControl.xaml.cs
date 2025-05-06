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
using Supabase;

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


        }

        private void AbrirArquivosLocais_Click(object sender, RoutedEventArgs e)
        {
            // Abre o diretório no explorador de arquivos
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.GetDirectoryName(DatabaseConnect.GetDatabasePath()),
                UseShellExecute = true
            });
        }

        private async void ListarSupabase_Click(object sender, RoutedEventArgs e)
        {
            // Abre a janela de listagem do Supabase
            var supabaseWindow = new SupabaseWindow();
            supabaseWindow.ShowDialog();
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
            try
            {
                ShowProgressBar.Visibility = Visibility.Visible;
                ProgressBarMessage.Text = "🔍 Listando arquivos no Supabase...";

                var arquivos = await SupabaseUploader.ListarArquivosAsync();

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

        private void AbrirSupabaseNoNavegador_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://supabase.com/dashboard/project/knuqicziazoirikljxcg/storage/buckets/boletwash",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir o navegador: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ImportarBancoDados_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowProgressBar.Visibility = Visibility.Visible;
                ProgressBarMessage.Text = "🔍 Buscando arquivos no Supabase...";

                var arquivos = await SupabaseUploader.ListarArquivosAsync();

                if (arquivos == null || arquivos.Count == 0)
                {
                    MessageBox.Show("Nenhum arquivo de banco de dados encontrado no Supabase.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Abre a janela para o usuário escolher o arquivo
                var pickerWindow = new SupabaseFilePickerWindow(arquivos);
                var resultado = pickerWindow.ShowDialog();

                if (resultado != true || pickerWindow.ArquivoSelecionado == null)
                    return;

                var arquivo = pickerWindow.ArquivoSelecionado;
                string bancoAtual = DatabaseConnect.GetDatabasePath();

                var confirmacao = MessageBox.Show(
                    "⚠️ Atenção! Esta operação irá substituir o banco de dados atual.\n\n" +
                    $"Banco atual: {Path.GetFileName(bancoAtual)}\n" +
                    $"Novo banco: {arquivo.name}\n\n" +
                    "Deseja continuar?",
                    "Confirmação de Importação",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmacao == MessageBoxResult.Yes)
                {
                    try
                    {
                        ProgressBarMessage.Text = $"⬇️ Baixando {arquivo.name}...";

                        // Cria um arquivo temporário para o download
                        string caminhoTemp = Path.Combine(Path.GetTempPath(), "Database.db");
                        await SupabaseUploader.DownloadFileAsync(arquivo.name, caminhoTemp);

                        // Fecha todas as conexões com o banco atual
                        DatabaseConnect.Disconnect();

                        // Aguarda um momento para garantir que todas as conexões foram fechadas
                        await Task.Delay(1000);

                        // Faz backup do banco atual antes de substituir usando o sistema padrão
                        if (File.Exists(bancoAtual))
                        {
                            DatabaseBackup.CreateBackup(bancoAtual);
                        }

                        // Tenta substituir o banco de dados
                        try
                        {
                            // Se o arquivo existir, tenta deletá-lo primeiro
                            if (File.Exists(bancoAtual))
                            {
                                File.Delete(bancoAtual);
                            }

                            // Copia o novo arquivo
                            File.Copy(caminhoTemp, bancoAtual, true);

                            // Verifica se o banco é válido
                            try
                            {
                                using (var testDb = new LiteDatabase(bancoAtual))
                                {
                                    // Se chegou aqui, o banco está íntegro
                                    testDb.Dispose();
                                }

                                ProgressBarMessage.Text = "✅ Banco de dados importado com sucesso!";

                                // Obtém informações do backup mais recente
                                var backupDir = Path.Combine(Path.GetDirectoryName(bancoAtual), "local");
                                var backups = Directory.GetFiles(backupDir, "Database_v*_*.db")
                                    .OrderByDescending(f => File.GetLastWriteTime(f))
                                    .ToList();

                                if (!backups.Any())
                                {
                                    MessageBox.Show(
                                        "❌ Erro: Nenhum backup encontrado na pasta local.",
                                        "Erro",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                                    return;
                                }

                                var successWindow = new ImportSuccessWindow(
                                    Path.GetFileName(backups.First()),
                                    File.GetLastWriteTime(backups.First()),
                                    caminhoTemp,
                                    bancoAtual);

                                successWindow.ShowDialog();

                                if (!successWindow.Confirmado)
                                {
                                    // Se o usuário cancelou, não faz nada
                                    return;
                                }
                            }
                            catch (Exception ex)
                            {
                                // Se o banco estiver corrompido, restaura o backup mais recente
                                var backupDir = Path.Combine(Path.GetDirectoryName(bancoAtual), "local");
                                var backups = Directory.GetFiles(backupDir, "Database_v*_*.db")
                                    .OrderByDescending(f => File.GetLastWriteTime(f))
                                    .ToList();

                                if (backups.Any())
                                {
                                    File.Copy(backups.First(), bancoAtual, true);
                                }

                                MessageBox.Show(
                                    $"❌ O banco de dados importado está corrompido:\n{ex.Message}\n\n" +
                                    "O banco anterior foi restaurado.",
                                    "Erro",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                $"❌ Erro ao substituir o banco de dados:\n{ex.Message}\n\n" +
                                "Tente fechar o programa e tentar novamente.",
                                "Erro",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"❌ Erro ao importar banco de dados:\n{ex.Message}",
                            "Erro",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Erro ao importar banco de dados:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                ShowProgressBar.Visibility = Visibility.Collapsed;
            }
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

        public static async Task<List<SupabaseArquivo>> ListarArquivosAsync()
        {
            var client = new Supabase.Client(supabaseUrl, supabaseKey);
            await client.InitializeAsync();

            var response = await client.Storage
                .From(bucket)
                .List();

            return response.Select(item => new SupabaseArquivo
            {
                name = item.Name,
                id = item.Id,
                bucket_id = item.BucketId,
                created_at = item.CreatedAt,
                updated_at = item.UpdatedAt
            }).ToList();
        }

        public static async Task DownloadFileAsync(string fileId, string destinationPath)
        {
            var client = new Supabase.Client(supabaseUrl, supabaseKey);
            await client.InitializeAsync();

            var response = await client.Storage
                .From(bucket)
                .Download(fileId, null);

            await File.WriteAllBytesAsync(destinationPath, response);
        }

        public static async Task UploadFileAsync(string filePath)
        {
            var client = new Supabase.Client(supabaseUrl, supabaseKey);
            await client.InitializeAsync();

            var fileName = Path.GetFileName(filePath);
            var fileBytes = await File.ReadAllBytesAsync(filePath);

            await client.Storage
                .From(bucket)
                .Upload(fileBytes, fileName, new Supabase.Storage.FileOptions
                {
                    CacheControl = "3600",
                    Upsert = true
                });
        }
    }
}


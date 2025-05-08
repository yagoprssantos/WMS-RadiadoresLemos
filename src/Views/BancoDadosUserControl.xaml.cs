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
using System.Text.Json;
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
            AtualizarInformacoes();
        }

        // Acessar arquivos
        private void AbrirArquivosLocais_Click(object sender, RoutedEventArgs e)
        {
            // Abre o diretório no explorador de arquivos
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.GetDirectoryName(DatabaseConnect.GetDatabasePath()),
                UseShellExecute = true
            });
        }
        private void AbrirSupabase_Click(object sender, RoutedEventArgs e)
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

        // Método para abrir a janela de listagem do Supabase
        private void ListarSupabase_Click(object sender, RoutedEventArgs e)
        {
            // Abre a janela de listagem do Supabase
            var supabaseWindow = new SupabaseWindow();
            supabaseWindow.ShowDialog();
        }


        // Backup
        // Método para importar dados do banco de dados para a aplicação
        private async void ImportarBancoDadosButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowProgressBar.Visibility = Visibility.Visible;
                ProgressBarMessage.Text = "Buscando arquivos no Supabase...";

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
                    "ATENÇÃO!\n" +
                    "Esta operação é IRREVERSÍVEL e irá substituir o banco de dados atual.\n\n" +
                    $"Banco atual: {Path.GetFileName(bancoAtual)}\n" +
                    $"Novo banco: {arquivo.name}\n\n" +
                    "Certifique-se de que deseja continuar antes de prosseguir.\n\n" +
                    "Deseja realmente continuar?",
                    "Confirmação de Importação",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmacao == MessageBoxResult.Yes)
                {
                    try
                    {
                        ProgressBarMessage.Text = $"Baixando {arquivo.name}...";

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

                                ProgressBarMessage.Text = "Banco de dados importado com sucesso!";

                                // Obtém informações do backup mais recente
                                var backupDir = Path.Combine(Path.GetDirectoryName(bancoAtual), "local");
                                var backups = Directory.GetFiles(backupDir, "Database_v*_*.db")
                                                       .OrderByDescending(f => File.GetLastWriteTime(f))
                                                       .ToList();

                                if (!backups.Any())
                                {
                                    MessageBox.Show(
                                        "Erro: Nenhum backup encontrado na pasta local.",
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
                                    $"O banco de dados importado está corrompido:\n{ex.Message}\n\n" +
                                    "O banco anterior foi restaurado.",
                                    "Erro",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                $"Erro ao substituir o banco de dados:\n{ex.Message}\n\n" +
                                "Tente fechar o programa e tentar novamente.",
                                "Erro",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Erro ao importar banco de dados:\n{ex.Message}",
                            "Erro",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao importar banco de dados:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                ShowProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        // Método para importar um backup local para a aplicação
        private async void ImportarBackupButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Selecione o arquivo de banco de dados para importar",
                Filter = "Arquivo de Banco de Dados (*.db)|*.db",
                InitialDirectory = Path.GetDirectoryName(DatabaseConnect.GetDatabasePath()),
                RestoreDirectory = true
            };

            if (dialog.ShowDialog() == true)
            {
                string bancoAtual = DatabaseConnect.GetDatabasePath();
                string novoBanco = dialog.FileName;

                var confirmacao = MessageBox.Show(
                    "ATENÇÃO!\n" +
                    "Esta operação é IRREVERSÍVEL e irá substituir o banco de dados atual.\n\n" +
                    $"Banco atual: {Path.GetFileName(bancoAtual)}\n" +
                    $"Novo banco: {Path.GetFileName(novoBanco)}\n\n" +
                    "Certifique-se de que deseja continuar antes de prosseguir.\n\n" +
                    "Deseja realmente continuar?",
                    "Confirmação de Importação",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmacao == MessageBoxResult.Yes)
                {
                    try
                    {
                        ShowProgressBar.Visibility = Visibility.Visible;
                        ProgressBarMessage.Text = "Importando banco de dados...";

                        // Fecha todas as conexões com o banco atual
                        DatabaseConnect.Disconnect();

                        // Aguarda um momento para garantir que todas as conexões foram fechadas
                        await Task.Delay(1000);

                        // Faz backup do banco atual antes de substituir
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
                            File.Copy(novoBanco, bancoAtual, true);

                            // Verifica se o banco é válido
                            try
                            {
                                using (var testDb = new LiteDatabase(bancoAtual))
                                {
                                    // Se chegou aqui, o banco está íntegro
                                    testDb.Dispose();
                                }

                                ProgressBarMessage.Text = "Banco de dados importado com sucesso!";

                                // Obtém informações do backup mais recente
                                var backupDir = Path.Combine(Path.GetDirectoryName(bancoAtual), "local");
                                var backups = Directory.GetFiles(backupDir, "Database_v*_*.db")
                                    .OrderByDescending(f => File.GetLastWriteTime(f))
                                    .ToList();

                                if (!backups.Any())
                                {
                                    MessageBox.Show(
                                        "Erro: Nenhum backup encontrado na pasta local.",
                                        "Erro",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                                    return;
                                }

                                var successWindow = new ImportSuccessWindow(
                                    Path.GetFileName(backups.First()),
                                    File.GetLastWriteTime(backups.First()),
                                    novoBanco,
                                    bancoAtual);

                                successWindow.ShowDialog();

                                if (!successWindow.Confirmado)
                                {
                                    // Se o usuário cancelou, restaura o backup
                                    var backupMaisRecente = backups.First();
                                    File.Copy(backupMaisRecente, bancoAtual, true);
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
                                    $"O banco de dados importado está corrompido:\n{ex.Message}\n\n" +
                                    "O banco anterior foi restaurado.",
                                    "Erro",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                $"Erro ao substituir o banco de dados:\n{ex.Message}\n\n" +
                                "Tente fechar o programa e tentar novamente.",
                                "Erro",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Erro ao importar banco de dados:\n{ex.Message}",
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
        }

        // Método para exportar o banco de dados para o Supabase
        private async void ExportarSupabaseButton_Click(object sender, RoutedEventArgs e)
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

                    ProgressBarMessage.Text = "Arquivo enviado com sucesso!";
                    MessageBox.Show("Arquivo enviado com sucesso para o Supabase!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    ProgressBarMessage.Text = "Erro ao exportar arquivo!";
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

        // Método para exportar o banco de dados local
        private void ExportarLocalButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string bancoAtual = DatabaseConnect.GetDatabasePath();
                if (!File.Exists(bancoAtual))
                {
                    MessageBox.Show("Banco de dados atual não encontrado.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"Database_{DateTime.Now:ddMMyyyy_HHmmss}.db",
                    Filter = "Arquivos de Banco de Dados (*.db)|*.db|Todos os arquivos (*.*)|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Title = "Salvar banco de dados como"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ShowProgressBar.Visibility = Visibility.Visible;
                    ProgressBarMessage.Text = "Exportando banco de dados...";

                    try
                    {
                        // Copia o arquivo para o local escolhido
                        File.Copy(bancoAtual, saveFileDialog.FileName, true);

                        ProgressBarMessage.Text = "Banco de dados exportado com sucesso!";
                        MessageBox.Show(
                            $"Banco de dados exportado com sucesso!\n\n" +
                            $"Origem: {Path.GetFileName(bancoAtual)}\n" +
                            $"Destino: {saveFileDialog.FileName}",
                            "Sucesso",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        // Pergunta se gostaria de abrir o local do arquivo
                        var abrirLocal = MessageBox.Show(
                            "Deseja abrir o local do arquivo exportado?",
                            "Abrir Local",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                        if (abrirLocal == MessageBoxResult.Yes)
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = Path.GetDirectoryName(saveFileDialog.FileName),
                                UseShellExecute = true
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        ProgressBarMessage.Text = "Erro ao exportar banco de dados!";
                        MessageBox.Show(
                            $"Erro ao exportar banco de dados:\n{ex.Message}",
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
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao exportar banco de dados:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        // Visualizar
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


        // Informações
        // Método para verificar as informações do banco de dados
        private async Task AtualizarInformacoes()
        {
            try
            {
                ShowProgressBar.Visibility = Visibility.Visible;
                ProgressBarMessage.Text = "Atualizando informações...";

                // Banco de Dados: Local
                var bancoPath = DatabaseConnect.GetDatabasePath();
                var bancoDir = Path.GetDirectoryName(bancoPath);
                var bancoNome = Path.GetFileName(bancoPath);
                BancoDadosText.Text = $"Banco de Dados: {bancoNome}";

                // Conexão com o OneDrive: Verifica se está conectado
                var onedrivePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive");
                var onedriveStatus = Directory.Exists(onedrivePath) ? "Conectado" : "Desconectado";
                SupabaseText.Text = $"Conexão com o Supabase: {onedriveStatus}";

                // Último backup importado
                var backupDir = Path.Combine(bancoDir, "local");
                if (Directory.Exists(backupDir))
                {
                    var backups = Directory.GetFiles(backupDir, "Database_v*_*.db")
                        .OrderByDescending(f => File.GetLastWriteTime(f))
                        .ToList();

                    if (backups.Any())
                    {
                        var ultimoBackup = backups.First();
                        var dataBackup = File.GetLastWriteTime(ultimoBackup);
                        UltimoBackupImportadoText.Text = $"Último backup importado: {dataBackup:dd/MM/yyyy HH:mm}";
                    }
                    else
                    {
                        UltimoBackupImportadoText.Text = "Último backup importado: Nenhum";
                    }
                }
                else
                {
                    UltimoBackupImportadoText.Text = "Último backup importado: Nenhum";
                }

                // Último backup exportado (do Supabase)
                var ultimoExportado = await ObterUltimoBackupExportado();
                UltimoBackupExportadoText.Text = $"Último backup exportado: {ultimoExportado:dd/MM/yyyy HH:mm}";

                // Backup Atual
                var backupAtual = VerificarBackupAtual();
                BackupAtualText.Text = $"Backup Atual: {backupAtual}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar informações: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        // Método para obter o último backup exportado do Supabase
        private async Task<DateTime> ObterUltimoBackupExportado()
        {
            try
            {
                var arquivos = await SupabaseUploader.ListarArquivosAsync();
                if (arquivos != null && arquivos.Any())
                {
                    return arquivos.Max(a => a.updated_at ?? DateTime.MinValue);
                }
            }
            catch { /* Ignora erro ao obter arquivos do Supabase */ }

            return DateTime.MinValue;
        }

        // Método para verificar se o backup atual está atualizado
        private string VerificarBackupAtual()
        {
            try
            {
                var bancoPath = DatabaseConnect.GetDatabasePath();
                var bancoDir = Path.GetDirectoryName(bancoPath);
                var backupDir = Path.Combine(bancoDir, "local");

                if (!Directory.Exists(backupDir))
                    return "Não salvo";

                var backups = Directory.GetFiles(backupDir, "Database_v*_*.db")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();

                if (!backups.Any())
                    return "Não salvo";

                var ultimoBackup = backups.First();
                var dataBackup = File.GetLastWriteTime(ultimoBackup);
                var dataBanco = File.GetLastWriteTime(bancoPath);

                return dataBackup >= dataBanco ? "Salvo" : "Não salvo";
            }
            catch
            {
                return "Erro ao verificar";
            }
        }

        // Recarrega as informações do banco de dados
        private async void AtualizarButton_Click(object sender, RoutedEventArgs e)
        {
            await AtualizarInformacoes();
        }


        // Backup automático
        // Método que verifica o banco de dados mais recente para fazer o backup
        private async Task FazerBackupAutomatico()
        {
            try
            {
                string arquivoMaisRecente = EncontrarBancoMaisRecente();
                if (string.IsNullOrEmpty(arquivoMaisRecente))
                {
                    Console.WriteLine("Nenhum arquivo de banco de dados encontrado para backup.");
                    return;
                }

                string dataHoraFormatada = DateTime.Now.ToString("ddMMyyyy_HHmmss");
                string novoNome = $"Database_{dataHoraFormatada}{Path.GetExtension(arquivoMaisRecente)}";
                string caminhoTemp = Path.Combine(Path.GetTempPath(), novoNome);

                // Copia o arquivo para uma pasta temporária
                File.Copy(arquivoMaisRecente, caminhoTemp, true);

                // Faz o upload para o Supabase
                await SupabaseUploader.UploadFileAsync(caminhoTemp);
                Console.WriteLine($"Backup automático realizado com sucesso: {Path.GetFileName(arquivoMaisRecente)}");

                // Limpa o arquivo temporário
                if (File.Exists(caminhoTemp))
                {
                    File.Delete(caminhoTemp);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao realizar backup automático: {ex.Message}");
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

        
        // Método para ser chamado quando o programa for fechado
        public async Task OnProgramClosing()
        {
            await FazerBackupAutomatico();
        }

        // Atualiza as informações quando o controle é carregado
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await AtualizarInformacoes();
        }

    }

    // Supabase
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


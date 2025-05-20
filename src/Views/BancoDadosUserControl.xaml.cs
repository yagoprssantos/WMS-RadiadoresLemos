using System;
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
using WMS_RadiadoresLemos_WPF.Views;
using System.Windows.Media;
using System.Diagnostics;
using LiteDB;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WMS_RadiadoresLemos_WPF.src.Config;
using WMS_RadiadoresLemos.Services;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class BancoDadosUserControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly AzureStorageService _azureService;
        private List<object> dadosFiltrados = new List<object>();
        private bool dadosCarregados = false;
        private List<string> tabelasSelecionadas = new List<string>();
        private static readonly string[] TabelasDisponiveis = { "usuarios", "produtos", "historico", "movimentacoes" };

        private ObservableCollection<BackupFile> backupFiles;
        private BackupFile selectedFile;
        private bool isImporting;
        private string statusMessage;
        private Brush statusColor;
        private long totalSpaceUsed;
        private string formattedSpaceUsed;

        private readonly BackupService _backupService;

        public static int UploadCount = 0;
        public static int DownloadCount = 0;
        public static int DeleteCount = 0;

        public ObservableCollection<BackupFile> BackupFiles
        {
            get => backupFiles;
            set
            {
                backupFiles = value;
                OnPropertyChanged(nameof(BackupFiles));
            }
        }

        public BackupFile SelectedFile
        {
            get => selectedFile;
            set
            {
                selectedFile = value;
                OnPropertyChanged(nameof(SelectedFile));
            }
        }

        public bool IsImporting
        {
            get => isImporting;
            set
            {
                isImporting = value;
                OnPropertyChanged(nameof(IsImporting));
            }
        }

        public string StatusMessage
        {
            get => statusMessage;
            set
            {
                statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public Brush StatusColor
        {
            get => statusColor;
            set
            {
                statusColor = value;
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        public long TotalSpaceUsed
        {
            get => totalSpaceUsed;
            set
            {
                totalSpaceUsed = value;
                OnPropertyChanged(nameof(TotalSpaceUsed));
                FormattedSpaceUsed = FormatFileSize(value);
            }
        }

        public string FormattedSpaceUsed
        {
            get => formattedSpaceUsed;
            set
            {
                formattedSpaceUsed = value;
                OnPropertyChanged(nameof(FormattedSpaceUsed));
            }
        }

        public BancoDadosUserControl()
        {
            InitializeComponent();
            DataContext = this;
            _azureService = new AzureStorageService();
            SetupLinks();
            AtualizarInformacoes();
            BackupFiles = new ObservableCollection<BackupFile>();
            StatusColor = Brushes.Black;
            LoadBackupFiles();
            _backupService = new BackupService();
            CarregarContadoresAzure();
        }

        private void CarregarContadoresAzure()
        {
            try
            {
                var stats = AzureUsageStats.LoadAllStats();
                if (stats.Any())
                {
                    var ultimaStats = stats.OrderByDescending(s => s.Data).First();
                    UploadCount = ultimaStats.Uploads;
                    DownloadCount = ultimaStats.Downloads;
                    DeleteCount = ultimaStats.Deletes;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar contadores do Azure: {ex.Message}");
            }
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

            // Configura o evento do botão para importar backup local
            var importarBackupButton = FindName("ImportarBackupButton") as Button;
            if (importarBackupButton != null)
            {
                importarBackupButton.Click += ImportarBackupButton_Click;
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

        private void AbrirOneDrive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Abre o OneDrive no navegador
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://onedrive.live.com/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir o OneDrive: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    "⚠️ Atenção! Esta operação irá substituir o banco de dados atual.\n\n" +
                    $"Banco atual: {Path.GetFileName(bancoAtual)}\n" +
                    $"Novo banco: {Path.GetFileName(novoBanco)}\n\n" +
                    "Deseja continuar?",
                    "Confirmação de Importação",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmacao == MessageBoxResult.Yes)
                {
            try
            {
                ShowProgressBar.Visibility = Visibility.Visible;
                        ProgressBarMessage.Text = "🔄 Importando banco de dados...";

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
            finally
            {
                ShowProgressBar.Visibility = Visibility.Collapsed;
            }
        }
            }
        }

        private async void ExportarLocalButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string bancoAtual = DatabaseConnect.GetDatabasePath();
                if (!File.Exists(bancoAtual))
                    {
                    MessageBox.Show("❌ Banco de dados atual não encontrado.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    ProgressBarMessage.Text = "🔄 Exportando banco de dados...";

                    try
                    {
                        // Copia o arquivo para o local escolhido
                        File.Copy(bancoAtual, saveFileDialog.FileName, true);

                        ProgressBarMessage.Text = "✅ Banco de dados exportado com sucesso!";
                        MessageBox.Show(
                            $"✅ Banco de dados exportado com sucesso!\n\n" +
                            $"Origem: {Path.GetFileName(bancoAtual)}\n" +
                            $"Destino: {saveFileDialog.FileName}",
                            "Sucesso",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                        ProgressBarMessage.Text = "❌ Erro ao exportar banco de dados!";
                        MessageBox.Show(
                            $"❌ Erro ao exportar banco de dados:\n{ex.Message}",
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
                    $"❌ Erro ao exportar banco de dados:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void TestarAzureButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowProgressBar.Visibility = Visibility.Visible;
                ProgressBarMessage.Text = "🔍 Testando conexão com Azure...";

                var backupService = new BackupService();
                
                // Tenta listar os backups
                var backups = await backupService.ListarBackupsAsync();
                
                // Atualiza o status
                AzureStatusText.Text = "Status Azure: Conectado";
                AzureStatusText.Foreground = new SolidColorBrush(Colors.Green);
                
                // Mostra mensagem de sucesso com mais detalhes
                string mensagem = "✅ Conexão com Azure estabelecida com sucesso!\n\n";
                mensagem += $"Container: {AzureConfig.ContainerName}\n";
                mensagem += $"Backups encontrados: {backups.Count}\n";
                
                if (backups.Count > 0)
                {
                    mensagem += "\nÚltimos backups:\n";
                    foreach (var backup in backups.Take(5))
                    {
                        mensagem += $"- {backup}\n";
                }
                }
                
                MessageBox.Show(mensagem, "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AzureStatusText.Text = "Status Azure: Erro na conexão";
                AzureStatusText.Foreground = new SolidColorBrush(Colors.Red);
                
                string mensagemErro = $"❌ Erro ao conectar com Azure:\n\n";
                mensagemErro += $"Container: {AzureConfig.ContainerName}\n";
                mensagemErro += $"Erro: {ex.Message}\n\n";
                mensagemErro += "Verifique se:\n";
                mensagemErro += "1. A string de conexão está correta\n";
                mensagemErro += "2. A conta de armazenamento está ativa\n";
                mensagemErro += "3. Você tem permissão para acessar o container";
                
                MessageBox.Show(mensagemErro, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private async void ExportarAzureButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowProgressBar.Visibility = Visibility.Visible;
                ProgressBarMessage.Text = "Buscando backup local mais recente...";
                ProgressBar.Value = 0;

                string pastaLocal = Path.Combine(Path.GetDirectoryName(DatabaseConfig.DatabasePath), "local");
                var backups = Directory.GetFiles(pastaLocal, "Database_v*_*.db")
                    .Where(f => Path.GetFileName(f).StartsWith("Database_v") && !Path.GetFileName(f).Contains("-log"))
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();

                if (!backups.Any())
                {
                    MessageBox.Show("Nenhum backup local encontrado para exportar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                    ShowProgressBar.Visibility = Visibility.Collapsed;
                    return;
                }

                string arquivoBackup = backups.First();
                Console.WriteLine($"Arquivo enviado para o Azure: {arquivoBackup}");

                ProgressBarMessage.Text = "Exportando backup para o Azure...";
                ProgressBar.Value = 50;

                await _backupService.CriarBackupAsync(arquivoBackup);

                ProgressBar.Value = 100;
                ProgressBarMessage.Text = "Backup exportado com sucesso!";
                await Task.Delay(1000);

                ShowProgressBar.Visibility = Visibility.Collapsed;

                MessageBox.Show("Backup exportado com sucesso para o Azure!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao exportar backup: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                ShowProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private async void ListarAzure_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowProgressBar.Visibility = Visibility.Visible;
                ProgressBarMessage.Text = "🔍 Listando arquivos no Azure...";

                var backupService = new BackupService();
                var backups = await backupService.ListarBackupsAsync();

                if (backups.Count > 0)
                {
                    string nomes = string.Join("\n", backups.Select(a => $"📄 {a}"));
                    MessageBox.Show($"Arquivos encontrados:\n\n{nomes}", "Arquivos no Azure", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Nenhum arquivo encontrado no Azure.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao conectar com Azure:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private async void ImportarBackupAzureButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowProgressBar.Visibility = Visibility.Visible;
                ProgressBarMessage.Text = "🔍 Listando arquivos no Azure...";

                var backupService = new BackupService();
                var arquivosAzure = await backupService.ListarBackupsAsync();

                if (arquivosAzure.Count == 0)
                {
                    MessageBox.Show("Nenhum arquivo de banco de dados encontrado no Azure.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Abre a janela para o usuário escolher o arquivo
                var pickerWindow = new AzureFilePickerWindow(arquivosAzure);
                var resultado = pickerWindow.ShowDialog();

                if (resultado != true || pickerWindow.ArquivoSelecionado == null)
                    return;

                var arquivo = pickerWindow.ArquivoSelecionado;
                string bancoAtual = DatabaseConnect.GetDatabasePath();

                var confirmacao = MessageBox.Show(
                    "⚠️ Atenção! Esta operação irá substituir o banco de dados atual.\n\n" +
                    $"Banco atual: {Path.GetFileName(bancoAtual)}\n" +
                    $"Novo banco: {arquivo}\n\n" +
                    "Deseja continuar?",
                    "Confirmação de Importação",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmacao == MessageBoxResult.Yes)
                {
                    try
                    {
                        ProgressBarMessage.Text = $"⬇️ Baixando {arquivo}...";

                        // Cria um arquivo temporário para o download
                        string caminhoTemp = Path.Combine(Path.GetTempPath(), "Database.db");
                        await backupService.RestaurarBackupAsync(arquivo, caminhoTemp);

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
                                var backupsLocais = Directory.GetFiles(backupDir, "Database_v*_*.db")
                                    .OrderByDescending(f => File.GetLastWriteTime(f))
                                    .ToList();

                                if (!backupsLocais.Any())
                                {
                                    MessageBox.Show(
                                        "❌ Erro: Nenhum backup encontrado na pasta local.",
                                        "Erro",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                                    return;
                                }

                                var successWindow = new ImportSuccessWindow(
                                    Path.GetFileName(backupsLocais.First()),
                                    File.GetLastWriteTime(backupsLocais.First()),
                                    caminhoTemp,
                                    bancoAtual);

                                successWindow.ShowDialog();

                                if (!successWindow.Confirmado)
                                {
                                    // Se o usuário cancelou, restaura o backup
                                    var backupMaisRecente = backupsLocais.First();
                                    File.Copy(backupMaisRecente, bancoAtual, true);
                                    return;
                                }
                            }
                            catch (Exception ex)
                            {
                                // Se o banco estiver corrompido, restaura o backup mais recente
                                var backupDir = Path.Combine(Path.GetDirectoryName(bancoAtual), "local");
                                var backupsLocais = Directory.GetFiles(backupDir, "Database_v*_*.db")
                                    .OrderByDescending(f => File.GetLastWriteTime(f))
                                    .ToList();

                                if (backupsLocais.Any())
                                {
                                    File.Copy(backupsLocais.First(), bancoAtual, true);
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

        private void AbrirAzureNoNavegador_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://portal.azure.com",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir o navegador: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string EncontrarBancoMaisRecente()
        {
            string diretorioBanco = Path.GetDirectoryName(DatabaseConnect.GetDatabasePath());
            if (string.IsNullOrEmpty(diretorioBanco) || !Directory.Exists(diretorioBanco))
            {
                return null;
            }

            // Procura especificamente pelo arquivo Database.db
            string databasePath = Path.Combine(diretorioBanco, "Database.db");
            if (File.Exists(databasePath))
            {
                return databasePath;
            }

            // Se não encontrar o Database.db, procura por outros arquivos .db
            var arquivos = Directory.GetFiles(diretorioBanco, "*.db")
                .Where(f => !f.EndsWith("-log.db")) // Exclui arquivos de log
                .ToList();

            if (arquivos.Count == 0)
            {
                return null;
            }

            return arquivos.OrderByDescending(f => File.GetLastWriteTime(f)).First();
        }

        private async Task VerificarStatusBackupAzure()
        {
            try
            {
                var backupService = new BackupService();
                var backups = await backupService.ListarBackupsAsync();
                
                if (backups.Count > 0)
                {
                    var ultimoBackup = backups.OrderByDescending(b => b).First();
                    var dataBackup = DateTime.ParseExact(
                        ultimoBackup.Replace("DatabaseBackup_", "").Replace(".db", ""),
                        "yyyyMMdd_HHmmss",
                        null
                    );
                    
                    UltimoBackupExportadoText.Text = $"Último backup exportado: {dataBackup:dd/MM/yyyy HH:mm}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao verificar status do backup: {ex.Message}");
            }
        }

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

                // Conexão com o Azure
                await VerificarStatusBackupAzure();

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

        // Atualiza as informações quando o controle é carregado
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await AtualizarInformacoes();
        }

        // Atualiza as informações quando o botão de atualizar é clicado
        private async void AtualizarButton_Click(object sender, RoutedEventArgs e)
        {
            await AtualizarInformacoes();
        }

        private async void LoadBackupFiles()
        {
            try
            {
                StatusMessage = "Carregando arquivos...";
                StatusColor = Brushes.Black;

                var files = await _azureService.ListBackupFilesAsync();
                BackupFiles.Clear();
                foreach (var file in files)
                {
                    BackupFiles.Add(file);
                }

                // Calcula o espaço total usado
                await CalculateTotalSpaceUsed();

                StatusMessage = "Arquivos carregados com sucesso!";
                StatusColor = Brushes.Green;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro ao carregar arquivos: {ex.Message}";
                StatusColor = Brushes.Red;
            }
        }

        private async Task CalculateTotalSpaceUsed()
        {
            try
            {
                long totalSize = 0;
                var files = await _azureService.ListBackupFilesAsync();
                
                foreach (var file in files)
                {
                    var size = ParseFileSize(file.Size);
                    totalSize += size;
                }

                TotalSpaceUsed = totalSize;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro ao calcular espaço usado: {ex.Message}";
                StatusColor = Brushes.Red;
            }
        }

        private long ParseFileSize(string size)
        {
            var parts = size.Split(' ');
            if (parts.Length != 2) return 0;

            double value = double.Parse(parts[0]);
            string unit = parts[1].ToUpper();

            switch (unit)
            {
                case "B": return (long)value;
                case "KB": return (long)(value * 1024);
                case "MB": return (long)(value * 1024 * 1024);
                case "GB": return (long)(value * 1024 * 1024 * 1024);
                case "TB": return (long)(value * 1024 * 1024 * 1024 * 1024);
                default: return 0;
            }
        }

        private void ShowAzureStorageInfo_Click(object sender, RoutedEventArgs e)
        {
            var infoWindow = new AzureStorageInfoWindow();
            infoWindow.ShowDialog();
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }

        private async void ImportarArquivosAzure_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowProgressBar.Visibility = Visibility.Visible;
                ProgressBarMessage.Text = "🔍 Listando arquivos no Azure...";

                var backupService = new BackupService();
                var arquivosAzure = await backupService.ListarBackupsAsync();

                if (arquivosAzure.Count == 0)
                {
                    MessageBox.Show("Nenhum arquivo encontrado no Azure.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Abre a janela para o usuário escolher o arquivo
                var pickerWindow = new AzureFilePickerWindow(arquivosAzure);
                var resultado = pickerWindow.ShowDialog();

                if (resultado != true || pickerWindow.ArquivoSelecionado == null)
                    return;

                var arquivo = pickerWindow.ArquivoSelecionado;

                // Abre o diálogo para escolher onde salvar o arquivo
                var saveFileDialog = new SaveFileDialog
                {
                    FileName = arquivo,
                    Filter = "Arquivos de Banco de Dados (*.db)|*.db|Todos os arquivos (*.*)|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Title = "Salvar arquivo como"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ProgressBarMessage.Text = $"⬇️ Baixando {arquivo}...";

                    try
                    {
                        // Cria um arquivo temporário para o download
                        string caminhoTemp = Path.Combine(Path.GetTempPath(), arquivo);
                        await backupService.RestaurarBackupAsync(arquivo, caminhoTemp);

                        // Copia o arquivo para o local escolhido
                        File.Copy(caminhoTemp, saveFileDialog.FileName, true);

                        // Deleta o arquivo temporário
                        if (File.Exists(caminhoTemp))
                        {
                            File.Delete(caminhoTemp);
                        }

                        ProgressBarMessage.Text = "✅ Arquivo importado com sucesso!";
                        MessageBox.Show(
                            $"✅ Arquivo importado com sucesso!\n\n" +
                            $"Arquivo: {arquivo}\n" +
                            $"Destino: {saveFileDialog.FileName}",
                            "Sucesso",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        // Abre o explorador de arquivos na pasta onde o arquivo foi salvo
                        Process.Start("explorer.exe", $"/select,\"{saveFileDialog.FileName}\"");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"❌ Erro ao importar arquivo:\n{ex.Message}",
                            "Erro",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Erro ao importar arquivo:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                ShowProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        public async Task FazerBackupAutomaticoParaAzure()
        {
            try
            {
                string pastaLocal = Path.Combine(Path.GetDirectoryName(DatabaseConfig.DatabasePath), "local");
                var backups = Directory.GetFiles(pastaLocal, "Database_v*_*.db")
                    .Where(f => Path.GetFileName(f).StartsWith("Database_v") && !Path.GetFileName(f).Contains("-log"))
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();

                if (!backups.Any())
                {
                    Console.WriteLine("Nenhum backup local encontrado para exportar automaticamente.");
                    return;
                }

                string arquivoBackup = backups.First();
                Console.WriteLine($"[AUTO] Arquivo enviado para o Azure: {arquivoBackup}");

                await _backupService.CriarBackupAsync(arquivoBackup);

                Console.WriteLine("[AUTO] Backup exportado com sucesso para o Azure!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUTO] Erro ao exportar backup automático: {ex.Message}");
            }
        }

        private void BtnOperacoesAzure_Click(object sender, RoutedEventArgs e)
        {
            // Soma o total de operações em todas as sessões
            int totalUploads = 0;
            int totalDownloads = 0;
            int totalDeletes = 0;
            try
            {
                var stats = AzureUsageStats.LoadAllStats();
                if (stats.Any())
                {
                    totalUploads = stats.Sum(s => s.Uploads);
                    totalDownloads = stats.Sum(s => s.Downloads);
                    totalDeletes = stats.Sum(s => s.Deletes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao somar operações Azure: {ex.Message}");
            }

            MessageBox.Show($"Operações Azure acumuladas:\n\nUploads: {totalUploads}\nDownloads: {totalDownloads}\nExclusões: {totalDeletes}",
                "Operações Azure", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnMetricasAzure_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowProgressBar.Visibility = Visibility.Visible;
                ProgressBarMessage.Text = "Obtendo métricas do banco de dados...";

                var config = ConfigManager.LoadConfig();
                var metricsService = new AzureMetricsService(
                    config.AzureSubscriptionId,
                    config.AzureResourceId,
                    config.AzureWorkspaceId,
                    config.AzureApiKey
                );

                var metrics = await metricsService.GetDatabaseMetricsAsync();

                var resultWindow = new Window
                {
                    Title = "Métricas do Banco de Dados",
                    Width = 600,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = (Brush)FindResource("BackgroundBrush")
                };

                var textBox = new TextBox
                {
                    Text = metrics,
                    IsReadOnly = true,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Margin = new Thickness(10),
                    Background = (Brush)FindResource("PanelBackgroundBrush"),
                    Foreground = (Brush)FindResource("TextBrush"),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12
                };

                resultWindow.Content = textBox;
                resultWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao obter métricas: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowProgressBar.Visibility = Visibility.Collapsed;
            }
        }
    }
}


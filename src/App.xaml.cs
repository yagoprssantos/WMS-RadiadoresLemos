using System;
using System.IO;
using System.Windows;
using WMS_RadiadoresLemos_WPF.src.Services;
using WMS_RadiadoresLemos_WPF.src.Views;
using System.Linq;
using System.Threading.Tasks;
using WMS_RadiadoresLemos_WPF.src.Config;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class App : Application
    {
        private const string ThemeFilePath = "theme.txt";
        private const string DefaultTheme = "LightTheme";
        private TaskCompletionSource<bool> _exitTaskSource = new TaskCompletionSource<bool>();

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Inicializa a configuração do Azure Storage primeiro
                AzureConfig.Initialize("https://boletwash.blob.core.windows.net/wms-backups?sp=racwdli&st=2025-05-15T00:37:27Z&se=2028-05-15T08:37:27Z&spr=https&sv=2024-11-04&sr=c&sig=eMvfkgQqNCIFbk2MF0eV66x28gKXXQr3BVpQhhqMpbs%3D");
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao inicializar configurações do Azure:\n{ex.Message}\n\n" +
                    "Por favor, verifique se a string de conexão está correta.",
                    "Erro de Configuração",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return; // Encerra a aplicação se não conseguir inicializar o Azure
            }

            LoadTheme();
            
            try
            {
                Console.WriteLine("🔄 Iniciando backup automático ao abrir o programa...");

                // Fazer backup automático ao abrir
                string pastaLocal = Path.Combine(Path.GetDirectoryName(DatabaseConnect.GetDatabasePath()), "local");
                var backups = Directory.GetFiles(pastaLocal, "Database_v*_*.db")
                    .Where(f => Path.GetFileName(f).StartsWith("Database_v") && !Path.GetFileName(f).Contains("-log"))
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();

                if (!backups.Any())
                {
                    Console.WriteLine("Nenhum backup local encontrado para exportar automaticamente.");
                }
                else
                {
                    string arquivoBackup = backups.First();
                    string nomeAzure = $"DatabaseBackup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                    string tempFile = Path.Combine(Path.GetTempPath(), nomeAzure);
                    File.Copy(arquivoBackup, tempFile, true);

                    Console.WriteLine($"📦 Backup automático enviado para o Azure: {tempFile}");
                    var backupService = new BackupService();
                    var backupFileName = await backupService.CriarBackupAsync(tempFile);
                    Console.WriteLine($"✅ Backup enviado para Azure: {backupFileName}");

                    // Apaga o arquivo temporário após o upload
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);

                    // Verifica se o backup foi realmente enviado
                    Console.WriteLine("🔍 Verificando backup no Azure...");
                    var backupsAzure = await backupService.ListarBackupsAsync();
                    if (backupsAzure.Contains(backupFileName))
                    {
                        Console.WriteLine("✅ Backup confirmado no Azure");
                    }
                    else
                    {
                        throw new Exception("O backup foi criado localmente, mas falhou ao ser enviado para o Azure");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao fazer backup automático: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Detalhes do erro: {ex.InnerException.Message}");
                }
            }

            // Adiciona o usuário administrador antes de qualquer outra operação
            AddAdminUser.AddAdmin();
            
            base.OnStartup(e);
        }

        // Metodo para quando aplicação for fechada
        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                Console.WriteLine("🔄 Iniciando processo de fechamento...");
            DatabaseConnect.Disconnect();
                Console.WriteLine("✅ Banco de dados desconectado");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao desconectar banco de dados: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("✅ Processos finalizados, encerrando programa...");
            base.OnExit(e);
            }
        }

        // Método para aguardar o fechamento completo
        public async Task WaitForExitAsync()
        {
            await _exitTaskSource.Task;
        }

        private void LoadTheme()
        {
            string themeName = DefaultTheme;
            string themePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ThemeFilePath);

            if (File.Exists(themePath))
            {
                themeName = File.ReadAllText(themePath).Trim();
            }

            ApplyTheme(themeName);
        }

        public static void ApplyTheme(string themeName)
        {
            var dict = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/src/Resources/Themes/{themeName}.xaml")
            };

            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }
    }
}

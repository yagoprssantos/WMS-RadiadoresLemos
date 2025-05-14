using System;
using System.IO;
using System.Windows;
using WMS_RadiadoresLemos_WPF.src.Services;
using WMS_RadiadoresLemos_WPF.src.Views;
using System.Linq;
using System.Threading.Tasks;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class App : Application
    {
        private const string ThemeFilePath = "theme.txt";
        private const string DefaultTheme = "LightTheme";
        private TaskCompletionSource<bool> _exitTaskSource = new TaskCompletionSource<bool>();

        protected override async void OnStartup(StartupEventArgs e)
        {
            LoadTheme();
            
            try
            {
                Console.WriteLine("🔄 Iniciando backup automático ao abrir o programa...");
                
                // Fazer backup automático ao abrir
                string diretorioBanco = Path.GetDirectoryName(DatabaseConnect.GetDatabasePath());
                if (!string.IsNullOrEmpty(diretorioBanco) && Directory.Exists(diretorioBanco))
                {
                    var arquivos = Directory.GetFiles(diretorioBanco, "*.db");
                    if (arquivos.Length > 0)
                    {
                        string arquivoMaisRecente = arquivos.OrderByDescending(f => File.GetLastWriteTime(f)).First();
                        Console.WriteLine($"📦 Encontrado banco de dados: {Path.GetFileName(arquivoMaisRecente)}");

                        // Desconecta do banco de dados antes de fazer o backup
                        Console.WriteLine("🔌 Desconectando do banco de dados...");
                        DatabaseConnect.Disconnect();
                        Console.WriteLine("✅ Banco de dados desconectado");

                        // Aguarda um momento para garantir que todas as conexões sejam fechadas
                        Console.WriteLine("⏳ Aguardando conexões serem fechadas...");
                        await Task.Delay(2000);

                        // Cria o backup local
                        Console.WriteLine("📦 Criando backup local...");
                        DatabaseBackup.CreateBackup(arquivoMaisRecente);
                        Console.WriteLine("✅ Backup local criado com sucesso");

                        // Faz o upload para o Azure
                        Console.WriteLine("☁️ Enviando para Azure...");
                        var backupService = new BackupService();
                        var backupFileName = await backupService.CriarBackupAsync(arquivoMaisRecente);
                        Console.WriteLine($"✅ Backup enviado para Azure: {backupFileName}");

                        // Verifica se o backup foi realmente enviado
                        Console.WriteLine("🔍 Verificando backup no Azure...");
                        var backups = await backupService.ListarBackupsAsync();
                        if (backups.Contains(backupFileName))
                        {
                            Console.WriteLine("✅ Backup confirmado no Azure");
                        }
                        else
                        {
                            throw new Exception("O backup foi criado localmente, mas falhou ao ser enviado para o Azure");
                        }
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

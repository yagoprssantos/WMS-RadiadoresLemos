using System;
using System.IO;
using System.Windows;
using WMS_RadiadoresLemos_WPF.src.Services;
using WMS_RadiadoresLemos_WPF.src.Views;
using System.Linq;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class App : Application
    {
        private const string ThemeFilePath = "theme.txt";
        private const string DefaultTheme = "LightTheme";

        // Metodo para quando a aplicação for iniciada
        protected override async void OnStartup(StartupEventArgs e)
        {            
            LoadTheme();

            // Adiciona o usuário administrador antes de qualquer outra operação
            AddAdminUser.AddAdmin();
            
            base.OnStartup(e);
        }

        // Metodo para quando aplicação for fechada
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                Console.WriteLine("Iniciando processo de backup ao fechar...");

                string diretorioBanco = Path.GetDirectoryName(DatabaseConnect.GetDatabasePath());
                if (!string.IsNullOrEmpty(diretorioBanco) && Directory.Exists(diretorioBanco))
                {
                    var arquivos = Directory.GetFiles(diretorioBanco, "*.db");
                    if (arquivos.Length > 0)
                    {
                        string arquivoMaisRecente = arquivos.OrderByDescending(f => File.GetLastWriteTime(f)).First();
                        string dataHoraFormatada = DateTime.Now.ToString("ddMMyyyy_HHmmss");
                        string novoNome = $"Database_{dataHoraFormatada}{Path.GetExtension(arquivoMaisRecente)}";
                        string caminhoTemp = Path.Combine(Path.GetTempPath(), novoNome);

                        File.Copy(arquivoMaisRecente, caminhoTemp, true);
                        Console.WriteLine($"Arquivo copiado para o caminho temporário: {caminhoTemp}");

                        // Executa o upload de forma síncrona usando Task.Run
                        Task.Run(async () => await SupabaseUploader.UploadFileAsync(caminhoTemp)).GetAwaiter().GetResult();

                        Console.WriteLine($"✅ Backup realizado com sucesso ao fechar: {Path.GetFileName(arquivoMaisRecente)}");

                        // Limpa o arquivo temporário
                        if (File.Exists(caminhoTemp))
                        {
                            File.Delete(caminhoTemp);
                            Console.WriteLine("Arquivo temporário excluído.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Nenhum arquivo .db encontrado no diretório.");
                    }
                }
                else
                {
                    Console.WriteLine("Diretório do banco de dados não encontrado ou inválido.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao fazer backup ao fechar: {ex.Message}");
            }

            // Desconecta do banco de dados
            DatabaseConnect.Disconnect();
            base.OnExit(e);
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

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

        protected override async void OnStartup(StartupEventArgs e)
        {
            
            LoadTheme();
            
            try
            {
                // Fazer backup automático ao abrir
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao fazer backup automático: {ex.Message}");
            }

            // Adiciona o usuário administrador antes de qualquer outra operação
            AddAdminUser.AddAdmin();
            
            base.OnStartup(e);
        }

        // Metodo para quando aplicação for fechada
        protected override void OnExit(ExitEventArgs e)
        {
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
                /* COMO FUNCIONA AS LINHAS ABAIXO
                 * 
                 * Apenas uma das linhas abaixo deve ser usada
                 * 
                 * Caso precise alterar o Style.xaml, use a segunda linha, assim você poderá
                 * ver as mudanças apenas alterando o Style.xaml, o que facilita o desenvolvimento.
                 * 
                 * Caso tenha terminado de desenvolver, tenha certeza que os outros temas em
                 * src/Resources/Themes estão prontos e funcionando e iguais ao novo Styel.xaml.
                 * 
                 * APENAS USE A PRIMEIRA LINHA SE O ARQUIVO XAML DO TEMA ESTIVER PRONTO.
                 */

                // A LINHA DEBAIXO É A LINHA QUE CARREGA O ARQUIVO XAML DO TEMA CORRETAMENTE
                Source = new Uri($"src/Resources/Themes/{themeName}.xaml", UriKind.Relative)

                // A LINHA DEBAIXO SERVE PARA USAR O Style.xaml PADRÃO - PARA DESENVOLVIMENTO
                //Source = new Uri("src/Resources/Style.xaml", UriKind.Relative)
            };

            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }
    }
}

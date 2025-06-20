using System;
using System.IO;
using System.Windows;
using WMS_RadiadoresLemos_WPF.src.Services;
using WMS_RadiadoresLemos_WPF.src.Views;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

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

                // Verificar e limpar arquivos antigos do Supabase ao abrir o aplicativo
                try
                {
                    var arquivosSupabase = await SupabaseUploader.ListarArquivosAsync();
                    await LimparArquivosAntigosSeNecessario(arquivosSupabase);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao verificar arquivos do Supabase: {ex.Message}");
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

        private async Task LimparArquivosAntigosSeNecessario(List<SupabaseArquivo> arquivos)
        {
            const long LIMITE_0_95GB = 1020054732; // 0.95 GB = 0.95 * 1024^3
            const int LIMITE_99_ARQUIVOS = 99;
            if (arquivos == null || arquivos.Count == 0)
                return;

            var arquivosOrdenados = arquivos.OrderBy(a => a.created_at ?? DateTime.MinValue).ToList();
            long espacoTotal = arquivos.Sum(a => a.size);
            int totalArquivos = arquivos.Count;
            bool precisaLimpar = espacoTotal > LIMITE_0_95GB || totalArquivos > LIMITE_99_ARQUIVOS;

            int deletados = 0;
            while ((espacoTotal > LIMITE_0_95GB || totalArquivos > LIMITE_99_ARQUIVOS) && arquivosOrdenados.Count > 0)
            {
                var arquivoMaisAntigo = arquivosOrdenados.First();
                await SupabaseUploader.DeletarArquivoAsync(arquivoMaisAntigo.fullPath);
                arquivosOrdenados.RemoveAt(0);
                espacoTotal -= arquivoMaisAntigo.size;
                totalArquivos--;
                deletados++;
            }
            if (deletados > 0)
            {
                Console.WriteLine($"{deletados} arquivo(s) antigo(s) foram removidos automaticamente para manter o limite de espaço ou quantidade.");
            }
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

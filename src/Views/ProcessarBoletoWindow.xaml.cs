using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text.RegularExpressions;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class ProcessarBoletoWindow : Window
    {
        private string arquivoPdfSelecionado;

        public ProcessarBoletoWindow()
        {
            InitializeComponent();
            ProcessarButton.IsEnabled = false;
        }

        private void SelecionarPdfButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Selecione o arquivo PDF do boleto",
                Filter = "Arquivos PDF (*.pdf)|*.pdf",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog() == true)
            {
                arquivoPdfSelecionado = dialog.FileName;
                ArquivoSelecionadoText.Text = Path.GetFileName(arquivoPdfSelecionado);
                ProcessarButton.IsEnabled = true;
                LogTextBox.Clear();
            }
        }

        private async void ProcessarButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(arquivoPdfSelecionado))
            {
                MessageBox.Show("Por favor, selecione um arquivo PDF primeiro.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ProcessarButton.IsEnabled = false;
                LogTextBox.AppendText("Iniciando processamento do PDF...\n");

                // Extrair texto do PDF
                string textoPdf = ExtrairTextoDoPdf(arquivoPdfSelecionado);
                LogTextBox.AppendText("Texto extraído do PDF com sucesso.\n");

                // Procurar por XML no texto
                string xml = ExtrairXmlDoTexto(textoPdf);
                if (string.IsNullOrEmpty(xml))
                {
                    LogTextBox.AppendText("Nenhum XML encontrado no PDF.\n");
                    return;
                }

                LogTextBox.AppendText("XML encontrado e extraído com sucesso.\n");

                // Salvar XML em um arquivo
                string diretorioSaida = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "BoletosProcessados"
                );
                Directory.CreateDirectory(diretorioSaida);

                string nomeArquivo = Path.GetFileNameWithoutExtension(arquivoPdfSelecionado);
                string caminhoXml = Path.Combine(diretorioSaida, $"{nomeArquivo}.xml");

                await File.WriteAllTextAsync(caminhoXml, xml);
                LogTextBox.AppendText($"XML salvo em: {caminhoXml}\n");

                MessageBox.Show(
                    "Processamento concluído com sucesso!\n" +
                    $"O XML foi salvo em: {caminhoXml}",
                    "Sucesso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                LogTextBox.AppendText($"Erro durante o processamento: {ex.Message}\n");
                MessageBox.Show(
                    $"Erro durante o processamento:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                ProcessarButton.IsEnabled = true;
            }
        }

        private string ExtrairTextoDoPdf(string caminhoPdf)
        {
            using var pdfReader = new PdfReader(caminhoPdf);
            using var pdfDocument = new PdfDocument(pdfReader);
            var strategy = new LocationTextExtractionStrategy();
            var texto = "";

            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                var page = pdfDocument.GetPage(i);
                texto += PdfTextExtractor.GetTextFromPage(page, strategy);
            }

            return texto;
        }

        private string ExtrairXmlDoTexto(string texto)
        {
            // Procura por padrões comuns de XML em boletos
            var padraoXml = @"(&lt;NFe[^>]*>.*?&lt;/NFe>)|(&lt;nfe[^>]*>.*?&lt;/nfe>)";
            var match = Regex.Match(texto, padraoXml, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (match.Success)
            {
                return match.Value;
            }

            return null;
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Wpf;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class BoletoExtractionWindow : Window
    {
        private BoletoData? _dadosExtraidos;
        private bool _dadosCarregados = false;
        private string _arquivoBoleto = string.Empty;
        private bool _webViewPronto = false;
        private bool _processoAutomaticoAgendado = false;

        public BoletoData? BoletoSalvo { get; private set; }

        public BoletoExtractionWindow()
        {
            InitializeComponent();
            Loaded += BoletoExtractionWindow_Loaded;
        }

        public BoletoExtractionWindow(string caminhoArquivo)
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(caminhoArquivo) && File.Exists(caminhoArquivo))
            {
                _arquivoBoleto = caminhoArquivo;
                _processoAutomaticoAgendado = true;
                Console.WriteLine($"BoletoExtractionWindow inicializada com arquivo: {caminhoArquivo}");
            }

            Loaded += BoletoExtractionWindow_Loaded;
        }

        private async void BoletoExtractionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusTextBlock.Text = "Inicializando...";
                await InicializarWebView();

                // Verifica se há um arquivo para processar automaticamente
                if (_processoAutomaticoAgendado && !string.IsNullOrEmpty(_arquivoBoleto) && File.Exists(_arquivoBoleto))
                {
                    StatusTextBlock.Text = $"Processando arquivo: {Path.GetFileName(_arquivoBoleto)}";
                    await ProcessarArquivoAutomaticamente(_arquivoBoleto);
                }
                else
                {
                    // Solicitar o arquivo do boleto se nenhum foi fornecido
                    SelecionarArquivoDoBoletoPrimeiro();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar a janela: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = $"Erro ao inicializar: {ex.Message}";
            }
        }

        private async Task ProcessarArquivoAutomaticamente(string caminhoArquivo)
        {
            try
            {
                // Espera até que o WebView esteja totalmente inicializado
                if (!_webViewPronto)
                {
                    StatusTextBlock.Text = "Aguardando inicialização do WebView...";
                    await EsperarWebViewPronto();
                }

                if (WebViewExtractor.CoreWebView2 == null)
                {
                    StatusTextBlock.Text = "WebView não inicializado corretamente. Tente novamente.";
                    return;
                }

                // Verificar se o arquivo existe
                if (!File.Exists(caminhoArquivo))
                {
                    StatusTextBlock.Text = $"Arquivo não encontrado: {caminhoArquivo}";
                    MessageBox.Show($"O arquivo não foi encontrado: {caminhoArquivo}",
                        "Arquivo não encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Lê o arquivo como base64
                byte[] fileBytes = await File.ReadAllBytesAsync(caminhoArquivo);
                string base64Data = Convert.ToBase64String(fileBytes);
                string mimeType = "application/octet-stream";

                // Determina o tipo MIME com base na extensão
                string extensao = Path.GetExtension(caminhoArquivo).ToLower();
                if (extensao == ".pdf") mimeType = "application/pdf";
                else if (extensao == ".png") mimeType = "image/png";
                else if (extensao == ".jpg" || extensao == ".jpeg") mimeType = "image/jpeg";
                else
                {
                    StatusTextBlock.Text = $"Tipo de arquivo não suportado: {extensao}";
                    MessageBox.Show($"O tipo de arquivo {extensao} não é suportado. Use PDF, PNG ou JPG.",
                        "Tipo de arquivo não suportado", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Formato completo de base64 com prefixo de dados
                string base64Content = $"data:{mimeType};base64,{base64Data}";
                string fileName = Path.GetFileName(caminhoArquivo);

                // Envia o comando para o WebView processar o arquivo
                var command = new
                {
                    action = "processarArquivo",
                    fileData = base64Content,
                    fileName = fileName
                };

                string commandJson = JsonSerializer.Serialize(command);
                Console.WriteLine($"Enviando comando para WebView: {fileName} ({mimeType})");

                // Atualiza a interface antes de enviar o comando
                StatusTextBlock.Text = $"Enviando arquivo para processamento: {fileName}";
                await Task.Delay(500); // Pequena pausa para garantir que a UI seja atualizada

                // Envia o comando para o WebView
                WebViewExtractor.CoreWebView2.PostWebMessageAsString(commandJson);

                // Guarda o caminho do arquivo para uso posterior
                _arquivoBoleto = caminhoArquivo;
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Erro ao processar arquivo: {ex.Message}";
                MessageBox.Show($"Erro ao processar arquivo: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task EsperarWebViewPronto(int timeoutMs = 10000)
        {
            int elapsedTime = 0;
            int checkInterval = 100;

            while (!_webViewPronto && elapsedTime < timeoutMs)
            {
                await Task.Delay(checkInterval);
                elapsedTime += checkInterval;
            }

            if (!_webViewPronto)
            {
                throw new TimeoutException("Tempo limite excedido aguardando a inicialização do WebView.");
            }
        }

        private void SelecionarArquivoDoBoletoPrimeiro()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Selecione o arquivo do boleto para extrair dados",
                    Filter = "Arquivos PDF (*.pdf)|*.pdf|" +
                             "Imagens (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|" +
                             "Todos os arquivos (*.*)|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    RestoreDirectory = true
                };

                if (dialog.ShowDialog() == true)
                {
                    _arquivoBoleto = dialog.FileName;
                    StatusTextBlock.Text = $"Arquivo selecionado: {Path.GetFileName(_arquivoBoleto)}";

                    // Se já existirem dados extraídos, atualizar o caminho
                    if (_dadosExtraidos != null)
                    {
                        _dadosExtraidos.CaminhoArquivo = _arquivoBoleto;
                    }

                    // Processar o arquivo selecionado automaticamente
                    if (_webViewPronto)
                    {
                        _ = ProcessarArquivoAutomaticamente(_arquivoBoleto);
                    }
                    else
                    {
                        _processoAutomaticoAgendado = true;
                    }
                }
                else
                {
                    // Se o usuário cancelar a seleção, fechar a janela
                    DialogResult = false;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao selecionar arquivo: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task InicializarWebView()
        {
            try
            {
                StatusTextBlock.Text = "Inicializando WebView...";
                await WebViewExtractor.EnsureCoreWebView2Async(null);
                WebViewExtractor.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                // Adiciona um handler para navegação completada
                WebViewExtractor.CoreWebView2.NavigationCompleted += (s, e) =>
                {
                    _webViewPronto = true;
                    StatusTextBlock.Text = "WebView inicializado e pronto.";

                    // Se houver um arquivo agendado para processamento, processa-o agora
                    if (_processoAutomaticoAgendado && !string.IsNullOrEmpty(_arquivoBoleto) && File.Exists(_arquivoBoleto))
                    {
                        _ = ProcessarArquivoAutomaticamente(_arquivoBoleto);
                    }
                };

                await CarregarArquivoHtml();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao inicializar WebView: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = $"Erro: {ex.Message}";
            }
        }

        private async Task CarregarArquivoHtml()
        {
            try
            {
                string[] possiveisCaminhos = {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "src", "Resources", "BoletoExtractor.html"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BoletoExtractor.html"),
                    Path.Combine(Directory.GetCurrentDirectory(), "src", "Resources", "BoletoExtractor.html"),
                    Path.Combine(Directory.GetCurrentDirectory(), "BoletoExtractor.html")
                };

                string arquivoEncontrado = null;
                foreach (string caminho in possiveisCaminhos)
                {
                    if (File.Exists(caminho))
                    {
                        arquivoEncontrado = caminho;
                        break;
                    }
                }

                if (arquivoEncontrado != null)
                {
                    StatusTextBlock.Text = "Carregando BoletoExtractor.html...";
                    string fullPath = Path.GetFullPath(arquivoEncontrado);
                    string fileUrl = $"file:///{fullPath.Replace("\\", "/")}";

                    WebViewExtractor.CoreWebView2.Navigate(fileUrl);
                    StatusTextBlock.Text = $"Arquivo carregado: {Path.GetFileName(arquivoEncontrado)}";
                }
                else
                {
                    throw new FileNotFoundException("Arquivo BoletoExtractor.html não encontrado!");
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Erro ao carregar arquivo: {ex.Message}";
                MessageBox.Show($"Erro: {ex.Message}\n\nVerifique se o arquivo BoletoExtractor.html está na pasta correta.",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string message = e.TryGetWebMessageAsString();
                if (string.IsNullOrEmpty(message)) return;

                var dadosJson = JsonSerializer.Deserialize<JsonElement>(message);

                if (dadosJson.TryGetProperty("type", out var type))
                {
                    string tipoMsg = type.GetString() ?? "";

                    if (tipoMsg == "dadosExtraidos")
                    {
                        ProcessarDadosExtraidos(dadosJson);
                    }
                    else if (tipoMsg == "status")
                    {
                        AtualizarStatus(dadosJson);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Erro ao processar mensagem: {ex.Message}";
            }
        }

        private void ProcessarDadosExtraidos(JsonElement dadosJson)
        {
            try
            {
                if (!dadosJson.TryGetProperty("dados", out var dados)) return;

                _dadosExtraidos = new BoletoData();

                // Extração dos dados do JSON
                if (dados.TryGetProperty("beneficiario", out var beneficiario))
                    _dadosExtraidos.Beneficiario = beneficiario.GetString() ?? "";

                if (dados.TryGetProperty("cnpjBeneficiario", out var cnpj))
                    _dadosExtraidos.CnpjBeneficiario = cnpj.GetString();

                if (dados.TryGetProperty("cepBeneficiario", out var cep))
                    _dadosExtraidos.CepBeneficiario = cep.GetString();

                if (dados.TryGetProperty("estadoBeneficiario", out var estado))
                    _dadosExtraidos.EstadoBeneficiario = estado.GetString();

                if (dados.TryGetProperty("pagador", out var pagador))
                    _dadosExtraidos.Pagador = pagador.GetString() ?? "";

                if (dados.TryGetProperty("cnpjPagador", out var cnpjPagador))
                    _dadosExtraidos.CnpjPagador = cnpjPagador.GetString();

                if (dados.TryGetProperty("linhaDigitavel", out var linha))
                    _dadosExtraidos.LinhaDigitavel = linha.GetString() ?? "";

                if (dados.TryGetProperty("nossoNumero", out var nosso))
                    _dadosExtraidos.NossoNumero = nosso.GetString();

                if (dados.TryGetProperty("agenciaCodigoBeneficiario", out var agencia))
                    _dadosExtraidos.AgenciaCodigoBeneficiario = agencia.GetString();

                // Parse do valor
                if (dados.TryGetProperty("valor", out var valor))
                {
                    try
                    {
                        string valorStr = valor.GetString() ?? "0";
                        valorStr = valorStr.Replace("R$", "").Replace(".", "").Replace(",", ".").Trim();
                        if (decimal.TryParse(valorStr, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal valorDecimal))
                            _dadosExtraidos.Valor = valorDecimal;
                        else
                            _dadosExtraidos.Valor = 0;
                    }
                    catch
                    {
                        _dadosExtraidos.Valor = 0;
                    }
                }

                // Parse da data de vencimento
                if (dados.TryGetProperty("vencimento", out var vencimento))
                {
                    try
                    {
                        string dataStr = vencimento.GetString() ?? "";
                        if (DateTime.TryParseExact(dataStr, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime data))
                            _dadosExtraidos.DataVencimento = data;
                        else if (DateTime.TryParse(dataStr, out DateTime dataGeneric))
                            _dadosExtraidos.DataVencimento = dataGeneric;
                        else
                            _dadosExtraidos.DataVencimento = DateTime.Now.AddMonths(1);
                    }
                    catch
                    {
                        _dadosExtraidos.DataVencimento = DateTime.Now.AddMonths(1);
                    }
                }

                // IMPORTANTE: Definir o caminho do arquivo se foi informado
                if (!string.IsNullOrEmpty(_arquivoBoleto))
                {
                    _dadosExtraidos.CaminhoArquivo = _arquivoBoleto;
                    Console.WriteLine($"Caminho do arquivo definido: {_arquivoBoleto}");
                }

                PreencherCampos();

                _dadosCarregados = true;
                ExtrairRetornarButton.IsEnabled = true;
                StatusTextBlock.Text = "✅ Dados extraídos com sucesso do boleto!";

                // Debug JSON
                JsonDebugTextBox.Text = JsonSerializer.Serialize(dados, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Erro ao processar dados extraídos: {ex.Message}";
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PreencherCampos()
        {
            if (_dadosExtraidos == null) return;

            try
            {
                BeneficiarioTextBox.Text = _dadosExtraidos.Beneficiario;
                CnpjBeneficiarioTextBox.Text = _dadosExtraidos.CnpjBeneficiario ?? "";
                CepBeneficiarioTextBox.Text = _dadosExtraidos.CepBeneficiario ?? "";
                EstadoBeneficiarioTextBox.Text = _dadosExtraidos.EstadoBeneficiario ?? "";
                PagadorTextBox.Text = _dadosExtraidos.Pagador;
                CnpjPagadorTextBox.Text = _dadosExtraidos.CnpjPagador ?? "";
                ValorTextBox.Text = _dadosExtraidos.Valor.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
                VencimentoTextBox.Text = _dadosExtraidos.DataVencimento.ToString("dd/MM/yyyy");
                LinhaDigitavelTextBox.Text = _dadosExtraidos.LinhaDigitavel;
                NossoNumeroTextBox.Text = _dadosExtraidos.NossoNumero ?? "";
                AgenciaCodigoTextBox.Text = _dadosExtraidos.AgenciaCodigoBeneficiario ?? "";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Erro ao preencher campos: {ex.Message}";
            }
        }

        private void AtualizarStatus(JsonElement statusJson)
        {
            if (statusJson.TryGetProperty("message", out var message))
                StatusTextBlock.Text = message.GetString() ?? "Processando...";
        }

        private void Limpar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Limpa todos os campos
                BeneficiarioTextBox.Text = "";
                CnpjBeneficiarioTextBox.Text = "";
                CepBeneficiarioTextBox.Text = "";
                EstadoBeneficiarioTextBox.Text = "";
                PagadorTextBox.Text = "";
                CnpjPagadorTextBox.Text = "";
                ValorTextBox.Text = "";
                VencimentoTextBox.Text = "";
                LinhaDigitavelTextBox.Text = "";
                NossoNumeroTextBox.Text = "";
                AgenciaCodigoTextBox.Text = "";
                JsonDebugTextBox.Text = "";

                _dadosExtraidos = null;
                _dadosCarregados = false;
                ExtrairRetornarButton.IsEnabled = false;
                StatusTextBlock.Text = "Campos limpos. Faça upload de um novo arquivo.";

                // Envia comando de limpeza para o HTML
                try
                {
                    WebViewExtractor.CoreWebView2?.PostWebMessageAsString("{\"action\":\"limpar\"}");
                }
                catch { }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Erro ao limpar campos: {ex.Message}";
            }
        }

        private void ExtrairERetornar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dadosExtraidos == null)
                {
                    MessageBox.Show("Nenhum dado foi extraído ainda.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Atualiza dados dos campos editados para o objeto
                AtualizarDadosDoFormulario();

                // Verifica se o CNPJ do pagador é exatamente "38.046.801/0001-60"
                if (!string.IsNullOrWhiteSpace(_dadosExtraidos.CnpjPagador))
                {
                    string cnpjLimpo = _dadosExtraidos.CnpjPagador.Replace(".", "").Replace("/", "").Replace("-", "");
                    string cnpjEsperado = "38046801000160";

                    if (cnpjLimpo != cnpjEsperado)
                    {
                        MessageBox.Show(
                            $"CNPJ do pagador inválido!\n\n" +
                            $"CNPJ encontrado: {_dadosExtraidos.CnpjPagador}\n" +
                            $"CNPJ esperado: 38.046.801/0001-60\n\n" +
                            $"Por favor, verifique se o boleto é realmente da empresa Radiadores Lemos.",
                            "CNPJ Inválido",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                }
                else
                {
                    MessageBox.Show(
                        "CNPJ do pagador não foi informado!\n\n" +
                        "Por favor, preencha o CNPJ do pagador para continuar.",
                        "CNPJ Obrigatório",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // SEMPRE garantir que o caminho do arquivo seja preservado
                if (!string.IsNullOrEmpty(_arquivoBoleto))
                {
                    _dadosExtraidos.CaminhoArquivo = _arquivoBoleto;
                }

                _dadosExtraidos.DataCadastro = DateTime.UtcNow;
                _dadosExtraidos.UsuarioCadastro = MainWindow.UsuarioLogado?.Nome;
                _dadosExtraidos.Status = _dadosExtraidos.DataVencimento < DateTime.Now ? StatusBoleto.Vencido : StatusBoleto.Pendente;

                // Importante: atribuir à propriedade BoletoSalvo para retornar os dados
                BoletoSalvo = _dadosExtraidos;

                // Debug para verificar se o caminho está sendo atribuído corretamente
                Console.WriteLine($"Caminho do arquivo sendo retornado: {_dadosExtraidos.CaminhoArquivo}");

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao extrair dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AtualizarDadosDoFormulario()
        {
            if (_dadosExtraidos == null) return;

            _dadosExtraidos.Beneficiario = BeneficiarioTextBox.Text;
            _dadosExtraidos.CnpjBeneficiario = CnpjBeneficiarioTextBox.Text;
            _dadosExtraidos.CepBeneficiario = CepBeneficiarioTextBox.Text;
            _dadosExtraidos.EstadoBeneficiario = EstadoBeneficiarioTextBox.Text;
            _dadosExtraidos.Pagador = PagadorTextBox.Text;
            _dadosExtraidos.CnpjPagador = CnpjPagadorTextBox.Text;

            string valorStr = ValorTextBox.Text.Replace("R$", "").Replace(".", "").Replace(",", ".").Trim();
            if (decimal.TryParse(valorStr, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal valorDecimal))
                _dadosExtraidos.Valor = valorDecimal;

            if (DateTime.TryParseExact(VencimentoTextBox.Text, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dataVencimento))
                _dadosExtraidos.DataVencimento = dataVencimento;

            _dadosExtraidos.LinhaDigitavel = LinhaDigitavelTextBox.Text;
            _dadosExtraidos.NossoNumero = NossoNumeroTextBox.Text;
            _dadosExtraidos.AgenciaCodigoBeneficiario = AgenciaCodigoTextBox.Text;

            // SEMPRE preserve o caminho do arquivo
            if (string.IsNullOrEmpty(_dadosExtraidos.CaminhoArquivo) && !string.IsNullOrEmpty(_arquivoBoleto))
            {
                _dadosExtraidos.CaminhoArquivo = _arquivoBoleto;
            }
        }

        private void Fechar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
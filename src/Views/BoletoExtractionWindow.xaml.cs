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

        public BoletoData? BoletoSalvo { get; private set; }

        public BoletoExtractionWindow()
        {
            InitializeComponent();
            Loaded += BoletoExtractionWindow_Loaded;
        }

        public BoletoExtractionWindow(string caminhoArquivo)
        {
            InitializeComponent();
            _arquivoBoleto = caminhoArquivo;
            Loaded += BoletoExtractionWindow_Loaded;
        }

        private async void BoletoExtractionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await InicializarWebView();

                // Solicitar o arquivo do boleto imediatamente ao abrir a janela
                SelecionarArquivoDoBoletoPrimeiro();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar a janela: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = $"Erro ao inicializar: {ex.Message}";
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

                    // Aqui você poderia enviar o arquivo para o WebView processar
                    // através de alguma comunicação JavaScript, se possível
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
                await WebViewExtractor.EnsureCoreWebView2Async(null);
                WebViewExtractor.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                await CarregarArquivoHtml();

                StatusTextBlock.Text = "Aplicação carregada. Use o extrator para processar o boleto.";
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

                // Definir o caminho do arquivo se foi informado
                if (!string.IsNullOrEmpty(_arquivoBoleto))
                {
                    _dadosExtraidos.CaminhoArquivo = _arquivoBoleto;
                }

                PreencherCampos();

                _dadosCarregados = true;
                SalvarBoletoButton.IsEnabled = true;
                ExtrairRetornarButton.IsEnabled = true;
                StatusTextBlock.Text = "✅ Dados extraídos com sucesso do boleto!";

                // Debug JSON
                JsonDebugTextBox.Text = JsonSerializer.Serialize(dados, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                // Exibe uma mensagem de sucesso mais simples para evitar problemas
                MessageBox.Show(
                    "Dados do boleto extraídos com sucesso!\n\n" +
                    "Você pode agora verificar os dados, validá-los e clicar em 'Extrair e Retornar' " +
                    "para enviar os dados de volta à tela anterior.",
                    "Extração Concluída",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
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
                SalvarBoletoButton.IsEnabled = false;
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

        private void SalvarBoleto_Click(object sender, RoutedEventArgs e)
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

                // Verifica se o CNPJ do pagador é válido
                if (!BoletoValidationService.ValidarCnpjPagador(_dadosExtraidos.CnpjPagador))
                {
                    return;
                }

                _dadosExtraidos.DataCadastro = DateTime.UtcNow;
                _dadosExtraidos.UsuarioCadastro = MainWindow.UsuarioLogado?.Nome;
                _dadosExtraidos.Status = _dadosExtraidos.DataVencimento < DateTime.Now ? StatusBoleto.Vencido : StatusBoleto.Pendente;

                var db = DatabaseConnect.Database;
                if (db != null)
                {
                    var collection = db.GetCollection<BoletoData>("boletos");
                    collection.Insert(_dadosExtraidos);

                    BoletoSalvo = _dadosExtraidos;
                    MessageBox.Show("Boleto salvo com sucesso no banco de dados!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Erro ao conectar com o banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar boleto: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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
                _dadosExtraidos.CaminhoArquivo = _arquivoBoleto;

                _dadosExtraidos.DataCadastro = DateTime.UtcNow;
                _dadosExtraidos.UsuarioCadastro = MainWindow.UsuarioLogado?.Nome;
                _dadosExtraidos.Status = _dadosExtraidos.DataVencimento < DateTime.Now ? StatusBoleto.Vencido : StatusBoleto.Pendente;

                // Importante: atribuir à propriedade BoletoSalvo para retornar os dados
                BoletoSalvo = _dadosExtraidos;

                // Debug para verificar se o caminho está sendo atribuído corretamente
                Console.WriteLine($"Caminho do arquivo sendo retornado: {_dadosExtraidos.CaminhoArquivo}");

                MessageBox.Show("Dados extraídos com sucesso! Retornando para a tela principal.",
                    "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

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

            if (string.IsNullOrEmpty(_dadosExtraidos.CaminhoArquivo) && !string.IsNullOrEmpty(_arquivoBoleto))
            {
                _dadosExtraidos.CaminhoArquivo = _arquivoBoleto;
            }
        }

        private void ValidarDados_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<string> erros = new List<string>();

                if (string.IsNullOrWhiteSpace(BeneficiarioTextBox.Text))
                    erros.Add("• Beneficiário não pode estar vazio");

                string cnpj = CnpjBeneficiarioTextBox.Text?.Replace(".", "").Replace("/", "").Replace("-", "");
                if (!string.IsNullOrEmpty(cnpj) && cnpj.Length != 14)
                    erros.Add("• CNPJ deve ter 14 dígitos");

                // Validação do CNPJ do pagador
                if (string.IsNullOrWhiteSpace(CnpjPagadorTextBox.Text))
                {
                    erros.Add("• CNPJ do pagador é obrigatório");
                }
                else
                {
                    string cnpjPagadorLimpo = CnpjPagadorTextBox.Text.Replace(".", "").Replace("/", "").Replace("-", "");
                    string cnpjEsperado = "38046801000160";
                    
                    if (cnpjPagadorLimpo != cnpjEsperado)
                    {
                        erros.Add("• CNPJ do pagador deve ser exatamente 38.046.801/0001-60 (Radiadores Lemos)");
                    }
                }

                if (!string.IsNullOrEmpty(ValorTextBox.Text))
                {
                    string valorStr = ValorTextBox.Text.Replace("R$", "").Replace(".", "").Replace(",", ".").Trim();
                    if (!decimal.TryParse(valorStr, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal valor) || valor <= 0)
                        erros.Add("• Valor deve ser maior que zero");
                }

                if (!string.IsNullOrEmpty(VencimentoTextBox.Text))
                {
                    if (!DateTime.TryParseExact(VencimentoTextBox.Text, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime data))
                        erros.Add("• Data de vencimento deve estar no formato dd/MM/yyyy");
                }

                if (erros.Count > 0)
                {
                    MessageBox.Show(
                        "❌ Dados com problemas:\n\n" + string.Join("\n", erros),
                        "Validação",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
                else
                {
                    MessageBox.Show(
                        "✅ Todos os dados estão válidos!\n\nVocê pode extrair e retornar ou salvar o boleto.",
                        "Validação",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    SalvarBoletoButton.IsEnabled = true;
                    ExtrairRetornarButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro na validação: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Fechar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
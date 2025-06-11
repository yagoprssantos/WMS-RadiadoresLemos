using Microsoft.Web.WebView2.Core;
using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class BoletoExtractionWindow : Window
    {
        private BoletoData? _dadosExtraidos;
        private bool _dadosCarregados = false;

        public BoletoData? BoletoSalvo { get; private set; }

        public BoletoExtractionWindow()
        {
            InitializeComponent();
            Loaded += BoletoExtractionWindow_Loaded;
        }

        private async void BoletoExtractionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await InicializarWebView();
        }

        private async Task InicializarWebView()
        {
            try
            {
                await WebViewExtractor.EnsureCoreWebView2Async(null);
                WebViewExtractor.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                // 🚀 CARREGA SEU ARQUIVO BOLETOEXTRACTOR.HTML
                await CarregarArquivoHtml();

                StatusTextBlock.Text = "Aplicação carregada. Faça upload do arquivo de boleto.";
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
                // 🔧 CAMINHOS POSSÍVEIS PARA SEU ARQUIVO
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

                // 📋 EXTRAI DADOS CONFORME SEU HTML
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

                if (dados.TryGetProperty("linhaDigitavel", out var linha))
                    _dadosExtraidos.LinhaDigitavel = linha.GetString() ?? "";

                if (dados.TryGetProperty("nossoNumero", out var nosso))
                    _dadosExtraidos.NossoNumero = nosso.GetString();

                if (dados.TryGetProperty("agenciaCodigoBeneficiario", out var agencia))
                    _dadosExtraidos.AgenciaCodigoBeneficiario = agencia.GetString();

                // Parse do valor
                if (dados.TryGetProperty("valor", out var valor))
                {
                    string valorStr = valor.GetString() ?? "0";
                    valorStr = valorStr.Replace("R$", "").Replace(".", "").Replace(",", ".").Trim();
                    if (decimal.TryParse(valorStr, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal valorDecimal))
                        _dadosExtraidos.Valor = valorDecimal;
                }

                // Parse da data de vencimento
                if (dados.TryGetProperty("vencimento", out var vencimento))
                {
                    string dataStr = vencimento.GetString() ?? "";
                    if (DateTime.TryParseExact(dataStr, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime data))
                        _dadosExtraidos.DataVencimento = data;
                    else if (DateTime.TryParse(dataStr, out DateTime dataGeneric))
                        _dadosExtraidos.DataVencimento = dataGeneric;
                }

                // 🎨 PREENCHE A INTERFACE
                PreencherCampos();

                _dadosCarregados = true;
                SalvarBoletoButton.IsEnabled = true;
                StatusTextBlock.Text = "✅ Dados extraídos com sucesso do boleto real!";

                // Debug JSON
                JsonDebugTextBox.Text = JsonSerializer.Serialize(dados, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                // 🎉 NOTIFICAÇÃO COM DADOS REAIS
                MessageBox.Show(
                    $"🎉 Dados do boleto extraídos!\n\n" +
                    $"Beneficiário: {_dadosExtraidos.Beneficiario}\n" +
                    $"CNPJ: {_dadosExtraidos.CnpjBeneficiario}\n" +
                    $"Pagador: {_dadosExtraidos.Pagador}\n" +
                    $"Valor: R$ {_dadosExtraidos.Valor:N2}\n" +
                    $"Vencimento: {_dadosExtraidos.DataVencimento:dd/MM/yyyy}",
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

            BeneficiarioTextBox.Text = _dadosExtraidos.Beneficiario;
            CnpjBeneficiarioTextBox.Text = _dadosExtraidos.CnpjBeneficiario ?? "";
            CepBeneficiarioTextBox.Text = _dadosExtraidos.CepBeneficiario ?? "";
            EstadoBeneficiarioTextBox.Text = _dadosExtraidos.EstadoBeneficiario ?? "";
            PagadorTextBox.Text = _dadosExtraidos.Pagador;
            ValorTextBox.Text = _dadosExtraidos.Valor.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
            VencimentoTextBox.Text = _dadosExtraidos.DataVencimento.ToString("dd/MM/yyyy");
            LinhaDigitavelTextBox.Text = _dadosExtraidos.LinhaDigitavel;
            NossoNumeroTextBox.Text = _dadosExtraidos.NossoNumero ?? "";
            AgenciaCodigoTextBox.Text = _dadosExtraidos.AgenciaCodigoBeneficiario ?? "";
        }

        private void AtualizarStatus(JsonElement statusJson)
        {
            if (statusJson.TryGetProperty("message", out var message))
                StatusTextBlock.Text = message.GetString() ?? "Processando...";
        }

        private void Limpar_Click(object sender, RoutedEventArgs e)
        {
            // Limpa todos os campos
            BeneficiarioTextBox.Text = "";
            CnpjBeneficiarioTextBox.Text = "";
            CepBeneficiarioTextBox.Text = "";
            EstadoBeneficiarioTextBox.Text = "";
            PagadorTextBox.Text = "";
            ValorTextBox.Text = "";
            VencimentoTextBox.Text = "";
            LinhaDigitavelTextBox.Text = "";
            NossoNumeroTextBox.Text = "";
            AgenciaCodigoTextBox.Text = "";
            JsonDebugTextBox.Text = "";

            _dadosExtraidos = null;
            _dadosCarregados = false;
            SalvarBoletoButton.IsEnabled = false;
            StatusTextBlock.Text = "Campos limpos. Faça upload de um novo arquivo.";

            // Envia comando de limpeza para o HTML
            try
            {
                WebViewExtractor.CoreWebView2?.PostWebMessageAsString("{\"action\":\"limpar\"}");
            }
            catch { }
        }

        private async void SalvarBoleto_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dadosExtraidos == null)
                {
                    MessageBox.Show("Nenhum dado foi extraído ainda.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    MessageBox.Show("Boleto salvo com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void ValidarDados_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<string> erros = new List<string>();

                // Validação do Beneficiário
                if (string.IsNullOrWhiteSpace(BeneficiarioTextBox.Text))
                    erros.Add("• Beneficiário não pode estar vazio");

                // Validação do CNPJ
                string cnpj = CnpjBeneficiarioTextBox.Text?.Replace(".", "").Replace("/", "").Replace("-", "");
                if (!string.IsNullOrEmpty(cnpj) && cnpj.Length != 14)
                    erros.Add("• CNPJ deve ter 14 dígitos");

                // Validação do Valor
                if (!string.IsNullOrEmpty(ValorTextBox.Text))
                {
                    string valorStr = ValorTextBox.Text.Replace("R$", "").Replace(".", "").Replace(",", ".").Trim();
                    if (!decimal.TryParse(valorStr, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal valor) || valor <= 0)
                        erros.Add("• Valor deve ser maior que zero");
                }

                // Validação da Data
                if (!string.IsNullOrEmpty(VencimentoTextBox.Text))
                {
                    if (!DateTime.TryParseExact(VencimentoTextBox.Text, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime data))
                        erros.Add("• Data de vencimento deve estar no formato dd/MM/yyyy");
                }

                // Mostra resultado
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
                        "✅ Todos os dados estão válidos!\n\nVocê pode salvar o boleto com segurança.",
                        "Validação",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    SalvarBoletoButton.IsEnabled = true;
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
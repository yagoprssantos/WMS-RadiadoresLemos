using LiteDB;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq; // Essencial para LINQ (Where, Select, etc.)
using System.Net.Http;
using System.Text;
using SystemTextJson = System.Text.Json; // Alias para System.Text.Json
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml.Linq;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class AddEntradaSaídaWindow : Window
    {
        private List<ProdutoData> produtos = new List<ProdutoData>();
        private ObservableCollection<MovimentacaoData> movimentacoes = new ObservableCollection<MovimentacaoData>();

        private List<MovimentacaoListItem> listaMovimentacoes = new();
        private List<CompraData> compras = new();
        private List<VendaData> vendas = new();
        private MovimentacaoData _itemEmEdicao = null;

        private ProdutoData? produtoSelecionado;
        private bool usePositiveNumber;
        private List<ClienteData> clientes = new List<ClienteData>();
        private string? clienteSelecionadoId;
        private string? clienteSelecionadoDisplay;
        private List<FornecedorData> fornecedores = new List<FornecedorData>();
        private string? fornecedorSelecionadoId;
        private string? fornecedorSelecionadoNome;

        private string? formaPagamentoSelecionada;
        private readonly List<string> opcoesFormaPagamento;
        private ObservableCollection<BoletoData> boletos = new ObservableCollection<BoletoData>();
        private string? numeroNotaFiscalAtual;

        private static readonly HttpClient httpClient = new HttpClient();
        private string GeminiApiKey = "AIzaSyDE-arZPG2EgGJRGSZtz4-k0o7KF4bfNTw";


        public AddEntradaSaídaWindow()
        {
            InitializeComponent();
            opcoesFormaPagamento = FormaPagamentoComboBox.Items.Cast<ComboBoxItem>()
                                    .Select(item => item.Content?.ToString() ?? "")
                                    .Where(s => !string.IsNullOrEmpty(s))
                                    .ToList();
            ListaItemsControl.ItemsSource = listaMovimentacoes;
            BoletosItemsControl.ItemsSource = boletos;
        }

        public AddEntradaSaídaWindow(bool isEntrada) : this()
        {
            usePositiveNumber = isEntrada;
            Setup(isEntrada);
            Title = isEntrada ? "Registrar Nova Compra" : "Registrar Nova Venda";

            var extracaoGroupBox = FindName("ExtrairBoletoGroupBox") as GroupBox;
            var camposBoletosStackPanel = FindName("CamposBoletos") as StackPanel;

            if (isEntrada)
            {
                Fornecedor.Visibility = Visibility.Visible;
                Cliente.Visibility = Visibility.Collapsed;
                if (extracaoGroupBox != null) extracaoGroupBox.Visibility = Visibility.Visible;
                if (camposBoletosStackPanel != null) camposBoletosStackPanel.Visibility = Visibility.Visible;
            }
            else
            {
                Fornecedor.Visibility = Visibility.Collapsed;
                Cliente.Visibility = Visibility.Visible;

                if (extracaoGroupBox != null) extracaoGroupBox.Visibility = Visibility.Collapsed;
                if (camposBoletosStackPanel != null) camposBoletosStackPanel.Visibility = Visibility.Collapsed;


                // Boletos
                CamposBoletos.Visibility = Visibility.Collapsed;

            }
        }

        private async void Setup(bool isEntrada)
        {
            produtoSelecionado = null;
            await CarregarDados();
            ToggleVisibility(false);
            Invalida();
        }

        private async Task CarregarDados()
        {
            await CarregarProdutos();
            if (usePositiveNumber) await CarregarFornecedores();
            else await CarregarClientes();
        }

        private async Task CarregarProdutos()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db != null)
                {
                    var collection = db.GetCollection<ProdutoData>("produtos");
                    produtos = await Task.Run(() => collection.FindAll().OrderBy(p => p.Nome).ToList());
                    ProdutoComboBox.ItemsSource = produtos.Select(p => p.Nome).ToList();
                }
            }
            catch (Exception ex) { MessageBox.Show($"Erro ao carregar produtos: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private async Task CarregarFornecedores()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db != null)
                {
                    var collection = db.GetCollection<FornecedorData>("fornecedores");
                    fornecedores = await Task.Run(() => collection.FindAll().OrderBy(f => f.Nome).ToList());
                    FornecedorComboBox.ItemsSource = fornecedores.Select(f => f.Nome).ToList();
                }
            }
            catch (Exception ex) { MessageBox.Show($"Erro ao carregar fornecedores: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private async Task CarregarClientes()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db != null)
                {
                    var collection = db.GetCollection<ClienteData>("clientes");
                    clientes = await Task.Run(() => collection.FindAll().OrderBy(c => c.CNPJ).ToList());
                    ClienteComboBox.ItemsSource = clientes.Select(c => $"{c.CNPJ} ({c.Email})").ToList();
                }
            }
            catch (Exception ex) { MessageBox.Show($"Erro ao carregar clientes: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ToggleVisibility(bool isVisible)
        {
            var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            if (FindName("ProdutoAntesDepois") is Grid grid)
            {
                grid.Visibility = visibility;
            }
        }

        private async void BtnExtrairDeArquivo_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(GeminiApiKey) || GeminiApiKey == "SUA_CHAVE_API_AQUI")
            {

                MessageBox.Show("Configure sua chave da API Gemini na variável 'GeminiApiKey'.", "Chave API Necessária", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;

                produtoSelecionado = produtos.FirstOrDefault(p => p.Nome == inputText);
                if (produtoSelecionado != null)
                {
                    AtualizarCamposProduto(produtoSelecionado);
                    DestacarMudancas();
                    ValidarMovimentacao();
                }

            }
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Selecionar Arquivo de Boleto",
                Filter = "Arquivos Suportados (*.png;*.jpg;*.jpeg;*.pdf)|*.png;*.jpg;*.jpeg;*.pdf|Imagens (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|PDF (*.pdf)|*.pdf|Todos (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                string fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
                ShowProgressExtracao("Iniciando...", true);
                try
                {

                    string base64ImageData = ""; string ocrText = "";
                    if (fileExtension == ".pdf")
                    {
                        ShowProgressExtracao("Processamento de PDF não implementado. Converta para imagem.", false, true);
                        MessageBox.Show("Converta o PDF para imagem (PNG/JPG) e tente novamente.", "PDF", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    else if (fileExtension == ".png" || fileExtension == ".jpg" || fileExtension == ".jpeg")
                    {
                        ShowProgressExtracao("Processando imagem...", true);
                        byte[] imageBytes = File.ReadAllBytes(filePath);
                        base64ImageData = Convert.ToBase64String(imageBytes);
                        ShowProgressExtracao("Extraindo texto da imagem (OCR)...", true);
                        ocrText = await ExtractTextFromImageAPIAsync(base64ImageData);
                    }
                    else { throw new Exception("Formato de arquivo não suportado."); }
                    if (string.IsNullOrWhiteSpace(ocrText)) { throw new Exception("Não foi possível extrair texto do arquivo."); }
                    ShowProgressExtracao("Estruturando dados...", true);
                    BoletoExtraidoData structuredData = await StructureTextToJsonAPIAsync(ocrText);
                    PopulateFieldsFromExtractedBoleto(structuredData, filePath);
                    ShowProgressExtracao("Dados extraídos! Verifique os campos e a lista de boletos.", false, isSuccess: true);

                    AtualizarCamposProduto(produtoSelecionado);
                    DestacarMudancas();
                    ValidarMovimentacao();

                }
                catch (Exception ex)
                {
                    ShowProgressExtracao($"Erro: {ex.Message}", false, true);
                    MessageBox.Show($"Erro na extração: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task<string> ExtractTextFromImageAPIAsync(string base64ImageData)
        {
            var imagePrompt = "Extraia todo o texto desta imagem de um boleto bancário brasileiro. Priorize a precisão de linha digitável, valor, vencimento, beneficiário e pagador.";
            var payload = new { contents = new[] { new { parts = new object[] { new { text = imagePrompt }, new { inlineData = new { mimeType = "image/jpeg", data = base64ImageData } } } } } };
            string requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={GeminiApiKey}";
            var jsonPayload = SystemTextJson.JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync(requestUri, content);
            string responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new Exception($"API OCR: {response.StatusCode} - {responseBody}");
            var geminiResponse = SystemTextJson.JsonSerializer.Deserialize<GeminiResponse>(responseBody);
            return geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";
        }


        private async Task<BoletoExtraidoData> StructureTextToJsonAPIAsync(string extractedText)
        {
            var schema = new GeminiSchema { Type = "OBJECT", Properties = new Dictionary<string, GeminiProperty> { { "beneficiario", new GeminiProperty { Type = "STRING", Description = "Nome do beneficiário. Se houver 'Beneficiário Final', usar este." } }, { "cnpjBeneficiario", new GeminiProperty { Type = "STRING", Description = "CNPJ do beneficiário (ou Final)." } }, { "cepBeneficiario", new GeminiProperty { Type = "STRING", Description = "CEP do beneficiário (ou Final)." } }, { "estadoBeneficiario", new GeminiProperty { Type = "STRING", Description = "Estado (UF) do beneficiário (ou Final)." } }, { "pagador", new GeminiProperty { Type = "STRING", Description = "Nome do pagador." } }, { "vencimento", new GeminiProperty { Type = "STRING", Description = "Data de vencimento (DD/MM/AAAA)." } }, { "valor", new GeminiProperty { Type = "STRING", Description = "Valor do boleto (ex: 123,45)." } }, { "linhaDigitavel", new GeminiProperty { Type = "STRING", Description = "Linha digitável completa." } }, { "nossoNumero", new GeminiProperty { Type = "STRING", Description = "'Nosso Número'." } }, { "agenciaCodigoBeneficiario", new GeminiProperty { Type = "STRING", Description = "'Agência / Código Beneficiário'." } } } };
            var jsonPrompt = $"Analise o texto OCR de um boleto e preencha o schema JSON. Se 'Beneficiário Final' existir, use seus dados para os campos de beneficiário. Se um campo não for encontrado, retorne null ou string vazia. Texto OCR:\n\n{extractedText}";
            var payload = new { contents = new[] { new { parts = new[] { new { text = jsonPrompt } } } }, generationConfig = new { responseMimeType = "application/json", responseSchema = schema } };
            string requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={GeminiApiKey}";
            var jsonPayload = SystemTextJson.JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync(requestUri, content);
            string responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new Exception($"API JSON: {response.StatusCode} - {responseBody}");
            var geminiResponse = SystemTextJson.JsonSerializer.Deserialize<GeminiResponse>(responseBody);
            string jsonDataPart = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            if (string.IsNullOrWhiteSpace(jsonDataPart)) throw new Exception("API JSON retornou resposta vazia.");
            return SystemTextJson.JsonSerializer.Deserialize<BoletoExtraidoData>(jsonDataPart) ?? new BoletoExtraidoData();

            if (FornecedorComboBox.SelectedItem is string selected)
                inputText = selected;

            var fornecedor = fornecedores.FirstOrDefault(f => f.Nome == inputText);
            if (!string.IsNullOrEmpty(inputText) && fornecedor != null)
            {
                fornecedorSelecionado = fornecedor.Nome;
            }
            else
            {
                FornecedorComboBox.Text = string.Empty;
                FornecedorComboBox.SelectedItem = null;
                fornecedorSelecionado = null;
            }

            ValidarMovimentacao();

        }

        private void PopulateFieldsFromExtractedBoleto(BoletoExtraidoData data, string filePath)
        {
            if (string.IsNullOrWhiteSpace(NotaFiscalTextBox.Text)) { NotaFiscalTextBox.Text = data.NossoNumero ?? data.LinhaDigitavel?.Split(' ').LastOrDefault()?.Trim() ?? ""; }
            if (usePositiveNumber && !string.IsNullOrWhiteSpace(data.Beneficiario)) { if (string.IsNullOrWhiteSpace(FornecedorComboBox.Text) || FornecedorComboBox.SelectedItem == null) { var fornecedorEncontrado = fornecedores.FirstOrDefault(f => f.Nome.Equals(data.Beneficiario, StringComparison.OrdinalIgnoreCase)); if (fornecedorEncontrado != null) { FornecedorComboBox.SelectedItem = fornecedorEncontrado.Nome; fornecedorSelecionadoId = fornecedorEncontrado.Id; fornecedorSelecionadoNome = fornecedorEncontrado.Nome; } else { FornecedorComboBox.Text = data.Beneficiario; fornecedorSelecionadoNome = data.Beneficiario; fornecedorSelecionadoId = null; } } }
            int proximaParcela = boletos.Count + 1;
            // ✅ CÓDIGO CORRIGIDO:
            var novoBoleto = new BoletoData
            {

                DataVencimento = DateTime.TryParseExact(data.Vencimento, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var venc) ? venc : DateTime.Today.AddMonths(proximaParcela - 1),
                CaminhoArquivo = filePath,
                LinhaDigitavel = data.LinhaDigitavel ?? "",
                Beneficiario = data.Beneficiario ?? "",
                CnpjBeneficiario = data.CnpjBeneficiario,
                Pagador = data.Pagador ?? "",
                Valor = decimal.TryParse(data.Valor?.Replace(".", "").Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out var valorDecimal) ? valorDecimal : 0,
                NossoNumero = data.NossoNumero,
                AgenciaCodigoBeneficiario = data.AgenciaCodigoBeneficiario,
                Status = StatusBoleto.Pendente,
                DataCadastro = DateTime.UtcNow,
                UsuarioCadastro = MainWindow.UsuarioLogado?.Nome,
                Observacoes = $"Parcela {proximaParcela} - Extraído automaticamente"
            };
            boletos.Add(novoBoleto);
            BoletosItemsControl.Items.Refresh();
            if (string.IsNullOrWhiteSpace(ParcelasTextBox.Text) || ParcelasTextBox.Text == "0") { ParcelasTextBox.Text = boletos.Count.ToString(); }
            if (FormaPagamentoComboBox.SelectedIndex == -1 && boletos.Count > 0) { var itemParcelado = FormaPagamentoComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(cbi => cbi.Content?.ToString() == "Parcelado"); if (itemParcelado != null) FormaPagamentoComboBox.SelectedItem = itemParcelado; }
            StringBuilder detalhesAdicionais = new StringBuilder(); if (!string.IsNullOrWhiteSpace(DetalhesTextBox.Text)) detalhesAdicionais.AppendLine(DetalhesTextBox.Text).AppendLine("---");
            detalhesAdicionais.AppendLine($"Dados Extraídos do Boleto (Parcela {proximaParcela}):");
            if (!string.IsNullOrWhiteSpace(data.Beneficiario)) detalhesAdicionais.AppendLine($"  Beneficiário: {data.Beneficiario}");
            if (!string.IsNullOrWhiteSpace(data.CnpjBeneficiario)) detalhesAdicionais.AppendLine($"  CNPJ Benef.: {data.CnpjBeneficiario}");
            if (!string.IsNullOrWhiteSpace(data.Pagador)) detalhesAdicionais.AppendLine($"  Pagador: {data.Pagador}");
            if (!string.IsNullOrWhiteSpace(data.LinhaDigitavel)) detalhesAdicionais.AppendLine($"  Linha Digitável: {data.LinhaDigitavel}");
            if (!string.IsNullOrWhiteSpace(data.NossoNumero)) detalhesAdicionais.AppendLine($"  Nosso Número: {data.NossoNumero}");
            if (!string.IsNullOrWhiteSpace(data.AgenciaCodigoBeneficiario)) detalhesAdicionais.AppendLine($"  Ag/Cód. Benef.: {data.AgenciaCodigoBeneficiario}");
            if (!string.IsNullOrWhiteSpace(data.Valor)) detalhesAdicionais.AppendLine($"  Valor (Boleto): {data.Valor}");
            if (!string.IsNullOrWhiteSpace(data.Vencimento)) detalhesAdicionais.AppendLine($"  Vencimento (Boleto): {data.Vencimento}");
            DetalhesTextBox.Text = detalhesAdicionais.ToString().Trim();
            ValidarMovimentacao(); ValidarFinanceiro();
        }

        private void ShowProgressExtracao(string message, bool isLoading, bool isError = false, bool isSuccess = false)
        {
            if (TxtStatusExtracao != null) { TxtStatusExtracao.Text = message; TxtStatusExtracao.Visibility = Visibility.Visible; TxtStatusExtracao.Foreground = System.Windows.Media.Brushes.Gray; if (isError) TxtStatusExtracao.Foreground = System.Windows.Media.Brushes.Red; if (isSuccess) TxtStatusExtracao.Foreground = System.Windows.Media.Brushes.Green; }
            if (ProgressBarExtracao != null) { ProgressBarExtracao.IsIndeterminate = isLoading; ProgressBarExtracao.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed; }
            if (BtnExtrairDeArquivo != null) BtnExtrairDeArquivo.IsEnabled = !isLoading;
        }

        private void ProdutoComboBox_TextChanged(object sender, TextChangedEventArgs e) { if (sender is ComboBox comboBox && comboBox.Template.FindName("PART_EditableTextBox", comboBox) is TextBox textBox) { string searchText = textBox.Text; var filteredProducts = produtos.Where(p => p.Nome.Contains(searchText, StringComparison.OrdinalIgnoreCase)).Select(p => p.Nome).ToList(); comboBox.ItemsSource = null; comboBox.Items.Clear(); foreach (var nome in filteredProducts) { comboBox.Items.Add(nome); } textBox.Text = searchText; textBox.CaretIndex = textBox.Text.Length; comboBox.IsDropDownOpen = true; } }
        private void ProdutoComboBox_LostFocus(object sender, RoutedEventArgs e) { string inputText = ProdutoComboBox.Text; if (ProdutoComboBox.SelectedItem is string selectedProductName) { inputText = selectedProductName; } if (!string.IsNullOrEmpty(inputText) && produtos.Any(p => p.Nome == inputText)) { produtoSelecionado = produtos.FirstOrDefault(p => p.Nome == inputText); if (produtoSelecionado != null) { AtualizarCamposProduto(produtoSelecionado); DestacarMudancas(); ToggleVisibility(true); ValidarMovimentacao(); } } else { ProdutoComboBox.Text = string.Empty; ProdutoComboBox.SelectedItem = null; ToggleVisibility(false); Invalida(); } }
        private void ProdutoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (ProdutoComboBox.SelectedItem is string selectedProductName) { produtoSelecionado = produtos.FirstOrDefault(p => p.Nome == selectedProductName); if (produtoSelecionado != null) { AtualizarCamposProduto(produtoSelecionado); DestacarMudancas(); ToggleVisibility(true); ValidarMovimentacao(); } else { MessageBox.Show("Produto não encontrado no cache."); ToggleVisibility(false); Invalida(); } } }
        private void FornecedorComboBox_TextChanged(object sender, TextChangedEventArgs e) { if (sender is ComboBox comboBox && comboBox.Template.FindName("PART_EditableTextBox", comboBox) is TextBox textBox) { string searchText = textBox.Text; var filteredFornecedores = fornecedores.Where(f => f.Nome.Contains(searchText, StringComparison.OrdinalIgnoreCase)).Select(f => f.Nome).ToList(); comboBox.ItemsSource = null; comboBox.Items.Clear(); foreach (var nome in filteredFornecedores) { comboBox.Items.Add(nome); } textBox.Text = searchText; textBox.CaretIndex = textBox.Text.Length; comboBox.IsDropDownOpen = true; } }
        private void FornecedorComboBox_LostFocus(object sender, RoutedEventArgs e) { string inputText = FornecedorComboBox.Text; if (FornecedorComboBox.SelectedItem is string selected) inputText = selected; var fornecedor = fornecedores.FirstOrDefault(f => f.Nome.Equals(inputText, StringComparison.OrdinalIgnoreCase)); if (fornecedor != null) { fornecedorSelecionadoNome = fornecedor.Nome; fornecedorSelecionadoId = fornecedor.Id; FornecedorComboBox.Text = fornecedor.Nome; } else if (!string.IsNullOrWhiteSpace(inputText)) { fornecedorSelecionadoNome = inputText; fornecedorSelecionadoId = null; } else { FornecedorComboBox.Text = string.Empty; FornecedorComboBox.SelectedItem = null; fornecedorSelecionadoNome = null; fornecedorSelecionadoId = null; } ValidarMovimentacao(); }
        private void FornecedorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (FornecedorComboBox.SelectedItem is string selectedName) { var fornecedor = fornecedores.FirstOrDefault(f => f.Nome == selectedName); if (fornecedor != null) { fornecedorSelecionadoNome = fornecedor.Nome; fornecedorSelecionadoId = fornecedor.Id; } } ValidarMovimentacao(); }

                fornecedorSelecionado = selected;
                ValidarMovimentacao();
            }
        }


        private void ClienteComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Template.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
            {
                string searchText = textBox.Text;

                // AQUI ESTÁ A LINHA COM O PROBLEMA (aproximadamente linha 274 da versão anterior)
                // Verifique se 'clientes' está nulo antes de usar LINQ
                if (clientes == null) return;

                var filteredClientes = clientes.Where(clienteLocal => // Renomeado para 'clienteLocal' para evitar conflito se 'c' for o problema
                                        (clienteLocal.CNPJ != null && clienteLocal.CNPJ.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                                        (clienteLocal.Email != null && clienteLocal.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
                                        .Select(clienteLocal => $"{clienteLocal.CNPJ} ({clienteLocal.Email})").ToList();

                comboBox.ItemsSource = null;
                comboBox.Items.Clear();
                foreach (var display in filteredClientes) { comboBox.Items.Add(display); }
                textBox.Text = searchText;
                textBox.CaretIndex = textBox.Text.Length;
                comboBox.IsDropDownOpen = true;
            }
        }
        private void ClienteComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            string inputText = ClienteComboBox.Text;
          
          
          

            if (ClienteComboBox.SelectedItem is string selectedDisplay)
                inputText = selectedDisplay;
            var cliente = clientes.FirstOrDefault(c => $"{c.CNPJ} ({c.Email})" == inputText);
            if (cliente != null)
            {
                clienteSelecionadoId = cliente.Id;
                clienteSelecionadoDisplay = $"{cliente.CNPJ} ({cliente.Email})"; // Fixed 'c' to 'cliente'
                ClienteComboBox.Text = clienteSelecionadoDisplay;
            }
            else
            {
                ClienteComboBox.Text = string.Empty;
                ClienteComboBox.SelectedItem = null;
                clienteSelecionado = null;
            }

            ValidarMovimentacao();
        }
        private void ClienteComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClienteComboBox.SelectedItem is string selected)
            {
                clienteSelecionado = selected;
                ValidarMovimentacao();

  
  
            }
            else if (!string.IsNullOrWhiteSpace(inputText))
            {
                clienteSelecionadoDisplay = inputText;
                clienteSelecionadoId = null;
            }
            else
            {

                ClienteComboBox.Text = string.Empty;
                ClienteComboBox.SelectedItem = null;
                clienteSelecionadoDisplay = null;
                clienteSelecionadoId = null;

            }
        }
        private void FormaPagamentoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FormaPagamentoComboBox.SelectedItem is ComboBoxItem selected)
            {
                formaPagamentoSelecionada = selected.Content?.ToString();

                if (formaPagamentoSelecionada == "À vista")
                {
                    ParcelasTextBox.Text = "1";
                    ParcelasTextBox.IsEnabled = false;
                }
                else
                {
                    ParcelasTextBox.Text = "";
                    ParcelasTextBox.IsEnabled = true;
                }

            }
            ValidarMovimentacao();
        }
        private void ClienteComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (ClienteComboBox.SelectedItem is string selectedDisplay) { var cliente = clientes.FirstOrDefault(c => $"{c.CNPJ} ({c.Email})" == selectedDisplay); if (cliente != null) { clienteSelecionadoId = cliente.Id; clienteSelecionadoDisplay = selectedDisplay; } } ValidarMovimentacao(); }

        private void FormaPagamentoComboBox_LostFocus(object sender, RoutedEventArgs e) { string inputText = FormaPagamentoComboBox.Text; var match = opcoesFormaPagamento.FirstOrDefault(o => o.Equals(inputText, StringComparison.OrdinalIgnoreCase)); if (match != null) { FormaPagamentoComboBox.SelectedItem = FormaPagamentoComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (i.Content?.ToString() ?? "") == match); formaPagamentoSelecionada = match; } else { FormaPagamentoComboBox.Text = string.Empty; FormaPagamentoComboBox.SelectedItem = null; formaPagamentoSelecionada = null; } ValidarFinanceiro(); }
        private void FormaPagamentoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (FormaPagamentoComboBox.SelectedItem is ComboBoxItem selected) { formaPagamentoSelecionada = selected.Content?.ToString(); if (FormaPagamentoComboBox.SelectedItem is ComboBoxItem selectedItem && (selectedItem.Content?.ToString() ?? "") == "À vista") { ParcelasTextBox.Text = "1"; ParcelasTextBox.IsEnabled = false; AdicionarBoletoButton.Visibility = Visibility.Collapsed; BoletosItemsControl.Visibility = Visibility.Collapsed; } else { ParcelasTextBox.Text = ""; ParcelasTextBox.IsEnabled = true; AdicionarBoletoButton.Visibility = Visibility.Visible; BoletosItemsControl.Visibility = Visibility.Visible; } } ValidarFinanceiro(); }
        private void AdicionarBoletoButton_Click(object sender, RoutedEventArgs e) { int proximaParcela = boletos.Count + 1; int totalParcelas = 1; if (!string.IsNullOrWhiteSpace(ParcelasTextBox.Text) && int.TryParse(ParcelasTextBox.Text, out int parsedParcelas) && parsedParcelas > 0) { totalParcelas = parsedParcelas; } else if (formaPagamentoSelecionada == "À vista") { totalParcelas = 1; } if (proximaParcela > totalParcelas && boletos.Any()) { MessageBox.Show("Todas as parcelas já foram adicionadas para o número de parcelas informado.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            // ✅ POR:
            var novoBoleto = new BoletoData
            {

                DataVencimento = DateTime.Now.AddMonths(proximaParcela - 1),
                CaminhoArquivo = string.Empty,
                LinhaDigitavel = "",
                Beneficiario = fornecedorSelecionadoNome ?? "",
                Pagador = "A definir",
                Valor = 0,
                Status = StatusBoleto.Pendente,
                DataCadastro = DateTime.UtcNow,
                UsuarioCadastro = MainWindow.UsuarioLogado?.Nome,
                Observacoes = $"Parcela {proximaParcela} - Adicionado manualmente"
            }; ; boletos.Add(novoBoleto); }
        private void RemoverBoletoButton_Click(object sender, RoutedEventArgs e) { if (sender is Button btn && btn.DataContext is BoletoData boletoParaRemover) { boletos.Remove(boletoParaRemover); for (int i = 0; i < boletos.Count; i++) { boletos[i].Parcela = i + 1; } BoletosItemsControl.Items.Refresh(); } }
        private void SelecionarBoletoButton_Click(object sender, RoutedEventArgs e) { if (sender is Button btn && btn.DataContext is BoletoData boleto) { var dialog = new OpenFileDialog { Title = "Selecione o arquivo do boleto", Filter = "Arquivos PDF (*.pdf)|*.pdf|Imagens (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Todos os arquivos (*.*)|*.*", InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), RestoreDirectory = true }; if (dialog.ShowDialog() == true) { boleto.CaminhoArquivo = dialog.FileName; } } }
        private bool AtualizarCamposProduto(ProdutoData produto) { if (produto == null) { MessageBox.Show("Produto inválido.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error); return false; } TipoAntesDadoTextBlock.Text = produto.Tipo; MarcaAntesDadoTextBlock.Text = produto.Marca; CodigoAntesDadoTextBlock.Text = produto.Codigo; QuantidadeAntesDadoTextBlock.Text = produto.Quantidade.ToString(); PrecoAntesDadoTextBlock.Text = produto.Preco.ToString("C", CultureInfo.GetCultureInfo("pt-BR")); TipoDepoisDadoTextBlock.Text = produto.Tipo; MarcaDepoisDadoTextBlock.Text = produto.Marca; CodigoDepoisDadoTextBlock.Text = produto.Codigo; if (string.IsNullOrEmpty(QuantidadeTextBox.Text) || string.IsNullOrEmpty(PrecoTextBox.Text)) { QuantidadeDepoisDadoTextBlock.Text = produto.Quantidade.ToString(); PrecoDepoisDadoTextBlock.Text = produto.Preco.ToString("C", CultureInfo.GetCultureInfo("pt-BR")); } else { if (int.TryParse(QuantidadeTextBox.Text, out int quantidadeAlterada) && double.TryParse(PrecoTextBox.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double precoAlterado)) { int quantidadeFinal = usePositiveNumber ? produto.Quantidade + quantidadeAlterada : produto.Quantidade - quantidadeAlterada; if (!usePositiveNumber && quantidadeFinal < 0) { MessageBox.Show("Quantidade insuficiente no estoque.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error); Invalida(); return false; } QuantidadeDepoisDadoTextBlock.Text = quantidadeFinal.ToString(); if (usePositiveNumber) { double precoTotal = (produto.Preco * produto.Quantidade) + (precoAlterado * quantidadeAlterada); int quantidadeTotal = produto.Quantidade + quantidadeAlterada; double precoPonderado = (quantidadeTotal > 0) ? precoTotal / quantidadeTotal : 0; PrecoDepoisDadoTextBlock.Text = precoPonderado.ToString("C", CultureInfo.GetCultureInfo("pt-BR")); } else { PrecoDepoisDadoTextBlock.Text = produto.Preco.ToString("C", CultureInfo.GetCultureInfo("pt-BR")); } } else { QuantidadeDepoisDadoTextBlock.Text = produto.Quantidade.ToString(); PrecoDepoisDadoTextBlock.Text = produto.Preco.ToString("C", CultureInfo.GetCultureInfo("pt-BR")); } } ProdutoAntesDepois.Visibility = Visibility.Visible; return true; }
        private void DestacarMudancas() { TipoDepoisDadoTextBlock.Foreground = TipoDepoisDadoTextBlock.Text != TipoAntesDadoTextBlock.Text ? (Brush)FindResource("AccentBrush") : (Brush)FindResource("TextBrush"); MarcaDepoisDadoTextBlock.Foreground = MarcaDepoisDadoTextBlock.Text != MarcaAntesDadoTextBlock.Text ? (Brush)FindResource("AccentBrush") : (Brush)FindResource("TextBrush"); CodigoDepoisDadoTextBlock.Foreground = CodigoDepoisDadoTextBlock.Text != CodigoAntesDadoTextBlock.Text ? (Brush)FindResource("AccentBrush") : (Brush)FindResource("TextBrush"); QuantidadeDepoisDadoTextBlock.Foreground = int.TryParse(QuantidadeDepoisDadoTextBlock.Text, out int qtdDepois) && int.TryParse(QuantidadeAntesDadoTextBlock.Text, out int qtdAntes) ? qtdDepois > qtdAntes ? (Brush)FindResource("AccentBrush") : qtdDepois < qtdAntes ? (Brush)FindResource("CancelButtonHoverBrush") : (Brush)FindResource("TextBrush") : (Brush)FindResource("TextBrush"); PrecoDepoisDadoTextBlock.Foreground = double.TryParse(PrecoDepoisDadoTextBlock.Text.Replace("R$", "").Trim().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double precoDepois) && double.TryParse(PrecoAntesDadoTextBlock.Text.Replace("R$", "").Trim().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double precoAntes) ? precoDepois > precoAntes ? (Brush)FindResource("AccentBrush") : precoDepois < precoAntes ? (Brush)FindResource("CancelButtonHoverBrush") : (Brush)FindResource("TextBrush") : (Brush)FindResource("TextBrush"); }
        private void ToggleLista_Click(object sender, RoutedEventArgs e) { Lista.Visibility = Lista.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; ToggleLista.Visibility = ToggleLista.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; }

                Parcela = proximaParcela,
                Vencimento = DateTime.Now.AddMonths(proximaParcela - 1),
                CaminhoArquivo = string.Empty
            };

            boletos.Add(novoBoleto);
        }

        // Remove boleto da lista
        private void RemoverBoletoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int parcela)
            {
                var boleto = boletos.FirstOrDefault(b => b.Parcela == parcela);
                if (boleto != null)
                {
                    boletos.Remove(boleto);
                    // Atualiza o ItemsControl
                    BoletosItemsControl.Items.Refresh();
                }
            }
        }
        private void SelecionarBoletoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int parcela)
            {
                var boleto = boletos.FirstOrDefault(b => b.Parcela == parcela);
                if (boleto != null)
                {
                    var dialog = new OpenFileDialog
                    {
                        Title = "Selecione o arquivo do boleto",
                        Filter = "Arquivos PDF (*.pdf)|*.pdf|Todos os arquivos (*.*)|*.*",
                        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        RestoreDirectory = true
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        boleto.CaminhoArquivo = dialog.FileName;
                        // Atualiza o ItemsControl
                        BoletosItemsControl.Items.Refresh();
                    }
                }
            }
        }


        // Método chamado quando um produto é selecionado, altera as informações apresentadas na tela
        private bool AtualizarCamposProduto(ProdutoData produto)
        {
            if (produto == null)
            {
                MessageBox.Show("Produto inválido.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Atualizar os campos "Antes" com os dados do produto
            TipoAntesDadoTextBlock.Text = produto.Tipo;
            MarcaAntesDadoTextBlock.Text = produto.Marca;
            CodigoAntesDadoTextBlock.Text = produto.Codigo;
            QuantidadeAntesDadoTextBlock.Text = produto.Quantidade.ToString();
            PrecoAntesDadoTextBlock.Text = produto.Preco.ToString("C");

            // Atualizar os campos "Depois" com os mesmos valores inicialmente
            TipoDepoisDadoTextBlock.Text = produto.Tipo;
            MarcaDepoisDadoTextBlock.Text = produto.Marca;
            CodigoDepoisDadoTextBlock.Text = produto.Codigo;

            // Validar os campos de entrada (Quantidade e Preço)
            if (string.IsNullOrEmpty(QuantidadeTextBox.Text) || string.IsNullOrEmpty(PrecoTextBox.Text))
            {
                // Se os campos estiverem vazios, apenas inicializa os valores "Depois" com os valores "Antes"
                QuantidadeDepoisDadoTextBlock.Text = produto.Quantidade.ToString();
                PrecoDepoisDadoTextBlock.Text = produto.Preco.ToString("C");
            }
            else
            {
                // Realizar cálculos com os valores inseridos
                if (int.TryParse(QuantidadeTextBox.Text, out int quantidadeAlterada) && double.TryParse(PrecoTextBox.Text, out double precoAlterado))
                {
                    int quantidadeFinal = usePositiveNumber
                        ? produto.Quantidade + quantidadeAlterada // Entrada
                        : produto.Quantidade - quantidadeAlterada; // Saída

                    if (quantidadeFinal < 0)
                    {
                        Alerta.AdicionarAlerta("Erro",
                                               "Quantidade insuficiente",
                                               "Erro ao registrar movimentação de produtos. Possíveis motivos:\n" +
                                               "- Quantidade insuficiente no estoque.",
                                               "- Verifique a quantidade disponível no estoque.\n" +
                                               "- Verifique se a quantidade inserida é válida.\n" +
                                               "- Atualize a quantidade de produtos no estoque.");
                        return false;
                    }

                    QuantidadeDepoisDadoTextBlock.Text = quantidadeFinal.ToString();

                    if (usePositiveNumber)
                    {
                        double precoAtual = produto.Preco;
                        int quantidadeAtual = produto.Quantidade;
                        int quantidadeNova = quantidadeAlterada;
                        int quantidadeTotal = quantidadeAtual + quantidadeNova;

                        double precoPonderado = ((precoAtual * quantidadeAtual) + (precoAlterado * quantidadeNova)) / quantidadeTotal;
                        PrecoDepoisDadoTextBlock.Text = precoPonderado.ToString("C");
                    }
                    else
                    {
                        PrecoDepoisDadoTextBlock.Text = produto.Preco.ToString("C");
                    }
                }
                else
                {
                    // Se não conseguir converter, apenas mostra os valores atuais
                    QuantidadeDepoisDadoTextBlock.Text = produto.Quantidade.ToString();
                    PrecoDepoisDadoTextBlock.Text = produto.Preco.ToString("C");
                }
            }

            // Sempre mostrar o painel de detalhes
            ProdutoAntesDepois.Visibility = Visibility.Visible;

            return true;
        }
        private void DestacarMudancas()
        {
            // Comparar e destacar mudanças
            TipoDepoisDadoTextBlock.Foreground = TipoDepoisDadoTextBlock.Text != TipoAntesDadoTextBlock.Text
                ? (Brush)FindResource("AccentBrush")
                : (Brush)FindResource("TextBrush");

            MarcaDepoisDadoTextBlock.Foreground = MarcaDepoisDadoTextBlock.Text != MarcaAntesDadoTextBlock.Text
                ? (Brush)FindResource("AccentBrush")
                : (Brush)FindResource("TextBrush");

            CodigoDepoisDadoTextBlock.Foreground = CodigoDepoisDadoTextBlock.Text != CodigoAntesDadoTextBlock.Text
                ? (Brush)FindResource("AccentBrush")
                : (Brush)FindResource("TextBrush");

            QuantidadeDepoisDadoTextBlock.Foreground =
                int.TryParse(QuantidadeDepoisDadoTextBlock.Text, out int qtdDepois) &&
                int.TryParse(QuantidadeAntesDadoTextBlock.Text, out int qtdAntes)
                    ? qtdDepois > qtdAntes
                        ? (Brush)FindResource("AccentBrush")
                        : qtdDepois < qtdAntes
                            ? (Brush)FindResource("CancelButtonHoverBrush")
                            : (Brush)FindResource("TextBrush")
                    : (Brush)FindResource("TextBrush");

            PrecoDepoisDadoTextBlock.Foreground =
                double.TryParse(PrecoDepoisDadoTextBlock.Text.Replace("R$", "").Trim(), out double precoDepois) &&
                double.TryParse(PrecoAntesDadoTextBlock.Text.Replace("R$", "").Trim(), out double precoAntes)
                    ? precoDepois > precoAntes
                        ? (Brush)FindResource("AccentBrush")
                        : precoDepois < precoAntes
                            ? (Brush)FindResource("CancelButtonHoverBrush")
                            : (Brush)FindResource("TextBrush")
                    : (Brush)FindResource("TextBrush");
        }

        // Métodos para Lista
        private void ToggleLista_Click(object sender, RoutedEventArgs e)
        {
            // Deixa lista visível
            Lista.Visibility = Lista.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

            // Oculta botão
            ToggleLista.Visibility = ToggleLista.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void AdicionarNaLista_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarMovimentacao() || !ValidarFinanceiro())
            {
                MessageBox.Show("Preencha todos os campos corretamente antes de adicionar à lista.");
                return;
            }

            numeroNotaFiscalAtual = NotaFiscalTextBox.Text.Trim();
            int quantidade = int.TryParse(QuantidadeTextBox.Text, out var qtd) ? qtd : 0;
            double preco = double.TryParse(PrecoTextBox.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var prc) ? prc : 0;
            int parcelas = int.TryParse(ParcelasTextBox.Text, out var parc) ? parc : 1;

            if (produtoSelecionado == null)
            {
                MessageBox.Show("Selecione um produto válido.");
                return;
            }

            var movimentacao = CriarMovimentacaoData(produtoSelecionado, quantidade, preco, DetalhesTextBox.Text);
            movimentacoes.Add(movimentacao);

            MovimentacaoListItem listItem;
            if (usePositiveNumber)
            {
                listItem = CriarMovimentacaoListItem(produtoSelecionado, quantidade, preco, parcelas, DetalhesTextBox.Text, movimentacao);
                var compra = CriarCompraData(produtoSelecionado, quantidade, preco, parcelas, DetalhesTextBox.Text, movimentacao);

                if (boletos.Any())
                {
                    compra.Boletos = new List<string>();

                    foreach (var boletoData in boletos)
                    {
                        if (string.IsNullOrEmpty(boletoData.FornecedorId) && !string.IsNullOrEmpty(fornecedorSelecionadoId))
                        {
                            boletoData.FornecedorId = fornecedorSelecionadoId;
                        }
                        boletoData.NotaFiscal = numeroNotaFiscalAtual;
                        boletoData.Id = int.Parse(DateTime.Now.ToString("MMddHHmm")) + boletoData.Parcela; // ✅ CORRIGIDO
                        compra.Boletos.Add(boletoData.Id.ToString()); // ✅ CONVERTIDO PARA STRING


                    var fornecedor = fornecedores.FirstOrDefault(f => f.Nome == fornecedorSelecionado);
                    if (fornecedor == null)
                    {
                        MessageBox.Show("Fornecedor não encontrado.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Criar uma cópia dos boletos atuais antes de iterar
                    var boletosCopia = BoletosItemsControl.Items.Cast<BoletoData>().ToList();

                    // Limpar a lista de boletos atual
                    boletos.Clear();

                    // Iterar sobre a cópia em vez do controle original
                    foreach (var boleto in boletosCopia)
                    {
                        // Usa o método CriarBoletoData para criar um novo boleto com os dados corretos
                        var novoBoleto = CriarBoletoData(boleto, numeroNotaFiscalAtual, fornecedor);

                        // Adiciona o novo boleto à lista de boletos
                        boletos.Add(novoBoleto);

                        // Gera o nome do boleto no formato BoletoNF{numeroNF}-Parcela{boleto.Parcela}
                        var extensao = Path.GetExtension(boleto.CaminhoArquivo);
                        var nomeBoleto = $"BoletoNF{numeroNotaFiscalAtual}-Parcela{boleto.Parcela}{extensao}";

                        // Adiciona o nome do boleto à lista de boletos da compra
                        compra.Boletos.Add(nomeBoleto);

                    }

                    // Atualiza o controle de UI com a nova lista de boletos
                    BoletosItemsControl.ItemsSource = null;
                    BoletosItemsControl.ItemsSource = boletos;
                }
                compras.Add(compra);
            }
            else
            {
                listItem = CriarMovimentacaoListItem(produtoSelecionado, quantidade, preco, parcelas, DetalhesTextBox.Text, movimentacao);
                var venda = CriarVendaData(produtoSelecionado, quantidade, preco, parcelas, DetalhesTextBox.Text, movimentacao);
                vendas.Add(venda);
            }

            listaMovimentacoes.Add(listItem);
            ListaItemsControl.ItemsSource = null;
            ListaItemsControl.ItemsSource = listaMovimentacoes;
            AnimateToggleLista();
            LimparCampos();
            Invalida();
        }

        private MovimentacaoData CriarMovimentacaoData(ProdutoData produto, int quantidade, double preco, string detalhes) { return new MovimentacaoData { ProdutoId = produto.Id, ProdutoNome = produto.Nome, Tipo = usePositiveNumber ? "Entrada" : "Saída", Preco = preco, Quantidade = quantidade, Data = DateTime.Now, Detalhes = detalhes }; }
        private MovimentacaoListItem CriarMovimentacaoListItem(ProdutoData produto, int quantidade, double preco, int parcelas, string detalhes, MovimentacaoData movimentacao) { return new MovimentacaoListItem { ProdutoId = produto.Id, ProdutoNome = produto.Nome, FornecedorId = usePositiveNumber ? fornecedorSelecionadoNome : null, ClienteId = !usePositiveNumber ? clienteSelecionadoDisplay : null, Quantidade = quantidade, Preco = preco, FormaPagamento = FormaPagamentoComboBox.Text, Parcelas = parcelas, Detalhes = detalhes, Data = DateTime.Now, MovimentacaoData = movimentacao }; }
        private CompraData CriarCompraData(ProdutoData produto, int quantidade, double preco, int parcelas, string detalhes, MovimentacaoData movimentacao) { var compra = new CompraData { FornecedorId = fornecedorSelecionadoId ?? string.Empty, FornecedorNome = fornecedorSelecionadoNome ?? string.Empty, DataCompra = DateTime.Now, DataPagamento = formaPagamentoSelecionada == "À vista" ? DateTime.Now : (DateTime?)null, TipoPagamento = formaPagamentoSelecionada ?? string.Empty, Parcelas = parcelas, NotaFiscal = NotaFiscalTextBox.Text, Itens = new List<MovimentacaoData> { movimentacao }, ValorTotal = (decimal)(preco * quantidade), Detalhes = detalhes }; if (!string.IsNullOrEmpty(compra.NotaFiscal)) compra.SetIdFromNotaFiscal(); else compra.Id = Guid.NewGuid().ToString(); return compra; }
        private VendaData CriarVendaData(ProdutoData produto, int quantidade, double preco, int parcelas, string detalhes, MovimentacaoData movimentacao) { var venda = new VendaData { ClienteId = clienteSelecionadoId ?? string.Empty, ClienteCNPJ = clientes.FirstOrDefault(c => c.Id == clienteSelecionadoId)?.CNPJ ?? string.Empty, Pedido = NotaFiscalTextBox.Text, DataCompra = DateTime.Now, DataPagamento = formaPagamentoSelecionada == "À vista" ? DateTime.Now : (DateTime?)null, TipoPagamento = formaPagamentoSelecionada ?? string.Empty, Parcelas = parcelas, NotaFiscal = NotaFiscalTextBox.Text, Itens = new List<MovimentacaoData> { movimentacao }, ValorTotal = (decimal)(preco * quantidade), DataCadastro = DateTime.Now, Detalhes = detalhes }; if (!string.IsNullOrEmpty(venda.NotaFiscal)) venda.SetIdFromNotaFiscal(); else venda.Id = Guid.NewGuid().ToString(); return venda; }
        private void AnimateToggleLista() { ColorAnimation colorAnimation = new ColorAnimation { From = ((SolidColorBrush)FindResource("PanelBackgroundBrush")).Color, To = ((SolidColorBrush)FindResource("AccentBrush")).Color, Duration = TimeSpan.FromSeconds(0.3), AutoReverse = true, RepeatBehavior = new RepeatBehavior(2) }; SolidColorBrush brush = new SolidColorBrush(((SolidColorBrush)FindResource("PanelBackgroundBrush")).Color); ToggleLista.Background = brush; brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation); }
        private void ExcluirItem_Click(object sender, RoutedEventArgs e) { var button = sender as Button; if (button?.DataContext is MovimentacaoListItem itemToRemove) { var movimentacaoToRemove = itemToRemove.MovimentacaoData; if (movimentacaoToRemove != null) { movimentacoes.Remove(movimentacaoToRemove); } listaMovimentacoes.Remove(itemToRemove); ListaItemsControl.ItemsSource = null; ListaItemsControl.ItemsSource = listaMovimentacoes; } }
        private async void ConfirmarPedido_Click(object sender, RoutedEventArgs e)

        private MovimentacaoData CriarMovimentacaoData(ProdutoData produto, int quantidade, double preco, string detalhes)
        {
            return new MovimentacaoData
            {
                ProdutoId = produto.Nome,
                ProdutoNome = produto.Nome,
                Tipo = usePositiveNumber ? "Entrada" : "Saída",
                Preco = preco,
                Quantidade = quantidade,
                Data = DateTime.Now,
                Detalhes = detalhes
            };
        }
        private MovimentacaoListItem CriarMovimentacaoListItem(ProdutoData produto, int quantidade, double preco, int parcelas, string detalhes, MovimentacaoData movimentacao)
        {
            return new MovimentacaoListItem
            {
                ProdutoId = produto.Nome,
                ProdutoNome = produto.Nome,
                FornecedorId = usePositiveNumber ? FornecedorComboBox.Text : null,
                ClienteId = !usePositiveNumber ? ClienteComboBox.Text : null,
                Quantidade = quantidade,
                Preco = preco,
                FormaPagamento = FormaPagamentoComboBox.Text,
                Parcelas = parcelas,
                Detalhes = detalhes,
                Data = DateTime.Now,
                MovimentacaoData = movimentacao
            };
        }
        private CompraData CriarCompraData(ProdutoData produto, int quantidade, double preco, int parcelas, string detalhes, MovimentacaoData movimentacao)
        {
            var compra = new CompraData
            {
                FornecedorId = fornecedores.FirstOrDefault(f => f.Nome == fornecedorSelecionado)?.Id ?? string.Empty,
                FornecedorNome = fornecedorSelecionado ?? string.Empty,
                DataCompra = DateTime.Now,
                TipoPagamento = formaPagamentoSelecionada ?? string.Empty,
                Parcelas = parcelas,
                NotaFiscal = NotaFiscalTextBox.Text,
                Itens = new List<MovimentacaoData> { movimentacao },
                ValorTotal = (decimal)(preco * quantidade),
                Detalhes = detalhes
            };
            if (!string.IsNullOrEmpty(compra.NotaFiscal))
                compra.SetIdFromNotaFiscal();
            else
                compra.Id = Guid.NewGuid().ToString();
            return compra;
        }
        private BoletoData CriarBoletoData(BoletoData boleto, string numeroNotaFiscal, FornecedorData fornecedor)
        {
            // Criar nome do boleto padronizado
            var extensao = Path.GetExtension(boleto.CaminhoArquivo);
            string nomeBoleto = $"BoletoNF{numeroNotaFiscal}-Parcela{boleto.Parcela}{extensao}";

            var novoBoleto = new BoletoData
            {
                Parcela = boleto.Parcela,
                Vencimento = boleto.Vencimento,
                Pagamento = boleto.Pagamento,
                NomeArquivo = nomeBoleto, // Use o nome formatado
                CaminhoArquivo = boleto.CaminhoArquivo,
                NotaFiscal = numeroNotaFiscal,
                FornecedorId = fornecedor.CNPJ
            };

            // Agora o ID será definido com o nome formatado
            novoBoleto.SetIdFromNome();
            return novoBoleto;
        }
        private VendaData CriarVendaData(ProdutoData produto, int quantidade, double preco, int parcelas, string detalhes, MovimentacaoData movimentacao)

        {
            if (!listaMovimentacoes.Any()) { MessageBox.Show("Adicione pelo menos um item à lista."); return; }
            try
            {

                if (usePositiveNumber)
                {
                    // 👈 ADICIONE ESTA LINHA QUE ESTAVA FALTANDO:
                    foreach (var compra in compras) { RegistrarCompras(compra); }

                ClienteId = cliente?.Id ?? string.Empty,
                ClienteCNPJ = cliente?.CNPJ ?? string.Empty,
                Pedido = NotaFiscalTextBox.Text, // ou outro campo de pedido se houver
                DataCompra = DateTime.Now,
                TipoPagamento = formaPagamentoSelecionada ?? string.Empty,
                Parcelas = parcelas,
                NotaFiscal = NotaFiscalTextBox.Text,
                Itens = new List<MovimentacaoData> { movimentacao },
                ValorTotal = (decimal)(preco * quantidade),
                DataCadastro = DateTime.Now,
                Detalhes = detalhes
            };
            if (!string.IsNullOrEmpty(venda.NotaFiscal))
                venda.SetIdFromNotaFiscal();
            else
                venda.Id = Guid.NewGuid().ToString();
            return venda;
        }


                    foreach (var boletoParaSalvar in boletos)
                    {
                        if (string.IsNullOrEmpty(boletoParaSalvar.NotaFiscal))
                            boletoParaSalvar.NotaFiscal = numeroNotaFiscalAtual;

                        if (string.IsNullOrEmpty(boletoParaSalvar.FornecedorId) && !string.IsNullOrEmpty(fornecedorSelecionadoId))
                            boletoParaSalvar.FornecedorId = fornecedorSelecionadoId;

                        // ✅ ID único e seguro para int
                        boletoParaSalvar.Id = int.Parse(DateTime.Now.ToString("MMddHHmm")) + boletoParaSalvar.Parcela;


                        var boletosCollection = DatabaseConnect.Database.GetCollection<BoletoData>("boletos");
                        boletosCollection.Upsert(boletoParaSalvar);
                    }
                }
                else { foreach (var venda in vendas) { RegistrarVendas(venda); } }
                foreach (var movItem in listaMovimentacoes) { await RegistrarMovimentacaoAsync(movItem.MovimentacaoData); }
                movimentacoes.Clear(); listaMovimentacoes.Clear(); compras.Clear(); vendas.Clear(); boletos.Clear(); ListaItemsControl.ItemsSource = null; this.DialogResult = true; this.Close();
            }
            catch (Exception ex) { MessageBox.Show($"Erro ao registrar {(usePositiveNumber ? "compra" : "venda")}: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
        private void RegistrarCompras(CompraData compra) { try { if (compra == null) { MessageBox.Show("Compra inválida.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning); return; } if (DatabaseConnect.Database == null) return; var comprasCollection = DatabaseConnect.Database.GetCollection<CompraData>("compras"); comprasCollection.Insert(compra); if (!string.IsNullOrEmpty(compra.FornecedorId)) { var fornecedoresCollection = DatabaseConnect.Database.GetCollection<FornecedorData>("fornecedores"); var fornecedor = fornecedoresCollection.FindById(compra.FornecedorId); if (fornecedor != null) { if (fornecedor.ComprasRelacionadas == null) fornecedor.ComprasRelacionadas = new List<string>(); fornecedor.ComprasRelacionadas.Add(compra.Id); fornecedoresCollection.Update(fornecedor); } } } catch (Exception ex) { MessageBox.Show($"Erro ao registrar compra: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error); } }
        private void RegistrarVendas(VendaData venda) { try { if (venda == null) { MessageBox.Show("Venda inválida.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning); return; } if (DatabaseConnect.Database == null) return; var vendasCollection = DatabaseConnect.Database.GetCollection<VendaData>("vendas"); vendasCollection.Insert(venda); if (!string.IsNullOrEmpty(venda.ClienteId)) { var clientesCollection = DatabaseConnect.Database.GetCollection<ClienteData>("clientes"); var cliente = clientesCollection.FindById(venda.ClienteId); if (cliente != null) { if (cliente.VendasRelacionadas == null) cliente.VendasRelacionadas = new List<string>(); cliente.VendasRelacionadas.Add(venda.Id); clientesCollection.Update(cliente); } } } catch (Exception ex) { MessageBox.Show($"Erro ao registrar venda: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error); throw; } }
        private async Task RegistrarMovimentacaoAsync(MovimentacaoData movimentacao) { try { if (movimentacao == null) { MessageBox.Show("Movimentação inválida.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning); return; } if (DatabaseConnect.Database == null) return; var collection = DatabaseConnect.Database.GetCollection<MovimentacaoData>("movimentacoes"); collection.Insert(movimentacao); var produto = produtos.FirstOrDefault(p => p.Id == movimentacao.ProdutoId); if (produto != null) { AtualizarProdutoNoBanco(produto, movimentacao.Tipo == "Entrada", movimentacao.Quantidade, movimentacao.Preco); } } catch (Exception ex) { MessageBox.Show($"Erro ao registrar movimentação: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error); } }
        private void AtualizarProdutoNoBanco(ProdutoData produto, bool isEntrada, int quantidade, double preco) { if (produto == null) return; if (isEntrada) { double precoTotal = (produto.Preco * produto.Quantidade) + (preco * quantidade); int novaQuantidade = produto.Quantidade + quantidade; produto.Preco = novaQuantidade > 0 ? precoTotal / novaQuantidade : 0; produto.Quantidade = novaQuantidade; } else { produto.Quantidade -= quantidade; if (produto.Quantidade < 0) produto.Quantidade = 0; } var produtoCollection = DatabaseConnect.Database.GetCollection<ProdutoData>("produtos"); produtoCollection.Update(produto); }
        private void LimparCampos() { ProdutoComboBox.SelectedItem = null; ProdutoComboBox.Text = string.Empty; produtoSelecionado = null; if (usePositiveNumber) { LimparComboBox(FornecedorComboBox, out fornecedorSelecionadoNome); fornecedorSelecionadoId = null; } else { LimparComboBox(ClienteComboBox, out clienteSelecionadoDisplay); clienteSelecionadoId = null; } LimparTextBox(QuantidadeTextBox, PrecoTextBox, ParcelasTextBox, DetalhesTextBox, NotaFiscalTextBox); FormaPagamentoComboBox.SelectedIndex = -1; formaPagamentoSelecionada = null; ParcelasTextBox.IsEnabled = true; ParcelasTextBox.Text = ""; boletos.Clear(); LimparTextBlock(TipoAntesDadoTextBlock, MarcaAntesDadoTextBlock, CodigoAntesDadoTextBlock, PrecoAntesDadoTextBlock, QuantidadeAntesDadoTextBlock, TipoDepoisDadoTextBlock, MarcaDepoisDadoTextBlock, CodigoDepoisDadoTextBlock, PrecoDepoisDadoTextBlock, QuantidadeDepoisDadoTextBlock); ProdutoAntesDepois.Visibility = Visibility.Collapsed; ProdutoComboBox.Focus(); Invalida(); }
        private void LimparComboBox(ComboBox comboBox, out string? selecionado) { comboBox.SelectedItem = null; comboBox.Text = string.Empty; selecionado = null; }
        private void LimparTextBox(params TextBox[] textBoxes) { foreach (var tb in textBoxes) tb.Clear(); }
        private void LimparTextBlock(params TextBlock[] textBlocks) { foreach (var tb in textBlocks) tb.Text = string.Empty; }
        private void QuantidadeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) { e.Handled = !e.Text.All(char.IsDigit); }
        private void QuantidadeTextBox_Pasting(object sender, DataObjectPastingEventArgs e) { if (e.DataObject.GetDataPresent(typeof(string))) { string text = (string)e.DataObject.GetData(typeof(string)); if (!text.All(char.IsDigit)) e.CancelCommand(); } else { e.CancelCommand(); } }
        private void QuantidadeTextBox_LostFocus(object sender, RoutedEventArgs e) { if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text)) { if (!textBox.Text.All(char.IsDigit)) { textBox.Clear(); return; } if (!usePositiveNumber && produtoSelecionado != null && int.TryParse(produtoSelecionado.Quantidade.ToString(), out int qtdAntes) && int.TryParse(textBox.Text, out int qtdDigitada)) { if (qtdAntes - qtdDigitada < 0) { MessageBox.Show("Falta no estoque.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error); textBox.Clear(); return; } } if (produtoSelecionado != null) { AtualizarCamposProduto(produtoSelecionado); DestacarMudancas(); ValidarMovimentacao(); } } }
        private void QuantidadeTextBox_TextChanged(object sender, TextChangedEventArgs e) { if (produtoSelecionado != null) { if (!usePositiveNumber && produtoSelecionado != null && int.TryParse(produtoSelecionado.Quantidade.ToString(), out int qtdAntes) && int.TryParse(QuantidadeTextBox.Text, out int qtdDigitada)) { if (qtdAntes - qtdDigitada < 0) { /*MessageBox.Show("Falta no estoque.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error); QuantidadeTextBox.Clear(); return; */} } AtualizarCamposProduto(produtoSelecionado); DestacarMudancas(); ValidarMovimentacao(); } }
        private void PrecoTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) { var textBox = (TextBox)sender; string text = textBox.Text.Insert(textBox.CaretIndex, e.Text); e.Handled = !IsValidDecimalInput(text); }
        private void PrecoTextBox_Pasting(object sender, DataObjectPastingEventArgs e) { if (e.DataObject.GetDataPresent(typeof(string))) { string text = (string)e.DataObject.GetData(typeof(string)); if (!IsValidDecimalInput(text)) e.CancelCommand(); } else { e.CancelCommand(); } }
        private void PrecoTextBox_LostFocus(object sender, RoutedEventArgs e) { if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text)) { if (!IsValidDecimalInput(textBox.Text)) textBox.Clear(); else { if (double.TryParse(textBox.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double valor)) textBox.Text = valor.ToString("N2", CultureInfo.GetCultureInfo("pt-BR")); } if (produtoSelecionado != null) { AtualizarCamposProduto(produtoSelecionado); DestacarMudancas(); ValidarMovimentacao(); } } }
        private bool IsValidDecimalInput(string text) { if (string.IsNullOrEmpty(text)) return true; int commaCount = text.Count(c => c == ','); if (commaCount > 1) return false; if (text.StartsWith(",")) return false; return text.All(c => char.IsDigit(c) || c == ','); }
        private void PrecoTextBox_TextChanged(object sender, TextChangedEventArgs e) { if (produtoSelecionado != null) { AtualizarCamposProduto(produtoSelecionado); DestacarMudancas(); ValidarMovimentacao(); } }
        private void ParcelasTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) { if (!e.Text.All(char.IsDigit)) { e.Handled = true; return; } var textBox = sender as TextBox; string novoTexto = textBox != null ? textBox.Text.Insert(textBox.CaretIndex, e.Text) : e.Text; if (int.TryParse(novoTexto, out int valor)) { e.Handled = valor > 12 || valor < 1; } else { e.Handled = true; } }
        private void ParcelasTextBox_Pasting(object sender, DataObjectPastingEventArgs e) { if (e.DataObject.GetDataPresent(typeof(string))) { string text = (string)e.DataObject.GetData(typeof(string)); if (!text.All(char.IsDigit)) e.CancelCommand(); } else { e.CancelCommand(); } }
        private void ParcelasTextBox_LostFocus(object sender, RoutedEventArgs e) { if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text)) { if (!textBox.Text.All(char.IsDigit) || !int.TryParse(textBox.Text, out int val) || val < 1 || val > 12) textBox.Clear(); } }
        private void ParcelasTextBox_TextChanged(object sender, TextChangedEventArgs e) { /* Validação já ocorre em LostFocus e PreviewTextInput */ }
        private void NotaFiscalTextBox_LostFocus(object sender, RoutedEventArgs e) { /* Validação simples, pode ser expandida */ }
        private void NotaFiscalTextBox_TextChanged_1(object sender, TextChangedEventArgs e) { /* Validação simples, pode ser expandida */ }
        private void NotaFiscalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) { /* Permite números e alguns caracteres comuns em NF */ e.Handled = !e.Text.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '/'); }
        private void NotaFiscalTextBox_Pasting(object sender, DataObjectPastingEventArgs e) { if (e.DataObject.GetDataPresent(typeof(string))) { string text = (string)e.DataObject.GetData(typeof(string)); if (!text.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '/')) e.CancelCommand(); } else { e.CancelCommand(); } }
        private bool ValidarMovimentacao() { if (produtoSelecionado == null) { Invalida(); return false; } if (usePositiveNumber && string.IsNullOrEmpty(fornecedorSelecionadoId) && string.IsNullOrEmpty(fornecedorSelecionadoNome)) { Invalida(); return false; } else if (!usePositiveNumber && string.IsNullOrEmpty(clienteSelecionadoId) && string.IsNullOrEmpty(clienteSelecionadoDisplay)) { Invalida(); return false; } if (!int.TryParse(QuantidadeTextBox.Text, out int quantidade) || quantidade <= 0) { Invalida(); return false; } if (!usePositiveNumber && produtoSelecionado.Quantidade < quantidade) { MessageBox.Show("Quantidade insuficiente no estoque.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error); Invalida(); return false; } if (!double.TryParse(PrecoTextBox.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double preco) || preco <= 0) { Invalida(); return false; } Valida(); return true; }
        private bool ValidarFinanceiro() { if (string.IsNullOrEmpty(formaPagamentoSelecionada)) { return false; } if (!int.TryParse(ParcelasTextBox.Text, out int parcelas) || parcelas <= 0 || parcelas > 12) { return false; } if (string.IsNullOrWhiteSpace(NotaFiscalTextBox.Text)) { return false; } return true; }
        private void Valida() { StatusMessage.Text = "Movimentação VÁLIDA!"; StatusMessage.Foreground = (Brush)FindResource("AccentBrush"); Financeiro.Visibility = Visibility.Visible; }
        private void Invalida() { StatusMessage.Text = "Movimentação INVÁLIDA"; StatusMessage.Foreground = (Brush)FindResource("CancelButtonHoverBrush"); Financeiro.Visibility = Visibility.Collapsed; }
        private void ImportarXMLButton_Click(object sender, RoutedEventArgs e)

        private void EditarItem_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is MovimentacaoListItem itemToEdit)
            {
                // Salva referência ao item em edição
                _itemEmEdicao = itemToEdit.MovimentacaoData;

                // Preenche os campos com os dados do item
                PreencherCamposComItem(itemToEdit);

                // Remove da lista de movimentações
                if (_itemEmEdicao != null)
                {
                    movimentacoes.Remove(_itemEmEdicao);
                }

                // Remove da lista de itens
                listaMovimentacoes.Remove(itemToEdit);

                // Remove da lista de compras ou vendas
                if (usePositiveNumber)
                {
                    var compraRelacionada = compras.FirstOrDefault(c => c.Itens.Contains(_itemEmEdicao));
                    if (compraRelacionada != null)
                    {
                        // Preserva a nota fiscal para reutilização
                        NotaFiscalTextBox.Text = compraRelacionada.NotaFiscal;
                        compras.Remove(compraRelacionada);
                    }
                }
                else
                {
                    var vendaRelacionada = vendas.FirstOrDefault(v => v.Itens.Contains(_itemEmEdicao));
                    if (vendaRelacionada != null)
                    {
                        // Preserva a nota fiscal para reutilização
                        NotaFiscalTextBox.Text = vendaRelacionada.NotaFiscal;
                        vendas.Remove(vendaRelacionada);
                    }
                }

                // Atualiza o ItemsSource do ListaItemsControl
                ListaItemsControl.ItemsSource = null;
                ListaItemsControl.ItemsSource = listaMovimentacoes;

                // Esconde a lista para focar na edição
                Lista.Visibility = Visibility.Collapsed;
                ToggleLista.Visibility = Visibility.Visible;

                // Foca no Produto novamente
                ProdutoComboBox.Focus();
            }
        }

        private void PreencherCamposComItem(MovimentacaoListItem item)
        {
            // Preenche o campo de produto
            ProdutoComboBox.Text = item.ProdutoNome;
            produtoSelecionado = produtos.FirstOrDefault(p => p.Nome == item.ProdutoNome);

            // Preenche fornecedor ou cliente dependendo do tipo
            if (usePositiveNumber)
            {
                FornecedorComboBox.Text = item.FornecedorId;
                fornecedorSelecionado = item.FornecedorId;
            }
            else
            {
                ClienteComboBox.Text = item.ClienteId;
                clienteSelecionado = item.ClienteId;
            }

            // Preenche quantidade e preço
            QuantidadeTextBox.Text = item.Quantidade.ToString();
            PrecoTextBox.Text = item.Preco.ToString();

            // Preenche forma de pagamento
            FormaPagamentoComboBox.SelectedItem = FormaPagamentoComboBox.Items
                        .OfType<ComboBoxItem>()
                        .FirstOrDefault(i => (i.Content?.ToString() ?? "") == item.FormaPagamento);
            formaPagamentoSelecionada = item.FormaPagamento;

            // Preenche parcelas
            ParcelasTextBox.Text = item.Parcelas.ToString();

            // Preenche detalhes
            DetalhesTextBox.Text = item.Detalhes;

            // Atualiza os campos de produto
            if (produtoSelecionado != null)
            {
                AtualizarCamposProduto(produtoSelecionado);
                DestacarMudancas();
            }

            // Garante que a seção financeira esteja visível
            Valida();
        }

        private void ExcluirItem_Click(object sender, RoutedEventArgs e)

        {
            var dialog = new OpenFileDialog { Title = "Selecione o arquivo XML da nota fiscal", Filter = "Arquivos XML (*.xml)|*.xml", InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), RestoreDirectory = true }; if (dialog.ShowDialog() == true)
            {
                try
                {
                    var xmlDoc = XDocument.Load(dialog.FileName); XNamespace ns = "http://www.portalfiscal.inf.br/nfe"; var infNFe = xmlDoc.Descendants(ns + "infNFe").FirstOrDefault(); if (infNFe == null) throw new Exception("Estrutura de XML inválida para NF-e."); var nota = new NotaData(); nota.NumeroNota = infNFe.Element(ns + "ide")?.Element(ns + "nNF")?.Value ?? string.Empty; nota.Id = nota.NumeroNota; nota.DataEmissao = DateTime.TryParse(infNFe.Element(ns + "ide")?.Element(ns + "dhEmi")?.Value, out var dataEmissao) ? dataEmissao : DateTime.MinValue; nota.NaturezaOperacao = infNFe.Element(ns + "ide")?.Element(ns + "natOp")?.Value ?? string.Empty; var emit = infNFe.Element(ns + "emit"); nota.EmitenteCNPJ = emit?.Element(ns + "CNPJ")?.Value ?? string.Empty; nota.EmitenteNome = emit?.Element(ns + "xNome")?.Value ?? string.Empty; var enderEmit = emit?.Element(ns + "enderEmit"); nota.EmitenteEndereco = enderEmit?.Element(ns + "xLgr")?.Value + ", " + enderEmit?.Element(ns + "nro")?.Value; nota.EmitenteBairro = enderEmit?.Element(ns + "xBairro")?.Value ?? string.Empty; nota.EmitenteMunicipio = enderEmit?.Element(ns + "xMun")?.Value ?? string.Empty; nota.EmitenteUF = enderEmit?.Element(ns + "UF")?.Value ?? string.Empty; nota.EmitenteCEP = enderEmit?.Element(ns + "CEP")?.Value ?? string.Empty; var dest = infNFe.Element(ns + "dest"); nota.DestinatarioCNPJ = dest?.Element(ns + "CNPJ")?.Value ?? string.Empty; nota.DestinatarioNome = dest?.Element(ns + "xNome")?.Value ?? string.Empty; NotaFiscalTextBox.Text = nota.NumeroNota;
                    if (usePositiveNumber && !string.IsNullOrWhiteSpace(nota.EmitenteNome)) { FornecedorComboBox.Text = nota.EmitenteNome; fornecedorSelecionadoNome = nota.EmitenteNome; var fornecedorExistente = fornecedores.FirstOrDefault(f => f.Nome.Equals(nota.EmitenteNome, StringComparison.OrdinalIgnoreCase)); if (fornecedorExistente != null) fornecedorSelecionadoId = fornecedorExistente.Id; else fornecedorSelecionadoId = null; }
                    MessageBox.Show("Nota fiscal importada com sucesso!\n\n" + $"Número: {nota.NumeroNota}\n" + $"Emissão: {nota.DataEmissao:dd/MM/yyyy}\n" + $"Emitente: {nota.EmitenteNome}", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                catch (Exception ex) { MessageBox.Show($"Erro ao importar arquivo XML:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error); }


                // Remove da lista de items
                listaMovimentacoes.Remove(itemToRemove);

                // Atualiza o ItemsSource do ListaItemsControl
                ListaItemsControl.ItemsSource = null;
                ListaItemsControl.ItemsSource = listaMovimentacoes;
            }
        }
        private async void ConfirmarPedido_Click(object sender, RoutedEventArgs e)
        {
            if (movimentacoes.Count == 0)
            {
                MessageBox.Show("Adicione pelo menos um item à lista.");
                return;
            }

            try
            {
                if (usePositiveNumber)
                {
                    // Registra cada compra
                    foreach (var compra in compras)
                    {
                        RegistrarCompras(compra);
                    }

                    // Organiza os boletos (arquivos físicos)
                    var organizadorBoleto = new OrganizarBoleto(numeroNotaFiscalAtual);
                    foreach (var boleto in boletos)
                    {
                        if (!string.IsNullOrEmpty(boleto.CaminhoArquivo))
                        {
                            organizadorBoleto.Organizar(boleto);
                        }
                    }

                    // Registra os boletos no banco de dados
                    if (boletos.Count > 0)
                    {
                        foreach (var boleto in boletos)
                        {
                            RegistrarBoletos(boleto);
                        }
                    }
                }
                else
                {
                    // Registra cada venda
                    foreach (var venda in vendas)
                    {
                        RegistrarVendas(venda);
                    }
                }

                // Registra cada movimentação individualmente
                foreach (var mov in movimentacoes)
                {
                    await RegistrarMovimentacaoAsync(mov);
                }

                // Limpa a lista e fecha a janela
                movimentacoes.Clear();
                listaMovimentacoes.Clear();
                boletos.Clear();
                ListaItemsControl.ItemsSource = null;
                ListaItemsControl.ItemsSource = listaMovimentacoes;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao registrar {(usePositiveNumber ? "compra" : "venda")}: {ex.Message}",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RegistrarCompras(CompraData compra)
        {
            try
            {
                if (compra == null)
                {
                    MessageBox.Show("Compra inválida.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (DatabaseConnect.Database == null)
                    return;

                // Inserir a compra no banco de dados
                var comprasCollection = DatabaseConnect.Database.GetCollection<CompraData>("compras");
                comprasCollection.Insert(compra);

                // Atualizar o relacionamento com o fornecedor
                if (!string.IsNullOrEmpty(compra.FornecedorId))
                {
                    var fornecedoresCollection = DatabaseConnect.Database.GetCollection<FornecedorData>("fornecedores");
                    var fornecedor = fornecedoresCollection.FindById(compra.FornecedorId);

                    if (fornecedor != null)
                    {
                        // Adicionar o ID da compra à lista de compras relacionadas do fornecedor
                        fornecedor.ComprasRelacionadas.Add(compra.Id);
                        fornecedoresCollection.Update(fornecedor);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao registrar compra: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }
        private void FecharLista_Click(object sender, RoutedEventArgs e) { Lista.Visibility = Visibility.Collapsed; ToggleLista.Visibility = Visibility.Visible; }


    }


    // Classes auxiliares para API Gemini (podem ser movidas para um arquivo de Models separado)
    public class GeminiResponse { [JsonPropertyName("candidates")] public List<Candidate> Candidates { get; set; } }
    public class Candidate { [JsonPropertyName("content")] public Content Content { get; set; } }
    public class Content { [JsonPropertyName("parts")] public List<Part> Parts { get; set; } [JsonPropertyName("role")] public string Role { get; set; } }
    public class Part { [JsonPropertyName("text")] public string Text { get; set; } }
    public class GeminiSchema { [JsonPropertyName("type")] public string Type { get; set; } [JsonPropertyName("properties")] public Dictionary<string, GeminiProperty> Properties { get; set; } }
    public class GeminiProperty { [JsonPropertyName("type")] public string Type { get; set; } [JsonPropertyName("description")] public string Description { get; set; } }
    public class BoletoExtraidoData
    {
        [JsonPropertyName("beneficiario")] public string Beneficiario { get; set; }
        [JsonPropertyName("cnpjBeneficiario")] public string CnpjBeneficiario { get; set; }
        [JsonPropertyName("cepBeneficiario")] public string CepBeneficiario { get; set; }
        [JsonPropertyName("estadoBeneficiario")] public string EstadoBeneficiario { get; set; }
        [JsonPropertyName("pagador")] public string Pagador { get; set; }
        [JsonPropertyName("vencimento")] public string Vencimento { get; set; }
        [JsonPropertyName("valor")] public string Valor { get; set; }
        [JsonPropertyName("linhaDigitavel")] public string LinhaDigitavel { get; set; }
        [JsonPropertyName("nossoNumero")] public string NossoNumero { get; set; }
        [JsonPropertyName("agenciaCodigoBeneficiario")] public string AgenciaCodigoBeneficiario { get; set; }

        private void RegistrarBoletos(BoletoData boleto)
        {
            try
            {
                if (boleto == null)
                {
                    MessageBox.Show("Boleto inválido.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (DatabaseConnect.Database == null)
                    return;

                // Inserir o boleto no banco de dados
                var boletosCollection = DatabaseConnect.Database.GetCollection<BoletoData>("boletos");
                boletosCollection.Insert(boleto);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao registrar boleto: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RegistrarVendas(VendaData venda)
        {
            try
            {
                if (venda == null)
                {
                    MessageBox.Show("Venda inválida.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (DatabaseConnect.Database == null)
                    return;

                // Inserir a venda no banco de dados
                var vendasCollection = DatabaseConnect.Database.GetCollection<VendaData>("vendas");
                vendasCollection.Insert(venda);

                // Atualizar o relacionamento com o cliente
                if (!string.IsNullOrEmpty(venda.ClienteId))
                {
                    var clientesCollection = DatabaseConnect.Database.GetCollection<ClienteData>("clientes");
                    var cliente = clientesCollection.FindById(venda.ClienteId);

                    if (cliente != null)
                    {
                        // Adicionar o ID da venda à lista de vendas relacionadas do cliente
                        cliente.VendasRelacionadas.Add(venda.Id);
                        clientesCollection.Update(cliente);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao registrar venda: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                throw; // Re-throw para ser capturado pelo método chamador
            }
        }

        // Método assíncrono para registrar a movimentação de produtos
        private async Task RegistrarMovimentacaoAsync(MovimentacaoData movimentacao)
        {
            try
            {
                if (movimentacao == null)
                {
                    MessageBox.Show("Movimentação inválida.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DatabaseConnect.Database == null)
                    return;

                var collection = DatabaseConnect.Database.GetCollection<MovimentacaoData>("movimentacoes");
                collection.Insert(movimentacao);

                // Atualiza o produto no banco de dados usando a função dedicada
                var produto = produtos.FirstOrDefault(p => p.Nome == movimentacao.ProdutoId);
                if (produto != null)
                {
                    AtualizarProdutoNoBanco(produto, movimentacao.Tipo == "Entrada", movimentacao.Quantidade, movimentacao.Preco);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao registrar movimentação: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para atualizar o produto no banco de dados
        private void AtualizarProdutoNoBanco(ProdutoData produto, bool isEntrada, int quantidade, double preco)
        {
            if (produto == null) return;

            if (isEntrada)
            {
                // Para compra - calcula preço médio ponderado
                double precoTotal = (produto.Preco * produto.Quantidade) + (preco * quantidade);
                int novaQuantidade = produto.Quantidade + quantidade;
                produto.Preco = novaQuantidade > 0 ? precoTotal / novaQuantidade : 0;
                produto.Quantidade = novaQuantidade;
            }
            else
            {
                // Para venda - apenas reduz a quantidade
                produto.Quantidade -= quantidade;
                if (produto.Quantidade < 0) produto.Quantidade = 0;
            }

            var produtoCollection = DatabaseConnect.Database.GetCollection<ProdutoData>("produtos");
            produtoCollection.Update(produto);
        }


        // Método para limpar os campos de entrada
        private void LimparCampos()
        {
            // Limpar campos de produto
            ProdutoComboBox.SelectedItem = null;
            ProdutoComboBox.Text = string.Empty;
            produtoSelecionado = null;

            // Limpar campos de fornecedor/cliente
            if (usePositiveNumber)
            {
                LimparComboBox(FornecedorComboBox, out fornecedorSelecionado);
            }
            else
            {
                LimparComboBox(ClienteComboBox, out clienteSelecionado);
            }

            // Limpar campos de quantidade, preço, pagamento e detalhes
            LimparTextBox(QuantidadeTextBox, PrecoTextBox, ParcelasTextBox, DetalhesTextBox, NotaFiscalTextBox);

            FormaPagamentoComboBox.SelectedItem = null;
            formaPagamentoSelecionada = null;

            // Limpar campos de exibição
            LimparTextBlock(
                TipoAntesDadoTextBlock, MarcaAntesDadoTextBlock, CodigoAntesDadoTextBlock, PrecoAntesDadoTextBlock, QuantidadeAntesDadoTextBlock,
                TipoDepoisDadoTextBlock, MarcaDepoisDadoTextBlock, CodigoDepoisDadoTextBlock, PrecoDepoisDadoTextBlock, QuantidadeDepoisDadoTextBlock
            );

            // Ocultar detalhes do produto
            ProdutoAntesDepois.Visibility = Visibility.Collapsed;

            // Foca no Produto novamente
            ProdutoComboBox.Focus();
        }
        private void LimparComboBox(ComboBox comboBox, out string? selecionado)
        {
            comboBox.SelectedItem = null;
            comboBox.Text = string.Empty;
            selecionado = null;
        }
        private void LimparTextBox(params TextBox[] textBoxes)
        {
            foreach (var tb in textBoxes)
                tb.Clear();
        }
        private void LimparTextBlock(params TextBlock[] textBlocks)
        {
            foreach (var tb in textBlocks)
                tb.Text = string.Empty;
        }


        // Todos os métodos de validação de entrada de texto
        // Quantidade
        private void QuantidadeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Permite apenas dígitos
            e.Handled = !e.Text.All(char.IsDigit);
        }
        private void QuantidadeTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!text.All(char.IsDigit))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }
        private void QuantidadeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
            {
                if (!textBox.Text.All(char.IsDigit))
                {
                    textBox.Clear();
                    return;
                }

                // Verifica se vai faltar no estoque (apenas para saída)
                if (!usePositiveNumber && int.TryParse(QuantidadeAntesDadoTextBlock.Text, out int qtdAntes) && int.TryParse(textBox.Text, out int qtdDigitada))
                {
                    if (qtdAntes - qtdDigitada < 0)
                    {
                        MessageBox.Show("Falta no estoque.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                        textBox.Clear();
                        return;
                    }
                }

                if (produtoSelecionado != null)
                {
                    AtualizarCamposProduto(produtoSelecionado);
                    DestacarMudancas();
                    ValidarMovimentacao();
                }
            }
        }
        private void QuantidadeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (produtoSelecionado != null)
            {
                // Verifica se vai faltar no estoque (apenas para saída)
                if (!usePositiveNumber && int.TryParse(QuantidadeAntesDadoTextBlock.Text, out int qtdAntes) && int.TryParse(QuantidadeTextBox.Text, out int qtdDigitada))
                {
                    if (qtdAntes - qtdDigitada < 0)
                    {
                        MessageBox.Show("Falta no estoque.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                        QuantidadeTextBox.Clear();
                        return;
                    }
                }

                AtualizarCamposProduto(produtoSelecionado);
                DestacarMudancas();
                ValidarMovimentacao();
            }
        }
        // Preço
        private void PrecoTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Permite apenas dígitos e uma vírgula (para decimal)
            var textBox = (TextBox)sender;
            string text = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            // Só permite uma vírgula e pelo menos um dígito
            e.Handled = !IsValidDecimalInput(text);
        }
        private void PrecoTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsValidDecimalInput(text))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }
        private void PrecoTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
            {
                if (!IsValidDecimalInput(textBox.Text))
                    textBox.Clear();

                if (produtoSelecionado != null)
                {
                    AtualizarCamposProduto(produtoSelecionado);
                    DestacarMudancas();
                    ValidarMovimentacao();
                }
            }
        }
        private bool IsValidDecimalInput(string text)
        {
            // Permite apenas dígitos e no máximo uma vírgula, e não pode começar por vírgula
            if (string.IsNullOrEmpty(text)) return true;
            int commaCount = text.Count(c => c == ',');
            if (commaCount > 1) return false;
            if (text.StartsWith(",")) return false;
            return text.All(c => char.IsDigit(c) || c == ',');
        }
        private void PrecoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (produtoSelecionado != null)
            {
                AtualizarCamposProduto(produtoSelecionado);
                DestacarMudancas();
                ValidarMovimentacao();
            }
        }
        // Parcelas
        private void ParcelasTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Permite apenas dígitos e impede valor maior que 8
            if (!e.Text.All(char.IsDigit))
            {
                e.Handled = true;
                return;
            }

            var textBox = sender as TextBox;
            string novoTexto = textBox != null
                ? textBox.Text.Insert(textBox.SelectionStart, e.Text)
                : e.Text;

            if (int.TryParse(novoTexto, out int valor))
            {
                e.Handled = valor > 8 || valor < 1;
            }
            else
            {
                e.Handled = true;
            }
        }
        private void ParcelasTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!text.All(char.IsDigit))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }
        private void ParcelasTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
            {
                if (!textBox.Text.All(char.IsDigit))
                    textBox.Clear();
            }
        }
        private void ParcelasTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            // Remove handlers para evitar recursão infinita ao alterar o texto
            textBox.TextChanged -= ParcelasTextBox_TextChanged;

            string textoOriginal = textBox.Text;
            if (!string.IsNullOrEmpty(textoOriginal))
            {
                // Remove formatação e espaços
                string textoLimpo = new string(textoOriginal.Where(char.IsDigit).ToArray());

                if (int.TryParse(textoLimpo, out int parcelas))
                {
                    // Limita o valor entre 1 e 8
                    if (parcelas < 1)
                        parcelas = 1;
                    else if (parcelas > 8)
                        parcelas = 8;

                    textBox.Text = parcelas.ToString("N0", new System.Globalization.CultureInfo("pt-BR"));
                    textBox.CaretIndex = textBox.Text.Length;
                }
                else
                {
                    MessageBox.Show("Parcelas inválidas.");
                    textBox.Clear();
                }

                // Verifica a forma de pagamento para alterar o texto
                if (FormaPagamentoComboBox.SelectedItem is ComboBoxItem selected)
                {
                    formaPagamentoSelecionada = selected.Content?.ToString();

                    if (formaPagamentoSelecionada == "À vista")
                    {
                        textBox.Text = "1";
                        textBox.IsEnabled = false;
                    }
                    else if (formaPagamentoSelecionada == "Parcelado")
                    {
                        // Se for parcelado, impede parcelas iguais a 1
                        if (textBox.Text == "1")
                            textBox.Text = "";
                    }

                    if (!textBox.IsEnabled && formaPagamentoSelecionada == "Parcelado")
                    {
                        textBox.IsEnabled = true;
                    }
                }
            }

            // Reanexa o handler
            textBox.TextChanged += ParcelasTextBox_TextChanged;
        }
        // Nota Fiscal - NÃO UTILIZADO ATÉ O MOMENTO
        private void NotaFiscalTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
            {
                // Verifica se o texto é um número válido
                if (!textBox.Text.All(char.IsDigit))
                {
                    MessageBox.Show("Nota fiscal inválida.");
                    textBox.Clear();
                }
            }
        }
        private void NotaFiscalTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
            {
                // Verifica se o texto é um número válido
                if (!textBox.Text.All(char.IsDigit))
                {
                    MessageBox.Show("Nota fiscal inválida.");
                    textBox.Clear();
                }
            }
        }
        private void NotaFiscalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Permite apenas dígitos
            e.Handled = !e.Text.All(char.IsDigit);
        }
        private void NotaFiscalTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!text.All(char.IsDigit))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }


        // Validações
        // Método para validar Movimentação
        private bool ValidarMovimentacao()
        {
            // Se algum campo estiver vazio, retorna false
            if (string.IsNullOrEmpty(ProdutoComboBox.Text) || string.IsNullOrEmpty(QuantidadeTextBox.Text) || string.IsNullOrEmpty(PrecoTextBox.Text))
            {
                Invalida();
                return false;
            }

            // Verifica se já não está válida
            if (StatusMessage.Text == "Movimentação VÁLIDA!")
            {
                return true;
            }

            // Valida produto selecionado
            if (produtoSelecionado == null)
            {
                MessageBox.Show("Produto inválido.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Invalida();
                return false;
            }

            // Valida fornecedor ou cliente selecionado
            if (usePositiveNumber && string.IsNullOrEmpty(FornecedorComboBox.Text))
            {
                MessageBox.Show("Fornecedor inválido.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Invalida();
                return false;
            }
            else if (!usePositiveNumber && string.IsNullOrEmpty(ClienteComboBox.Text))
            {
                MessageBox.Show("Cliente inválido.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Invalida();
                return false;
            }

            // Valida quantidade
            if (!int.TryParse(QuantidadeTextBox.Text, out int quantidade) || quantidade <= 0)
            {
                // Verifica se existe quantidade suficiente no estoque
                if (!usePositiveNumber && produtoSelecionado.Quantidade < quantidade)
                {
                    MessageBox.Show("Quantidade insuficiente no estoque.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    Invalida();
                    return false;
                }
                MessageBox.Show("Quantidade inválida.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Invalida();
                return false;
            }

            // Valida preço
            if (!double.TryParse(PrecoTextBox.Text, out double preco) || preco <= 0)
            {
                MessageBox.Show("Preço inválido.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Invalida();
                return false;
            }

            // Se todas as validações passarem, valida
            Valida();

            return true;
        }
        // Método para validar parte financeira
        private bool ValidarFinanceiro()
        {
            // Se algum campo estiver vazio, retorna false
            if (string.IsNullOrEmpty(FormaPagamentoComboBox.Text) || string.IsNullOrEmpty(ParcelasTextBox.Text) || string.IsNullOrEmpty(NotaFiscalTextBox.Text))
            {
                return false;
            }
            // Valida forma de pagamento
            if (formaPagamentoSelecionada == null)
            {
                MessageBox.Show("Forma de pagamento inválida.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            // Valida parcelas
            if (!int.TryParse(ParcelasTextBox.Text, out int parcelas) || parcelas <= 0)
            {
                MessageBox.Show("Parcelas inválidas.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        private void Valida()
        {
            // Ao validar, altera mensagem status
            StatusMessage.Text = "Movimentação VÁLIDA!";
            StatusMessage.Foreground = (Brush)FindResource("AccentBrush");

            // Altera visibilidade da parte financeira
            Financeiro.Visibility = Visibility.Visible;
        }

        private void Invalida()
        {
            // Ao invalidar, altera mensagem status
            StatusMessage.Text = "Movimentação INVÁLIDA!";
            StatusMessage.Foreground = (Brush)FindResource("CancelButtonHoverBrush");

            // Altera visibilidade da parte financeira
            Financeiro.Visibility = Visibility.Collapsed;
        }

        // Método para validar parte financeira










        // TEMP

        private void ImportarXMLButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Selecione o arquivo XML da nota fiscal",
                Filter = "Arquivos XML (*.xml)|*.xml",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                RestoreDirectory = true
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var xmlDoc = XDocument.Load(dialog.FileName);
                    XNamespace ns = "http://www.portalfiscal.inf.br/nfe";
                    var infNFe = xmlDoc.Descendants(ns + "infNFe").FirstOrDefault();
                    if (infNFe == null)
                        throw new Exception("Estrutura de XML inválida para NF-e.");

                    var nota = new NotaData();

                    // Identificação
                    nota.NumeroNota = infNFe.Element(ns + "ide")?.Element(ns + "nNF")?.Value ?? string.Empty;
                    nota.Id = nota.NumeroNota; // O Id será o número da nota
                    nota.DataEmissao = DateTime.TryParse(infNFe.Element(ns + "ide")?.Element(ns + "dhEmi")?.Value, out var dataEmissao) ? dataEmissao : DateTime.MinValue;
                    nota.NaturezaOperacao = infNFe.Element(ns + "ide")?.Element(ns + "natOp")?.Value ?? string.Empty;

                    // Emitente
                    var emit = infNFe.Element(ns + "emit");
                    nota.EmitenteCNPJ = emit?.Element(ns + "CNPJ")?.Value ?? string.Empty;
                    nota.EmitenteNome = emit?.Element(ns + "xNome")?.Value ?? string.Empty;
                    var enderEmit = emit?.Element(ns + "enderEmit");
                    nota.EmitenteEndereco = enderEmit?.Element(ns + "xLgr")?.Value + ", " + enderEmit?.Element(ns + "nro")?.Value;
                    nota.EmitenteBairro = enderEmit?.Element(ns + "xBairro")?.Value ?? string.Empty;
                    nota.EmitenteMunicipio = enderEmit?.Element(ns + "xMun")?.Value ?? string.Empty;
                    nota.EmitenteUF = enderEmit?.Element(ns + "UF")?.Value ?? string.Empty;
                    nota.EmitenteCEP = enderEmit?.Element(ns + "CEP")?.Value ?? string.Empty;

                    // Destinatário
                    var dest = infNFe.Element(ns + "dest");
                    nota.DestinatarioCNPJ = dest?.Element(ns + "CNPJ")?.Value ?? string.Empty;
                    nota.DestinatarioNome = dest?.Element(ns + "xNome")?.Value ?? string.Empty;

                    //// Salva no banco de dados
                    //var collection = DatabaseConnect.Database.GetCollection<NotaData>("notas");
                    //collection.Upsert(nota);

                    MessageBox.Show(
                        "Nota fiscal importada com sucesso!\n\n" +
                        $"Número: {nota.NumeroNota}\n" +
                        $"Emissão: {nota.DataEmissao:dd/MM/yyyy}\n" +
                        $"Emitente: {nota.EmitenteNome}",
                        "Sucesso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Erro ao importar arquivo XML:\n{ex.Message}",
                        "Erro",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void FecharLista_Click(object sender, RoutedEventArgs e)
        {
            // Deixa lista invisível
            Lista.Visibility = Visibility.Collapsed;

            // Mostra botão
            ToggleLista.Visibility = Visibility.Visible;
        }


    }
}

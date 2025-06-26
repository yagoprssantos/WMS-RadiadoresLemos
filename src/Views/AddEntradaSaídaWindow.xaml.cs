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
using WMS_RadiadoresLemos_WPF.src.Views;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

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
        private string? clienteSelecionadoCNPJ;

        private List<FornecedorData> fornecedores = new List<FornecedorData>();
        private string? fornecedorSelecionadoId;
        private string? fornecedorSelecionadoNome;

        private string? formaPagamentoSelecionada;
        private readonly List<string> opcoesFormaPagamento;

        private ObservableCollection<BoletoData> boletos = new ObservableCollection<BoletoData>();
        private List<BoletoData> todosBoletos = new List<BoletoData>();
        private string? numeroNotaFiscalAtual;

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
            Setup(isEntrada);

            Title = isEntrada ? "Registrar Nova Compra" : "Registrar Nova Venda";

            if (isEntrada)
            {
                // Para entrada

                // Esconde Cliente - Exibe Fornecedor
                Fornecedor.Visibility = Visibility.Visible;
                Cliente.Visibility = Visibility.Collapsed;

                // Exibe Campos Boletos
                CamposBoletos.Visibility = Visibility.Visible;
            }
            else
            {
                // Para saída

                // Esconde Fornecedor - Exibe Cliente
                Fornecedor.Visibility = Visibility.Collapsed;
                Cliente.Visibility = Visibility.Visible;

                // Esconde Campos Boletos
                CamposBoletos.Visibility = Visibility.Collapsed;
            }
        }

        private async void Setup(bool isEntrada)
        {
            produtoSelecionado = null;
            usePositiveNumber = isEntrada;

            await CarregarDados();
            ToggleVisibility(false);
        }

        private async Task CarregarDados()
        {
            await CarregarProdutos();

            // Carrega Fornecedores ou Clientes dependendo do tipo de movimentação
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

                    // Se for venda, apresenta apenas produtos com quantidade > 0
                    if (!usePositiveNumber)
                    {
                        produtos = produtos.Where(p => p.Quantidade > 0).ToList();
                    }

                    // Limpar os itens antes de definir o ItemsSource
                    ProdutoComboBox.ItemsSource = null;
                    ProdutoComboBox.Items.Clear();
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

                    // Limpa os itens antes de definir o ItemsSource
                    FornecedorComboBox.ItemsSource = null;
                    FornecedorComboBox.Items.Clear();
                    FornecedorComboBox.ItemsSource = fornecedores.Select(f => f.Nome).ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar fornecedores: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    
                    // Limpar os itens antes de definir o ItemsSource
                    ClienteComboBox.ItemsSource = null;
                    ClienteComboBox.Items.Clear();
                    ClienteComboBox.ItemsSource = clientes.Select(c => c.CNPJ).ToList();
                }
            }
            catch (Exception ex) { MessageBox.Show($"Erro ao carregar clientes: {ex.Message}", "Erro", MessageBoxButton.OK, System.Windows.MessageBoxImage.Error); }
        }

        private void ToggleVisibility(bool isVisible)
        {
            var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

            // Atualizar visibilidade dos elementos
            ProdutoAntesDepois.Visibility = visibility;

            // Desabilitar ou habilitar o ComboBox
            ProdutoComboBox.IsHitTestVisible = !isVisible;
            ProdutoComboBox.IsEnabled = !isVisible;
        }

        // Método apresentação de dados
        private bool AtualizarCamposProduto(ProdutoData produto)
        {
            if (produto == null)
            {
                MessageBox.Show("Produto inválido.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Atualiza campos "Antes"
            AtualizarCamposAntes(produto);

            // Atualiza campos "Depois"
            AtualizarCamposDepois(produto);

            // Processa alterações se houver valores nos campos de entrada
            if (!string.IsNullOrEmpty(QuantidadeTextBox.Text) && !string.IsNullOrEmpty(PrecoTextBox.Text))
            {
                if (ProcessarAlteracoes(produto))
                {
                    return true;
                }
                return false;
            }

            // Sempre mostrar o painel de detalhes
            ProdutoAntesDepois.Visibility = Visibility.Visible;

            return true;
        }

        private void AtualizarCamposAntes(ProdutoData produto)
        {
            TipoAntesDadoTextBlock.Text = produto.Tipo;
            MarcaAntesDadoTextBlock.Text = produto.Marca;
            CodigoAntesDadoTextBlock.Text = produto.Codigo;
            QuantidadeAntesDadoTextBlock.Text = produto.Quantidade.ToString();
            PrecoAntesDadoTextBlock.Text = produto.Preco.ToString("C2", CultureInfo.GetCultureInfo("pt-BR"));
        }
        private void AtualizarCamposDepois(ProdutoData produto)
        {
            TipoDepoisDadoTextBlock.Text = produto.Tipo;
            MarcaDepoisDadoTextBlock.Text = produto.Marca;
            CodigoDepoisDadoTextBlock.Text = produto.Codigo;

            if (string.IsNullOrEmpty(QuantidadeTextBox.Text) || string.IsNullOrEmpty(PrecoTextBox.Text))
            {
                QuantidadeDepoisDadoTextBlock.Text = produto.Quantidade.ToString();
                PrecoDepoisDadoTextBlock.Text = produto.Preco.ToString("C2", CultureInfo.GetCultureInfo("pt-BR"));
            }
        }

        private bool ProcessarAlteracoes(ProdutoData produto)
        {
            // Se os campos de entrada estiverem vazios, não processa alterações
            if (!TryParseValoresEntrada(out int quantidadeAlterada, out double precoAlterado))
            {
                return false;
            }

            int quantidadeFinal = CalcularQuantidadeFinal(produto.Quantidade, quantidadeAlterada);

            if (!ValidarQuantidade(quantidadeFinal))
            {
                return false;
            }

            RecalcularCamposDepois(produto, quantidadeFinal, precoAlterado);
            return true;
        }

        private bool TryParseValoresEntrada(out int quantidade, out double preco)
        {
            quantidade = 0;
            preco = 0;

            bool quantidadeValida = int.TryParse(QuantidadeTextBox.Text, out quantidade);
            bool precoValido = double.TryParse(
                PrecoTextBox.Text.Replace(",", "."),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out preco
            );

            return quantidadeValida && precoValido;
        }
        private int CalcularQuantidadeFinal(int quantidadeAtual, int alteracao)
        {
            return usePositiveNumber
                ? quantidadeAtual + alteracao  // Entrada
                : quantidadeAtual - alteracao; // Saída
        }
        private bool ValidarQuantidade(int quantidadeFinal)
        {
            if (!usePositiveNumber && quantidadeFinal < 0)
            {
                MessageBox.Show("Quantidade insuficiente no estoque.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        private void RecalcularCamposDepois(ProdutoData produto, int quantidadeFinal, double precoAlterado)
        {
            QuantidadeDepoisDadoTextBlock.Text = quantidadeFinal.ToString();

            if (usePositiveNumber)
            {
                AtualizarPrecoMedioPonderado(produto, quantidadeFinal, precoAlterado);
            }
            else
            {
                PrecoDepoisDadoTextBlock.Text = produto.Preco.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
            }
        }

        private void AtualizarPrecoMedioPonderado(ProdutoData produto, int quantidadeFinal, double precoAlterado)
        {
            double precoTotal = (produto.Preco * produto.Quantidade) + (precoAlterado * (quantidadeFinal - produto.Quantidade));
            double precoPonderado = quantidadeFinal > 0 ? precoTotal / quantidadeFinal : 0;
            PrecoDepoisDadoTextBlock.Text = precoPonderado.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
        }

        private void DestacarMudancas()
        {
            // Destaca mudanças nos campos de texto
            DestacarMudancaCampoTexto(TipoDepoisDadoTextBlock, TipoAntesDadoTextBlock);
            DestacarMudancaCampoTexto(MarcaDepoisDadoTextBlock, MarcaAntesDadoTextBlock);
            DestacarMudancaCampoTexto(CodigoDepoisDadoTextBlock, CodigoAntesDadoTextBlock);

            // Destaca mudanças em campos numéricos
            DestacarMudancaQuantidade();
            DestacarMudancaPreco();
        }
        private void DestacarMudancaCampoTexto(TextBlock campoDepois, TextBlock campoAntes)
        {
            campoDepois.Foreground = campoDepois.Text != campoAntes.Text
                ? (Brush)FindResource("AccentBrush")
                : (Brush)FindResource("TextBrush");
        }
        private void DestacarMudancaQuantidade()
        {
            if (TryParseQuantidades(out int qtdDepois, out int qtdAntes))
            {
                QuantidadeDepoisDadoTextBlock.Foreground = DeterminarCorMudanca(qtdDepois, qtdAntes);
            }
            else
            {
                QuantidadeDepoisDadoTextBlock.Foreground = (Brush)FindResource("TextBrush");
            }
        }
        private void DestacarMudancaPreco()
        {
            if (TryParsePrecos(out double precoDepois, out double precoAntes))
            {
                PrecoDepoisDadoTextBlock.Foreground = DeterminarCorMudanca(precoDepois, precoAntes);
            }
            else
            {
                PrecoDepoisDadoTextBlock.Foreground = (Brush)FindResource("TextBrush");
            }
        }
        private Brush DeterminarCorMudanca<T>(T valorDepois, T valorAntes) where T : IComparable
        {
            int comparacao = valorDepois.CompareTo(valorAntes);
            return comparacao > 0 ? (Brush)FindResource("AccentBrush") :
                   comparacao < 0 ? (Brush)FindResource("CancelButtonHoverBrush") :
                                  (Brush)FindResource("TextBrush");
        }

        private bool TryParseQuantidades(out int qtdDepois, out int qtdAntes)
        {
            qtdDepois = 0; // Ensure qtdDepois is initialized
            qtdAntes = 0;  // Ensure qtdAntes is initialized

            return int.TryParse(QuantidadeDepoisDadoTextBlock.Text, out qtdDepois) &&
                   int.TryParse(QuantidadeAntesDadoTextBlock.Text, out qtdAntes);
        }
        private bool TryParsePrecos(out double precoDepois, out double precoAntes)
        {
            precoDepois = 0.0; // Ensure precoDepois is initialized
            precoAntes = 0.0;  // Ensure precoAntes is initialized

            return double.TryParse(PrecoDepoisDadoTextBlock.Text.Replace("R$", "").Trim().Replace(",", "."),
                                  NumberStyles.Any, CultureInfo.InvariantCulture, out precoDepois) &&
                   double.TryParse(PrecoAntesDadoTextBlock.Text.Replace("R$", "").Trim().Replace(",", "."),
                                  NumberStyles.Any, CultureInfo.InvariantCulture, out precoAntes);
        }

        // Métodos sobre boleto
        private void AdicionarBoletoButton_Click(object sender, RoutedEventArgs e)
        {
            // Validar campos obrigatórios para adicionar boletos
            if (string.IsNullOrEmpty(fornecedorSelecionadoId))
            {
                MessageBox.Show("Selecione um fornecedor antes de adicionar boletos.",
                    "Fornecedor Obrigatório", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Verifica se o número de parcelas foi definido e é válido
            if (string.IsNullOrEmpty(ParcelasTextBox.Text) || !int.TryParse(ParcelasTextBox.Text, out int totalParcelas) || totalParcelas <= 0)
            {
                MessageBox.Show("Defina um número válido de parcelas antes de adicionar boletos.",
                    "Parcelas Inválidas", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Verifica se é possível adicionar mais boletos com base no número de parcelas e na quantidade de boletos já existentes
            if (boletos.Count >= totalParcelas)
            {
                MessageBox.Show("Já foram adicionados boletos suficientes para o número de parcelas definido.",
                    "Limite de Parcelas Atingido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Se a nota fiscal não estiver definida, solicita ao usuário
            if (string.IsNullOrEmpty(NotaFiscalTextBox.Text))
            {
                MessageBox.Show("Informe o número da Nota Fiscal antes de adicionar boletos.",
                    "Nota Fiscal Obrigatória", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Foca no campo de nota fiscal
                NotaFiscalTextBox.Focus();
                return;
            }

            // Atualiza a variável global da nota fiscal
            numeroNotaFiscalAtual = NotaFiscalTextBox.Text.Trim();

            // Determina a próxima parcela
            int proximaParcela = boletos.Count + 1;

            // Verifica se já atingiu o limite de parcelas
            if (proximaParcela > totalParcelas && boletos.Count > 0)
            {
                return;
            }

            // Cria o novo boleto com dados completos
            var novoBoleto = CriarNovoBoleto(proximaParcela);

            // Verifica se o boleto foi criado com sucesso
            if (novoBoleto != null)
            {
                // Adiciona à coleção e atualiza a interface
                boletos.Add(novoBoleto);
                BoletosItemsControl.Items.Refresh();
            }
        }

        private BoletoData CriarNovoBoleto(int numeroParcela)
        {
            // Validações iniciais
            if (string.IsNullOrEmpty(fornecedorSelecionadoId))
            {
                MessageBox.Show(
                    "Selecione um fornecedor antes de adicionar boletos.",
                    "Fornecedor Obrigatório",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            // Obtém o fornecedor selecionado para usar seus dados
            var fornecedor = fornecedores.FirstOrDefault(f => f.Id == fornecedorSelecionadoId);
            if (fornecedor == null)
            {
                MessageBox.Show(
                    "Fornecedor não encontrado no sistema. Verifique a seleção.",
                    "Fornecedor Inválido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            // Atualiza o número da nota fiscal atual
            if (!string.IsNullOrEmpty(NotaFiscalTextBox.Text))
            {
                numeroNotaFiscalAtual = NotaFiscalTextBox.Text.Trim();
            }

            // Calcula a data de vencimento (30 dias por parcela a partir da data atual)
            DateTime dataVencimento = DateTime.Now.AddDays(30 * numeroParcela);

            // Calcula o valor da parcela com base nas movimentações ou total de parcelas
            decimal valorParcela = 0;
            if (int.TryParse(ParcelasTextBox.Text, out int totalParcelas) && totalParcelas > 0)
            {
                // Se já existem produtos na lista, calcula com base neles
                if (listaMovimentacoes.Count > 0)
                {
                    decimal valorTotal = listaMovimentacoes.Sum(item => (decimal)(item.Preco * item.Quantidade));
                    valorParcela = Math.Round(valorTotal / totalParcelas, 2);
                }
                // Se não há produtos, verifica se há um único produto selecionado
                else if (produtoSelecionado != null &&
                         !string.IsNullOrEmpty(QuantidadeTextBox.Text) &&
                         !string.IsNullOrEmpty(PrecoTextBox.Text) &&
                         int.TryParse(QuantidadeTextBox.Text, out int quantidade) &&
                         double.TryParse(PrecoTextBox.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double preco))
                {
                    decimal valorTotal = (decimal)(preco * quantidade);
                    valorParcela = Math.Round(valorTotal / totalParcelas, 2);
                }
            }

            // Cria um novo boleto com todos os dados necessários
            var novoBoleto = new BoletoData
            {
                // Dados de identificação e controle
                Id = string.IsNullOrEmpty(numeroNotaFiscalAtual)
                    ? $"Boleto-Parcela{numeroParcela}"
                    : $"BoletoNF{numeroNotaFiscalAtual}-Parcela{numeroParcela}",
                Parcela = numeroParcela,
                NotaFiscal = numeroNotaFiscalAtual,

                // Dados do arquivo
                CaminhoArquivo = string.Empty,
                NomeArquivo = string.IsNullOrEmpty(numeroNotaFiscalAtual)
                    ? $"Boleto-Parcela{numeroParcela}"
                    : $"BoletoNF{numeroNotaFiscalAtual}-Parcela{numeroParcela}",

                // Dados do fornecedor/beneficiário
                FornecedorId = fornecedorSelecionadoId,
                Beneficiario = fornecedor.Nome,
                CnpjBeneficiario = null,
                EstadoBeneficiario = fornecedor.Estado,

                // Dados financeiros
                DataVencimento = dataVencimento,
                DataPagamento = null,
                Valor = valorParcela,
                Status = StatusBoleto.Pendente,

                // Dados do pagador (Radiadores Lemos)
                Pagador = "Radiadores Lemos",
                CnpjPagador = "38.046.801/0001-60",  // CNPJ padrão da empresa

                // Outros dados
                LinhaDigitavel = string.Empty,
                NossoNumero = string.Empty,
                AgenciaCodigoBeneficiario = string.Empty,

                // Dados de auditoria
                DataCadastro = DateTime.UtcNow,
                UsuarioCadastro = MainWindow.UsuarioLogado?.Nome,
                Observacoes = $"Parcela {numeroParcela} de {(ParcelasTextBox.Text?.Trim() ?? "1")} - Criado em {DateTime.Now:dd/MM/yyyy HH:mm}"
            };

            return novoBoleto;
        }

        private void AtualizarValoresBoletos()
        {
            // Se não houver boletos ou parcelas definidas, não há o que atualizar
            if (boletos.Count == 0 || string.IsNullOrEmpty(ParcelasTextBox.Text) ||
                !int.TryParse(ParcelasTextBox.Text, out int totalParcelas) || totalParcelas <= 0)
                return;

            // Calcula o valor total das movimentações
            decimal valorTotal = 0;

            // Se já temos movimentações na lista, usamos seus valores
            if (listaMovimentacoes.Count > 0)
            {
                valorTotal = listaMovimentacoes.Sum(item => (decimal)(item.Preco * item.Quantidade));
            }
            // Se não, verificamos se há um produto selecionado com quantidade e preço
            else if (produtoSelecionado != null &&
                     !string.IsNullOrEmpty(QuantidadeTextBox.Text) &&
                     !string.IsNullOrEmpty(PrecoTextBox.Text))
            {
                if (int.TryParse(QuantidadeTextBox.Text, out int quantidade) &&
                    double.TryParse(PrecoTextBox.Text.Replace(",", "."),
                                   NumberStyles.Any, CultureInfo.InvariantCulture, out double preco))
                {
                    valorTotal = (decimal)(preco * quantidade);
                }
            }

            // Se não há valor total, não há o que distribuir
            if (valorTotal <= 0)
                return;

            // Calcula o valor por parcela
            decimal valorPorParcela = Math.Round(valorTotal / totalParcelas, 2);

            // Distribui o valor arredondado entre as parcelas
            decimal valorRestante = valorTotal;

            // Atualiza cada boleto na coleção
            for (int i = 0; i < boletos.Count; i++)
            {
                // Para a última parcela, usamos o valor restante para evitar diferenças de arredondamento
                if (i == boletos.Count - 1 && i < totalParcelas - 1)
                {
                    boletos[i].Valor = valorRestante;
                }
                else
                {
                    boletos[i].Valor = valorPorParcela;
                    valorRestante -= valorPorParcela;
                }

                // Atualiza a observação para refletir o número total de parcelas
                boletos[i].Observacoes = $"Parcela {boletos[i].Parcela} de {totalParcelas} - Atualizado em {DateTime.Now:dd/MM/yyyy HH:mm}";

                // Atualiza o nome do arquivo com a nota fiscal atual
                if (!string.IsNullOrEmpty(numeroNotaFiscalAtual))
                {
                    var extensao = Path.GetExtension(boletos[i].CaminhoArquivo);
                    boletos[i].NomeArquivo = $"BoletoNF{numeroNotaFiscalAtual}-Parcela{boletos[i].Parcela}{extensao}";
                    boletos[i].NotaFiscal = numeroNotaFiscalAtual;
                }
            }

            // Atualiza a interface
            BoletosItemsControl.Items.Refresh();
        }

        private void RemoverBoletoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is BoletoData boletoParaRemover)
            {
                // Remove o boleto selecionado da lista
                boletos.Remove(boletoParaRemover);

                // Reordena os números das parcelas
                for (int i = 0; i < boletos.Count; i++)
                {
                    boletos[i].Parcela = i + 1;
                }

                // Atualiza a interface
                BoletosItemsControl.Items.Refresh();
            }
        }

        private void ExtrairDadosBoleto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is BoletoData boleto)
            {
                try
                {
                    BoletoExtractionWindow extractionWindow;

                    // Verifica se o boleto já tem um caminho de arquivo associado
                    if (!string.IsNullOrEmpty(boleto.CaminhoArquivo) && File.Exists(boleto.CaminhoArquivo))
                    {
                        // Se o arquivo existe, passa o caminho para a janela de extração processar automaticamente
                        extractionWindow = new BoletoExtractionWindow(boleto.CaminhoArquivo);
                    }
                    else
                    {
                        // Se não tem arquivo associado, solicita ao usuário selecionar um arquivo
                        OpenFileDialog openFileDialog = new OpenFileDialog
                        {
                            Title = "Selecione o arquivo do boleto",
                            Filter = "Arquivos PDF (*.pdf)|*.pdf|Todos os arquivos (*.*)|*.*",
                            Multiselect = false
                        };

                        if (openFileDialog.ShowDialog() == true)
                        {
                            // Preenche o caminho do arquivo selecionado no boleto
                            boleto.CaminhoArquivo = openFileDialog.FileName;

                            // Cria a janela de extração com o caminho do arquivo selecionado
                            extractionWindow = new BoletoExtractionWindow(openFileDialog.FileName);
                        }
                        else
                        {
                            // Se o usuário cancelar, não faz nada
                            return;
                        }
                    }

                    extractionWindow.Owner = this;
                    bool? result = extractionWindow.ShowDialog();

                    // Se o diálogo retornar true, dados foram extraídos com sucesso
                    if (result == true && extractionWindow.BoletoSalvo != null)
                    {
                        // Copia os dados extraídos para o boleto atual
                        AtualizarBoletoDadosExtraidos(boleto, extractionWindow.BoletoSalvo);

                        // Atualiza a interface
                        BoletosItemsControl.Items.Refresh();

                        // Log para debug - confirmar que o caminho está sendo propagado
                        Console.WriteLine($"Caminho do arquivo após extração: {boleto.CaminhoArquivo}");

                        // Verifica se o CNPJ do pagador é válido após a extração
                        if (!string.IsNullOrWhiteSpace(boleto.CnpjPagador))
                        {
                            string cnpjLimpo = boleto.CnpjPagador.Replace(".", "").Replace("/", "").Replace("-", "");
                            string cnpjEsperado = "38046801000160";

                            if (cnpjLimpo != cnpjEsperado)
                            {
                                MessageBox.Show("Dados do boleto foram aplicados, mas o CNPJ do pagador não corresponde ao esperado.\n\n" +
                                    $"CNPJ encontrado: {boleto.CnpjPagador}\n" +
                                    $"CNPJ esperado: 38.046.801/0001-60\n\n" +
                                    "Por favor, verifique e corrija o CNPJ do pagador antes de salvar.",
                                    "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Dados do boleto foram aplicados, mas o CNPJ do pagador não foi encontrado.\n\n" +
                                "Por favor, preencha o CNPJ do pagador manualmente antes de salvar.",
                                "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao extrair dados do boleto: {ex.Message}",
                        "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private async void AtualizarBoletoDadosExtraidos(BoletoData boletoDestino, BoletoData boletoExtraido)
        {
            try
            {
                // Preserva os campos que não devem ser substituídos
                int parcela = boletoDestino.Parcela;
                string fornecedorId = boletoDestino.FornecedorId;
                string notaFiscal = boletoDestino.NotaFiscal;
                DateTime dataCadastro = boletoDestino.DataCadastro;
                string usuarioCadastro = boletoDestino.UsuarioCadastro;
                
                // Copia todos os campos relevantes do boleto extraído
                boletoDestino.Beneficiario = boletoExtraido.Beneficiario;
                boletoDestino.CnpjBeneficiario = boletoExtraido.CnpjBeneficiario;
                boletoDestino.CepBeneficiario = boletoExtraido.CepBeneficiario;
                boletoDestino.EstadoBeneficiario = boletoExtraido.EstadoBeneficiario;
                boletoDestino.Pagador = boletoExtraido.Pagador;
                boletoDestino.CnpjPagador = boletoExtraido.CnpjPagador;
                boletoDestino.DataVencimento = boletoExtraido.DataVencimento;
                boletoDestino.Valor = boletoExtraido.Valor;
                boletoDestino.LinhaDigitavel = boletoExtraido.LinhaDigitavel;
                boletoDestino.NossoNumero = boletoExtraido.NossoNumero;
                boletoDestino.AgenciaCodigoBeneficiario = boletoExtraido.AgenciaCodigoBeneficiario;
                
                // IMPORTANTE: SEMPRE use o caminho do arquivo extraído
                boletoDestino.CaminhoArquivo = boletoExtraido.CaminhoArquivo;
                
                // Verifica se o fornecedor do boleto é diferente do selecionado
                await VerificarFornecedorBoleto(boletoDestino);
                
                // Restaura os campos que não devem ser modificados (apenas se não foram atualizados pela verificação do fornecedor)
                if (string.IsNullOrEmpty(boletoDestino.FornecedorId))
                    boletoDestino.FornecedorId = fornecedorId;
                    
                boletoDestino.Parcela = parcela;
                boletoDestino.NotaFiscal = notaFiscal;
                boletoDestino.DataCadastro = dataCadastro;
                boletoDestino.UsuarioCadastro = usuarioCadastro;
                
                // Atualiza a observação
                boletoDestino.Observacoes = $"Parcela {parcela} - Dados extraídos automaticamente em {DateTime.Now:dd/MM/yyyy HH:mm}";
                
                // Verificações do CNPJ do pagador (código original mantido)
                if (!string.IsNullOrWhiteSpace(boletoDestino.CnpjPagador))
                {
                    string cnpjLimpo = boletoDestino.CnpjPagador.Replace(".", "").Replace("/", "").Replace("-", "");
                    string cnpjEsperado = "38046801000160";
                    
                    if (cnpjLimpo != cnpjEsperado)
                    {
                        MessageBox.Show(
                            $"CNPJ do pagador inválido no boleto da parcela {boletoDestino.Parcela}!\n\n" +
                            $"CNPJ encontrado: {boletoDestino.CnpjPagador}\n" +
                            $"CNPJ esperado: 38.046.801/0001-60\n\n" +
                            $"Por favor, verifique se o boleto é realmente da empresa Radiadores Lemos.",
                            "CNPJ Inválido",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        
                        boletoDestino.CnpjPagador = "";
                    }
                }
                else
                {
                    MessageBox.Show(
                        $"CNPJ do pagador não foi extraído do boleto da parcela {boletoDestino.Parcela}!\n\n" +
                        $"Por favor, verifique se o arquivo do boleto contém o CNPJ do pagador ou preencha manualmente.",
                        "CNPJ Não Encontrado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                // Atualizar boleto
                AtualizarValoresBoletos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar dados do boleto: {ex.Message}", 
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<bool> VerificarFornecedorBoleto(BoletoData boleto)
        {
            // Se não houver nome do beneficiário no boleto ou fornecedor selecionado, não há o que verificar
            if (string.IsNullOrWhiteSpace(boleto.Beneficiario) || string.IsNullOrWhiteSpace(fornecedorSelecionadoNome))
                return true;

            // Se o fornecedor do boleto for igual ao selecionado, está ok
            if (boleto.Beneficiario.Equals(fornecedorSelecionadoNome, StringComparison.OrdinalIgnoreCase))
                return true;

            // Verificar se o fornecedor já existe no banco de dados
            var fornecedorExistente = fornecedores.FirstOrDefault(f => 
                f.Nome.Equals(boleto.Beneficiario, StringComparison.OrdinalIgnoreCase));

            if (fornecedorExistente != null)
            {
                // O fornecedor já existe no sistema, perguntar se deseja usá-lo
                var resultado = MessageBox.Show(
                    $"O nome do fornecedor no boleto ({boleto.Beneficiario}) é diferente do fornecedor selecionado ({fornecedorSelecionadoNome}).\n\n" +
                    "Este fornecedor já existe no sistema. Deseja usá-lo para este boleto?",
                    "Fornecedor Encontrado",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    // Atualizar fornecedor selecionado
                    fornecedorSelecionadoId = fornecedorExistente.Id;
                    fornecedorSelecionadoNome = fornecedorExistente.Nome;
                    FornecedorComboBox.Text = fornecedorSelecionadoNome;
                    
                    // Atualizar o fornecedor no boleto
                    boleto.FornecedorId = fornecedorSelecionadoId;
                    
                    return true;
                }
                else
                {
                    // Usuário não quis usar o fornecedor existente
                    return false;
                }
            }
            else
            {
                // Fornecedor não existe, perguntar se deseja cadastrar
                var resultado = MessageBox.Show(
                    $"O nome do fornecedor no boleto ({boleto.Beneficiario}) é diferente do fornecedor selecionado ({fornecedorSelecionadoNome}).\n\n" +
                    "Este fornecedor não existe no sistema. Deseja cadastrá-lo?",
                    "Novo Fornecedor",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    // Criar novo fornecedor com os dados do boleto
                    var novoFornecedor = new FornecedorData
                    {
                        Nome = boleto.Beneficiario,
                        CNPJ = boleto.CnpjBeneficiario ?? "", // CNPJ vazio se não disponível
                        Estado = boleto.EstadoBeneficiario ?? "", // Estado vazio se não disponível
                        ComprasRelacionadas = new List<string>(),
                        Id = string.Empty // Será preenchido pelo SetIdFromCNPJ
                    };

                    // Abrir janela de edição do fornecedor
                    var editarFornecedorWindow = new EditarFornecedorWindow(novoFornecedor);
                    bool? result = editarFornecedorWindow.ShowDialog();
                    
                    if (result == true)
                    {
                        // Fornecedor salvo com sucesso
                        await CarregarFornecedores();
                        
                        // Atualizar fornecedor selecionado
                        fornecedorSelecionadoId = editarFornecedorWindow.Fornecedor.Id;
                        fornecedorSelecionadoNome = editarFornecedorWindow.Fornecedor.Nome;
                        FornecedorComboBox.Text = fornecedorSelecionadoNome;
                        
                        // Atualizar o fornecedor no boleto
                        boleto.FornecedorId = fornecedorSelecionadoId;
                        
                        return true;
                    }
                    else
                    {
                        // Usuário cancelou a adição do fornecedor
                        return false;
                    }
                }
                
                // Usuário não quis adicionar o fornecedor
                return false;
            }
        }

        private void AdicionarNaLista_Click(object sender, RoutedEventArgs e)
        {
            // Garante validação correta
            if (!ValidarMovimentacao() || !ValidarFinanceiro())
            {
                MessageBox.Show("Preencha todos os campos corretamente.");
                return;
            }

            numeroNotaFiscalAtual = NotaFiscalTextBox.Text.Trim();
            int quantidade = int.TryParse(QuantidadeTextBox.Text, out var qtd) ? qtd : 0;
            double preco = double.TryParse(PrecoTextBox.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var prc) ? prc : 0;
            int parcelas = int.TryParse(ParcelasTextBox.Text, out var parc) ? parc : 1;

            var movimentacao = CriarMovimentacaoData(produtoSelecionado, quantidade, preco, DetalhesTextBox.Text);
            movimentacoes.Add(movimentacao);

            MovimentacaoListItem listItem = CriarMovimentacaoListItem(produtoSelecionado, quantidade, preco, parcelas, DetalhesTextBox.Text, movimentacao);

            // Para compra
            if (usePositiveNumber)
            {
                var compra = CriarCompraData(produtoSelecionado, quantidade, preco, parcelas, DetalhesTextBox.Text, movimentacao);

                if (boletos.Any())
                {
                    compra.Boletos = new List<string>();
                    var fornecedor = fornecedores.FirstOrDefault(f => f.Id == fornecedorSelecionadoId);

                    // Preserva os boletos existentes e apenas atualiza os campos necessários
                    foreach (var boleto in boletos)
                    {
                        // Atualiza apenas os campos que precisam ser padronizados para a nota fiscal
                        if (string.IsNullOrEmpty(boleto.FornecedorId) && !string.IsNullOrEmpty(fornecedorSelecionadoId))
                        {
                            boleto.FornecedorId = fornecedorSelecionadoId;
                        }

                        // Atualiza a nota fiscal no boleto
                        boleto.NotaFiscal = numeroNotaFiscalAtual;

                        // Atualiza o nome do arquivo baseado na nota fiscal e parcela
                        var extensao = !string.IsNullOrEmpty(boleto.CaminhoArquivo) ?
                            Path.GetExtension(boleto.CaminhoArquivo) : "";
                        boleto.NomeArquivo = $"BoletoNF{numeroNotaFiscalAtual}-Parcela{boleto.Parcela}{extensao}";

                        // Gera um ID apropriado se necessário
                        if (string.IsNullOrEmpty(boleto.Id))
                        {
                            // Define o ID do boleto com base no nome do arquivo ou hora+parcela
                            if (!string.IsNullOrEmpty(boleto.NomeArquivo))
                            {
                                boleto.Id = boleto.NomeArquivo;
                            }
                            else
                            {
                                boleto.Id = (int.Parse(DateTime.Now.ToString("MMddHHmm")) + boleto.Parcela).ToString();
                            }
                        }

                        // Adiciona o ID do boleto à lista de boletos da compra
                        compra.Boletos.Add(boleto.Id);
                        
                        // IMPORTANTE: Adiciona o boleto à coleção permanente
                        todosBoletos.Add(boleto);
                    }

                    compras.Add(compra);
                }
                else
                {
                    compras.Add(compra);
                }
            }
            // Para venda
            else
            {
                var venda = CriarVendaData(produtoSelecionado, quantidade, preco, parcelas, DetalhesTextBox.Text, movimentacao);
                vendas.Add(venda);
            }

            // Adiciona o item à lista de movimentações
            listaMovimentacoes.Add(listItem);
            ListaItemsControl.ItemsSource = null;
            ListaItemsControl.ItemsSource = listaMovimentacoes;
            AnimateToggleLista();

            // Limpa os boletos da interface para a próxima compra
            boletos.Clear();
            BoletosItemsControl.Items.Refresh();

            LimparCampos();

            // Deixa mensagem status invisível para não confundir
            StatusMessage.Visibility = Visibility.Collapsed;

            // Abre a lista de movimentações se estiver oculta
            if (Lista.Visibility == Visibility.Collapsed)
            {
                Lista.Visibility = Visibility.Visible;
            }
        }

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
                FornecedorId = fornecedores.FirstOrDefault(f => f.Id == fornecedorSelecionadoId)?.Id ?? string.Empty,
                FornecedorNome = fornecedorSelecionadoNome ?? string.Empty,
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
                DataVencimento = boleto.DataVencimento,
                DataPagamento = boleto.DataPagamento,
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
            var venda = new VendaData
            {
                ClienteId = clienteSelecionadoId ?? string.Empty,
                ClienteCNPJ = clienteSelecionadoCNPJ ?? string.Empty,
                Pedido = NotaFiscalTextBox.Text,
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

        // Lista Retrátil
        private void ToggleLista_Click(object sender, RoutedEventArgs e)
        {
            // Alterna visibilidade da lista e do botão
            var novaVisibilidade = Lista.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;

            Lista.Visibility = novaVisibilidade;
            ToggleLista.Visibility = novaVisibilidade == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
        private void AnimateToggleLista()
        {
            ColorAnimation colorAnimation = new ColorAnimation
            {
                From = ((SolidColorBrush)FindResource("PanelBackgroundBrush")).Color,
                To = ((SolidColorBrush)FindResource("AccentBrush")).Color,
                Duration = TimeSpan.FromSeconds(0.3),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(2)
            };

            SolidColorBrush brush = new SolidColorBrush(
                ((SolidColorBrush)FindResource("PanelBackgroundBrush")).Color
            );

            ToggleLista.Background = brush;
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        private void ExcluirItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is MovimentacaoListItem itemToRemove)
            {
                var movimentacaoToRemove = itemToRemove.MovimentacaoData;

                if (movimentacaoToRemove != null)
                {
                    movimentacoes.Remove(movimentacaoToRemove);
                }

                listaMovimentacoes.Remove(itemToRemove);

                ListaItemsControl.ItemsSource = null;
                ListaItemsControl.ItemsSource = listaMovimentacoes;
            }
        }

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
                fornecedorSelecionadoId = item.FornecedorId;
            }
            else
            {
                ClienteComboBox.Text = item.ClienteId;
                clienteSelecionadoId = item.ClienteId;
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


        // Dentro de Lista - Confirmar Pedido
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
                    // Usa a coleção todosBoletos em vez de boletos
                    foreach (var boleto in todosBoletos)
                    {
                        if (!string.IsNullOrEmpty(boleto.CaminhoArquivo))
                        {
                            // Usa o método estático OrganizarArquivoBoleto
                            OrganizarBoleto.OrganizarArquivoBoleto(boleto, boleto.NotaFiscal);
                        }
                    }

                    // Registra os boletos no banco de dados
                    // Usa a coleção todosBoletos em vez de boletos
                    if (todosBoletos.Count > 0)
                    {
                        foreach (var boleto in todosBoletos)
                        {
                            await RegistrarBoletosAsync(boleto);
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

                // Limpa todas as coleções
                movimentacoes.Clear();
                listaMovimentacoes.Clear();
                boletos.Clear();
                todosBoletos.Clear();
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
        private async Task RegistrarBoletosAsync(BoletoData boleto)
        {
            try
            {
                if (boleto == null)
                {
                    MessageBox.Show("Boleto inválido.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Verifica se o CNPJ do pagador é exatamente "38.046.801/0001-60"
                if (!string.IsNullOrWhiteSpace(boleto.CnpjPagador))
                {
                    string cnpjLimpo = boleto.CnpjPagador.Replace(".", "").Replace("/", "").Replace("-", "");
                    string cnpjEsperado = "38046801000160";
                    
                    if (cnpjLimpo != cnpjEsperado)
                    {
                        MessageBox.Show(
                            $"CNPJ do pagador inválido no boleto da parcela {boleto.Parcela}!\n\n" +
                            $"CNPJ encontrado: {boleto.CnpjPagador}\n" +
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
                        $"CNPJ do pagador não foi informado no boleto da parcela {boleto.Parcela}!\n\n" +
                        $"Por favor, preencha o CNPJ do pagador para continuar.",
                        "CNPJ Obrigatório",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Verificar fornecedor do boleto
                if (!await VerificarFornecedorBoleto(boleto))
                {
                    MessageBox.Show(
                        "O fornecedor do boleto não foi confirmado. Verifique o fornecedor antes de continuar.",
                        "Fornecedor Não Confirmado",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
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
        private void AtualizarProdutoNoBanco(ProdutoData produto, bool isEntrada, int quantidade, double preco)
        {
            if (produto == null)
                return;

            if (isEntrada)
            {
                // Cálculo do preço médio ponderado para entradas
                double precoTotal = (produto.Preco * produto.Quantidade) + (preco * quantidade);
                int novaQuantidade = produto.Quantidade + quantidade;
                produto.Preco = novaQuantidade > 0 ? precoTotal / novaQuantidade : 0;
                produto.Quantidade = novaQuantidade;
            }
            else
            {
                // Diminuição da quantidade para saídas
                produto.Quantidade -= quantidade;
                if (produto.Quantidade < 0)
                    produto.Quantidade = 0;
            }

            var produtoCollection = DatabaseConnect.Database.GetCollection<ProdutoData>("produtos");
            produtoCollection.Update(produto);
        }


        private void FecharLista_Click(object sender, RoutedEventArgs e) { Lista.Visibility = Visibility.Collapsed; ToggleLista.Visibility = Visibility.Visible; }


        // Limpar campos e resetar estado
        private void LimparCampos()
        {
            // Limpa seleção de produto
            ProdutoComboBox.SelectedItem = null;
            ProdutoComboBox.Text = string.Empty;
            produtoSelecionado = null;

            // Limpa fornecedor ou cliente dependendo do modo
            if (usePositiveNumber)
            {
                LimparComboBox(FornecedorComboBox, out fornecedorSelecionadoNome);
                fornecedorSelecionadoId = null;
            }
            else
            {
                LimparComboBox(ClienteComboBox, out clienteSelecionadoCNPJ);
                clienteSelecionadoId = null;
            }

            // Limpa campos de texto
            LimparTextBox(QuantidadeTextBox, PrecoTextBox, ParcelasTextBox, DetalhesTextBox, NotaFiscalTextBox);

            // Limpa forma de pagamento
            FormaPagamentoComboBox.SelectedIndex = -1;
            formaPagamentoSelecionada = null;

            // Reseta parcelas
            ParcelasTextBox.IsEnabled = true;
            ParcelasTextBox.Text = "";

            // Limpa campos de comparação do produto
            LimparTextBlock(
                TipoAntesDadoTextBlock, MarcaAntesDadoTextBlock, CodigoAntesDadoTextBlock,
                PrecoAntesDadoTextBlock, QuantidadeAntesDadoTextBlock, TipoDepoisDadoTextBlock,
                MarcaDepoisDadoTextBlock, CodigoDepoisDadoTextBlock, PrecoDepoisDadoTextBlock,
                QuantidadeDepoisDadoTextBlock
            );

            // Esconde a seção de comparação
            ProdutoAntesDepois.Visibility = Visibility.Collapsed;

            // Foca no combo de produtos
            ProdutoComboBox.Focus();

            // Define o estado como inválido
            Invalida();
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
            {
                tb.Clear();
            }
        }
        private void LimparTextBlock(params TextBlock[] textBlocks)
        {
            foreach (var tb in textBlocks)
            {
                tb.Text = string.Empty;
            }
        }


        // Métodos de validação de entrada de texto
        // Produto
        private void ProdutoComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Template.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
            {
                string searchText = textBox.Text;
                // 1. Filtra o texto com base no texto digitado
                var filteredProducts = produtos.Where(p => p.Nome.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                                              .Select(p => p.Nome)
                                              .ToList();

                // 2. Reorganiza os itens do ComboBox para exibir primeiro os que começam com o texto digitado
                filteredProducts = filteredProducts.OrderBy(p => p.StartsWith(searchText, StringComparison.OrdinalIgnoreCase) ? 0 : 1).ToList();

                comboBox.ItemsSource = null;
                comboBox.Items.Clear();

                foreach (var nome in filteredProducts)
                {
                    comboBox.Items.Add(nome);
                }

                textBox.Text = searchText;
                textBox.CaretIndex = textBox.Text.Length;
                comboBox.IsDropDownOpen = true;
            }
        }
        private void ProdutoComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            string inputText = ProdutoComboBox.Text;

            if (ProdutoComboBox.SelectedItem is string selectedProductName)
            {
                inputText = selectedProductName;
            }

            if (!string.IsNullOrEmpty(inputText) && produtos.Any(p => p.Nome == inputText))
            {
                produtoSelecionado = produtos.FirstOrDefault(p => p.Nome == inputText);

                if (produtoSelecionado != null)
                {
                    AtualizarCamposProduto(produtoSelecionado);
                    DestacarMudancas();
                    ValidarMovimentacao();
                }
            }
            else
            {
                ProdutoComboBox.Text = string.Empty;
                ProdutoComboBox.SelectedItem = null;
            }
        }
        private void ProdutoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProdutoComboBox.SelectedItem is string selectedProductName)
            {
                produtoSelecionado = produtos.FirstOrDefault(p => p.Nome == selectedProductName);

                if (produtoSelecionado != null)
                {
                    AtualizarCamposProduto(produtoSelecionado);
                    DestacarMudancas();
                    ValidarMovimentacao();
                }
                else
                {
                    MessageBox.Show("Produto não encontrado no cache.");
                }
            }
        }

        // Fornecedor e Cliente
        private void FornecedorComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Template.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
            {
                string searchText = textBox.Text;
                // 1. Filtra o texto com base no texto digitado
                var filteredFornecedores = fornecedores.Where(f => f.Nome.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                                                      .Select(f => f.Nome)
                                                      .ToList();

                // 2. Reorganiza os itens do ComboBox para exibir primeiro os que começam com o texto digitado
                filteredFornecedores = filteredFornecedores.OrderBy(f => f.StartsWith(searchText, StringComparison.OrdinalIgnoreCase) ? 0 : 1).ToList();

                comboBox.ItemsSource = null;
                comboBox.Items.Clear();

                foreach (var nome in filteredFornecedores)
                {
                    comboBox.Items.Add(nome);
                }

                textBox.Text = searchText;
                textBox.CaretIndex = textBox.Text.Length;
                comboBox.IsDropDownOpen = true;
            }
        }
        private void FornecedorComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            string inputText = FornecedorComboBox.Text;

            if (FornecedorComboBox.SelectedItem is string selected)
                inputText = selected;

            var fornecedor = fornecedores.FirstOrDefault(f => f.Nome.Equals(inputText, StringComparison.OrdinalIgnoreCase));

            if (fornecedor != null)
            {
                fornecedorSelecionadoNome = fornecedor.Nome;
                fornecedorSelecionadoId = fornecedor.Id;
                FornecedorComboBox.Text = fornecedor.Nome;
            }
            else if (!string.IsNullOrWhiteSpace(inputText))
            {
                fornecedorSelecionadoNome = inputText;
                fornecedorSelecionadoId = null;
                // TODO: Tratar caso de fornecedor não encontrado - Adicionar Fornecedor
            }
            else
            {
                FornecedorComboBox.Text = string.Empty;
                FornecedorComboBox.SelectedItem = null;
                fornecedorSelecionadoNome = null;
                fornecedorSelecionadoId = null;
            }
        }
        private void FornecedorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FornecedorComboBox.SelectedItem is string selectedName)
            {
                var fornecedor = fornecedores.FirstOrDefault(f => f.Nome == selectedName);

                if (fornecedor != null)
                {
                    fornecedorSelecionadoNome = fornecedor.Nome;
                    fornecedorSelecionadoId = fornecedor.Id;

                    ValidarMovimentacao();
                }
            }
        }
        private void ClienteComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Template.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
            {
                string searchText = textBox.Text;

                if (clientes == null) return;

                var filteredClientes = clientes.Where(clienteLocal =>
                                         (clienteLocal.CNPJ != null && clienteLocal.CNPJ.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                                         (clienteLocal.Email != null && clienteLocal.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
                                       .Select(clienteLocal => $"{clienteLocal.CNPJ} ({clienteLocal.Email})")
                                       .ToList();

                comboBox.ItemsSource = null;
                comboBox.Items.Clear();

                foreach (var nome in filteredClientes)
                {
                    comboBox.Items.Add(nome);
                }

                textBox.Text = searchText;
                textBox.CaretIndex = textBox.Text.Length;
                comboBox.IsDropDownOpen = true;
            }
        }
        private void ClienteComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            string inputText = ClienteComboBox.Text;

            if (ClienteComboBox.SelectedItem is string selected)
                inputText = selected;

            var cliente = clientes.FirstOrDefault(c => c.CNPJ == inputText);
            if (cliente != null)
            {
                clienteSelecionadoId = cliente.Id;
                clienteSelecionadoCNPJ = cliente.CNPJ;
                ClienteComboBox.Text = clienteSelecionadoCNPJ;
            }
            else
            {
                ClienteComboBox.Text = string.Empty;
                ClienteComboBox.SelectedItem = null;
                clienteSelecionadoId = null;
                clienteSelecionadoCNPJ = null;
            }

        }
        private void ClienteComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClienteComboBox.SelectedItem is string selected)
            {
                var cliente = clientes.FirstOrDefault(c => c.CNPJ == selected);
                if (cliente != null)
                {
                    clienteSelecionadoId = cliente.Id;
                    clienteSelecionadoCNPJ = cliente.CNPJ;
                    ValidarMovimentacao();
                }
            }
        }


        // Quantidade
        private void QuantidadeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Permite apenas dígitos numéricos
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
                // Verifica se contém apenas dígitos
                if (!textBox.Text.All(char.IsDigit))
                {
                    textBox.Clear();
                    return;
                }

                // Verifica se há quantidade suficiente no estoque para saídas
                if (!usePositiveNumber && produtoSelecionado != null &&
                    int.TryParse(produtoSelecionado.Quantidade.ToString(), out int qtdAntes) &&
                    int.TryParse(textBox.Text, out int qtdDigitada))
                {
                    if (qtdAntes - qtdDigitada < 0)
                    {
                        MessageBox.Show("Falta no estoque.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                        textBox.Clear();
                        return;
                    }
                }

                // Atualiza os campos do produto se um produto estiver selecionado
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
                if (!usePositiveNumber && produtoSelecionado != null &&
                    int.TryParse(produtoSelecionado.Quantidade.ToString(), out int qtdAntes) &&
                    int.TryParse(QuantidadeTextBox.Text, out int qtdDigitada))
                {
                    if (qtdAntes - qtdDigitada < 0)
                    {
                        QuantidadeTextBox.Clear();
                    }
                }

                AtualizarCamposProduto(produtoSelecionado);
                DestacarMudancas();
                ValidarMovimentacao();
            }
        }

        // Preço
        // TODO: Ver validação por conta do ponto em 1.000,00 (valores acima de mil)
        private void PrecoTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = (TextBox)sender;
            string text = textBox.Text.Insert(textBox.CaretIndex, e.Text);
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
                {
                    textBox.Clear();
                }
                else
                {
                    // Formata o valor como moeda brasileira
                    if (double.TryParse(textBox.Text.Replace(",", "."),
                        NumberStyles.Any, CultureInfo.InvariantCulture, out double valor))
                    {
                        textBox.Text = valor.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"));
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
        private bool IsValidDecimalInput(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            int commaCount = text.Count(c => c == ',');

            // Verifica se há mais de uma vírgula
            if (commaCount > 1)
                return false;

            // Verifica se começa com vírgula
            if (text.StartsWith(","))
                return false;

            // Verifica se contém apenas dígitos e vírgula
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

        // Forma de DataPagamento
        private void FormaPagamentoComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            string inputText = FormaPagamentoComboBox.Text;
            var match = opcoesFormaPagamento.FirstOrDefault(o => o.Equals(inputText, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                FormaPagamentoComboBox.SelectedItem = FormaPagamentoComboBox.Items
                    .OfType<ComboBoxItem>()
                    .FirstOrDefault(i => (i.Content?.ToString() ?? "") == match);
                formaPagamentoSelecionada = match;
            }
            else
            {
                FormaPagamentoComboBox.Text = string.Empty;
                FormaPagamentoComboBox.SelectedItem = null;
                formaPagamentoSelecionada = null;
            }
        }
        private void FormaPagamentoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FormaPagamentoComboBox.SelectedItem is ComboBoxItem selected)
            {
                formaPagamentoSelecionada = selected.Content?.ToString();

                if (FormaPagamentoComboBox.SelectedItem is ComboBoxItem selectedItem &&
                    (selectedItem.Content?.ToString() ?? "") == "À vista")
                {
                    ParcelasTextBox.Text = "1";
                    ParcelasTextBox.IsEnabled = false;
                }
                else
                {
                    ParcelasTextBox.Text = "";
                    ParcelasTextBox.IsEnabled = true;
                    AdicionarBoletoButton.Visibility = Visibility.Visible;
                    BoletosItemsControl.Visibility = Visibility.Visible;
                }
            }
        }

        // Parcelas
        private void ParcelasTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Verifica se é um dígito
            if (!e.Text.All(char.IsDigit))
            {
                e.Handled = true;
                return;
            }

            var textBox = sender as TextBox;
            string novoTexto = textBox != null ? textBox.Text.Insert(textBox.CaretIndex, e.Text) : e.Text;

            // Verifica se o valor está entre 1 e 12
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
                // Limpa o texto se não for um número entre 1 e 8
                if (!textBox.Text.All(char.IsDigit) ||
                    !int.TryParse(textBox.Text, out int val) ||
                    val < 1 || val > 8)
                {
                    textBox.Text = "1";
                }
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
                    textBox.Text = "1";
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
                            textBox.Text = "2";
                    }

                    if (!textBox.IsEnabled && formaPagamentoSelecionada == "Parcelado")
                    {
                        textBox.IsEnabled = true;
                    }
                }
            }

            // Reanexa o handler
            textBox.TextChanged += ParcelasTextBox_TextChanged;

            // Atualiza os valores dos boletos quando o número de parcelas muda
            AtualizarValoresBoletos();
        }

        // Boleto
        private void VencimentoDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DatePicker datePicker && datePicker.DataContext is BoletoData boleto)
            {
                try
                {
                    // Se a data for vazia ou inválida
                    if (!datePicker.SelectedDate.HasValue)
                    {
                        // Define uma data padrão (hoje + 30 dias)
                        boleto.DataVencimento = DateTime.Today.AddDays(30);
                        datePicker.SelectedDate = boleto.DataVencimento;
                    }
                    else if (datePicker.SelectedDate.Value < DateTime.Today)
                    {
                        // Se a data for anterior a hoje, redefine para hoje
                        boleto.DataVencimento = DateTime.Today;
                        datePicker.SelectedDate = boleto.DataVencimento;
                    }
                    else
                    {
                        // Data válida, atualiza no modelo
                        boleto.DataVencimento = datePicker.SelectedDate.Value;
                    }

                    // Atualiza a interface
                    BoletosItemsControl.Items.Refresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao definir data de vencimento: {ex.Message}",
                        "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Nota Fiscal
        private void NotaFiscalTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
            {
                // Verifica se o texto é um número válido
                if (!textBox.Text.All(char.IsDigit))
                {
                    textBox.Clear();
                }
                else
                {
                    // Verifica se a nota fiscal já existe
                    if (NotaFiscalExiste(textBox.Text).Result)
                    {
                        textBox.Clear();
                    }
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
                    textBox.Clear();
                }
                else
                {
                    // Atualiza a nota fiscal nos boletos
                    if (!string.IsNullOrEmpty(NotaFiscalTextBox.Text))
                    {
                        numeroNotaFiscalAtual = NotaFiscalTextBox.Text.Trim();
                        AtualizarValoresBoletos();
                    }
                }
            }
        }
        private void NotaFiscalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            /* Permite números e alguns caracteres comuns em NF */
            e.Handled = !e.Text.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '/');
        }
        private void NotaFiscalTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!text.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '/'))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        private async Task<bool> NotaFiscalExiste(string numeroNotaFiscal, bool mostrarMensagem = true)
        {
            // Retorna false se a nota fiscal estiver vazia
            if (string.IsNullOrWhiteSpace(numeroNotaFiscal))
                return false;

            try
            {
                // Verifica nas compras/vendas da lista atual
                var existeNaLista = usePositiveNumber
                    ? compras.Any(c => c.NotaFiscal == numeroNotaFiscal)
                    : vendas.Any(v => v.NotaFiscal == numeroNotaFiscal);

                if (existeNaLista)
                {
                    if (mostrarMensagem)
                        MessageBox.Show(
                            $"Já existe uma {(usePositiveNumber ? "compra" : "venda")} com esta nota fiscal na lista atual.",
                            "Nota Fiscal Duplicada",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    return true;
                }

                // Verifica no banco de dados (se disponível)
                var db = DatabaseConnect.Database;
                if (db == null)
                    return false;

                // Verifica de acordo com o tipo de operação (entrada ou saída)
                bool existeNoBanco;
                
                if (usePositiveNumber)
                {
                    // Verificação em compras (entradas)
                    var colecaoCompras = db.GetCollection<CompraData>("compras");
                    existeNoBanco = colecaoCompras.Exists(Query.EQ("notaFiscal", numeroNotaFiscal));
                }
                else
                {
                    // Verificação em vendas (saídas)
                    var colecaoVendas = db.GetCollection<VendaData>("vendas");
                    existeNoBanco = colecaoVendas.Exists(Query.EQ("notaFiscal", numeroNotaFiscal));
                }

                if (existeNoBanco && mostrarMensagem)
                    MessageBox.Show(
                        $"Já existe uma {(usePositiveNumber ? "compra" : "venda")} com esta nota fiscal no banco de dados.",
                        "Nota Fiscal Duplicada",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                return existeNoBanco;
            }
            catch (OperationCanceledException)
            {
                if (mostrarMensagem)
                    MessageBox.Show(
                        "A verificação da nota fiscal demorou muito tempo e foi cancelada.",
                        "Tempo Esgotado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                return false;
            }
            catch (Exception ex)
            {
                if (mostrarMensagem)
                    MessageBox.Show(
                        $"Erro ao verificar nota fiscal: {ex.Message}",
                        "Erro",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                return false;
            }
        }

        // Validações
        private bool ValidarMovimentacao()
        {
            // Validar se o produto foi selecionado
            if (produtoSelecionado == null)
            {
                Invalida();
                return false;
            }

            // Validar o fornecedor (para entrada) ou cliente (para saída)
            if (usePositiveNumber && string.IsNullOrEmpty(fornecedorSelecionadoId) &&
                string.IsNullOrEmpty(fornecedorSelecionadoNome))
            {
                Invalida();
                return false;
            }
            else if (!usePositiveNumber && string.IsNullOrEmpty(clienteSelecionadoId) &&
                     string.IsNullOrEmpty(clienteSelecionadoCNPJ))
            {
                Invalida();
                return false;
            }

            // Validar quantidade
            if (!int.TryParse(QuantidadeTextBox.Text, out int quantidade) || quantidade <= 0)
            {
                Invalida();
                return false;
            }

            // Verificar estoque para operações de saída
            if (!usePositiveNumber && produtoSelecionado.Quantidade < quantidade)
            {
                Invalida();
                return false;
            }

            // Validar preço
            if (!double.TryParse(PrecoTextBox.Text.Replace(".", "").Replace(",", "."),
                NumberStyles.Any, CultureInfo.InvariantCulture, out double preco) || preco <= 0)
            {
                Invalida();
                return false;
            }

            Valida();
            return true;
        }
        // TODO: Fazer mensagem de invalidação para financeiro
        private bool ValidarFinanceiro()
        {
            // Validar forma de pagamento
            if (string.IsNullOrEmpty(formaPagamentoSelecionada))
            {
                return false;
            }

            // Validar número de parcelas
            if (!int.TryParse(ParcelasTextBox.Text, out int parcelas) || parcelas <= 0 || parcelas > 8)
            {
                return false;
            }

            // Valida todos campos de boleto
            if (usePositiveNumber && boletos.Count > 0)
            {
                foreach (var boleto in boletos)
                {
                    if (boleto == null)
                    {
                        MessageBox.Show("Um ou mais boletos são inválidos.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    try
                    {
                        // Verifica se a data está vazia (valor default do DateTime)
                        if (boleto.DataVencimento == default(DateTime))
                        {
                            MessageBox.Show("Um ou mais boletos estão com data de vencimento em branco.",
                                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }

                        // Verifica se a data é anterior a hoje
                        if (boleto.DataVencimento < DateTime.Today)
                        {
                            MessageBox.Show($"Boleto com data de vencimento inválida: {boleto.DataVencimento.ToString("d")}.",
                                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao validar a data de vencimento de um boleto: {ex.Message}",
                            "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
            }

            // Validar nota fiscal
            if (string.IsNullOrWhiteSpace(NotaFiscalTextBox.Text))
            {
                return false;
            }

            return true;
        }
        private void Valida()
        {
            StatusMessage.Visibility = Visibility.Collapsed;
            Financeiro.Visibility = Visibility.Visible;
        }
        private void Invalida()
        {
            StatusMessage.Visibility = Visibility.Visible;
            Financeiro.Visibility = Visibility.Collapsed;
        }

        private void AdicionarProdutoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var editarProdutoWindow = new EditarProdutoWindow(null);
                if (editarProdutoWindow.ShowDialog() == true)
                {
                    // Atualiza a lista de produtos e seleciona o produto recém-criado
                    CarregarProdutos();
                    ProdutoComboBox.SelectedValue = editarProdutoWindow.Produto.Codigo;
                    ProdutoComboBox.Text = editarProdutoWindow.Produto.Nome;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir janela de edição de produto: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Alerta.AdicionarAlerta("Erro", 
                    ex.Message,
                    "Erro ao adicionar novo produto.",
                    "- Verifique se a janela de edição de produto pode ser aberta.");
            }
        }

        private void AdicionarFornecedorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var editarFornecedorWindow = new EditarFornecedorWindow(null);
                if (editarFornecedorWindow.ShowDialog() == true)
                {
                    // Atualiza a lista de fornecedores e seleciona o fornecedor recém-criado
                    CarregarFornecedores();
                    FornecedorComboBox.SelectedValue = editarFornecedorWindow.Fornecedor.CNPJ;
                    FornecedorComboBox.Text = editarFornecedorWindow.Fornecedor.Nome;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir janela de edição de fornecedor: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Alerta.AdicionarAlerta("Erro", 
                    ex.Message,
                    "Erro ao adicionar novo fornecedor.",
                    "- Verifique se a janela de edição de fornecedor pode ser aberta.");
            }
        }

        private void AdicionarClienteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var editarClienteWindow = new EditarClienteWindow(null);
                if (editarClienteWindow.ShowDialog() == true)
                {
                    // Atualiza a lista de clientes e seleciona o cliente recém-criado
                    CarregarClientes();
                    ClienteComboBox.SelectedValue = editarClienteWindow.Cliente.CNPJ;
                    ClienteComboBox.Text = editarClienteWindow.Cliente.Email;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir janela de edição de cliente: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Alerta.AdicionarAlerta("Erro", 
                    ex.Message,
                    "Erro ao adicionar novo cliente.",
                    "- Verifique se a janela de edição de cliente pode ser aberta.");
            }
        }
    }
}

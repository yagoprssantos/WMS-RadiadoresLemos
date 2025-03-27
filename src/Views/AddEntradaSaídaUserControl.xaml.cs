using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Google.Cloud.Firestore;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF
{
    // Definição parcial da classe AddEntradaSaidaUserControl que herda de UserControl
    public partial class AddEntradaSaidaUserControl : UserControl
    {
        // Declaração de variáveis privadas para armazenar dados dos produtos
        private List<ProdutoData> produtos = new List<ProdutoData>();
        private ObservableCollection<string> produtosFiltrados = new ObservableCollection<string>();
        private Dictionary<string, string> produtoNomeParaId = new Dictionary<string, string>();
        private ProdutoData? produtoSelecionado;
        private bool usePositiveNumber = true;
        private static readonly string CaminhoArquivoProdutos = new DatabaseFileManager().ObterCaminhoArquivo("Produtos");


        // Construtor da classe que inicializa os componentes e carrega os produtos
        public AddEntradaSaidaUserControl()
        {
            InitializeComponent();
            ProdutoComboBox.ItemsSource = produtosFiltrados;
            Setup();
        }

        private void Setup()
        {
            CarregarProdutos();
            ToggleVisibility(false);
        }

        private void CarrinhoDeComprasButton_Click(object sender, RoutedEventArgs e)
        {
            CarrinhoDeComprasPopup.IsOpen = true;
        }

        private void FecharCarrinhoDeCompras_Click(object sender, RoutedEventArgs e)
        {
            CarrinhoDeComprasPopup.IsOpen = false;
        }


        // Método para carregar produtos do cache
        private void CarregarProdutos()
        {
            try
            {
                // Carregar produtos do cache
                if (DadosCache.Tabelas.TryGetValue("Produtos", out List<object>? produtosCache))
                {
                    // Converte a lista de objetos para uma lista de produtos
                    produtos = produtosCache.Cast<ProdutoData>().ToList();
                    produtoNomeParaId = produtos.ToDictionary(p => p.Nome, p => p.Id);
                    produtosFiltrados.Clear();
                    foreach (var produto in produtos)
                    {
                        produtosFiltrados.Add(produto.Nome);
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Erro ao carregar produtos: {ex.Message}");

                // Adicionar alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Erro ao carregar produtos do cache. Possíveis motivos:\n" +
                                            "- Falha na conexão com o banco de dados.\n" +
                                            "- Não foi possível carregar os produtos do cache.",
                                            "- Verifique a conexão com a internet.\n" +
                                            "- Reinicie o aplicativo.");
            }
        }

        // Método para alternar a visibilidade dos detalhes do produto
        private void ToggleVisibility(bool isVisible)
        {
            var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

            // Títulos e detalhes antes e depois
            AntesTextBlock.Visibility = visibility;
            DepoisTextBlock.Visibility = visibility;
            AntesGrid.Visibility = visibility;
            DepoisGrid.Visibility = visibility;

            // Produto Selecionado
            ProdutoSelecionado.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
            ProdutoAntesDepois.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        // Método que é chamado quando o texto da caixa de pesquisa é alterado
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = ProdutoComboBox.Text.ToLower();
            var filteredProducts = produtos
                .Where(p => p.Nome.ToLower().Contains(searchText))
                .Select(p => p.Nome)
                .ToList();

            if (!filteredProducts.SequenceEqual(produtosFiltrados))
            {
                produtosFiltrados.Clear();
                foreach (var produto in filteredProducts)
                {
                    produtosFiltrados.Add(produto);
                }
            }

            ProdutoComboBox.IsDropDownOpen = produtosFiltrados.Count > 0;
        }

        // Método para confirmar se o produto selecionado é válido
        private void ProdutoComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            string inputText = ProdutoComboBox.Text;

            if (ProdutoComboBox.SelectedItem is string selectedProductName)
            {
                inputText = selectedProductName;
            }

            if (!string.IsNullOrEmpty(inputText) && !produtoNomeParaId.ContainsKey(inputText))
            {
                ProdutoComboBox.Text = string.Empty;
                ProdutoComboBox.SelectedItem = null;
                ProdutoComboBox.Focus();
            }
        }

        // Método que é chamado quando a seleção do ComboBox é alterada
        private void ProdutoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProdutoComboBox.SelectedItem is string selectedProductName)
            {
                produtoSelecionado = produtos.FirstOrDefault(p => p.Nome == selectedProductName);
                if (produtoSelecionado != null)
                {
                    AtualizarProdutoSelecionado(produtoSelecionado);
                }
                else
                {
                    //MessageBox.Show("Produto não encontrado no cache.");
                }
            }
        }

        // Método para atualizar os detalhes do produto selecionado
        private void AtualizarProdutoSelecionado(ProdutoData produto)
        {
            NomeSelecionadoDadoTextBlock.Text = produto.Nome;
            TipoSelecionadoDadoTextBlock.Text = produto.Tipo;
            MarcaSelecionadoDadoTextBlock.Text = produto.Marca;
            CodigoSelecionadoDadoTextBlock.Text = produto.Codigo;
            QuantidadeSelecionadoDadoTextBlock.Text = produto.Quantidade.ToString();
            PrecoSelecionadoDadoTextBlock.Text = produto.Preço.ToString("C");
        }

        // Método para atualizar os detalhes do produto selecionado
        private bool AtualizarDetalhesProduto(ProdutoData produto)
        {
            // Mostrar os detalhes atuais do produto (TextBlock1)
            TipoAntesDadoTextBlock.Text = produto.Tipo;
            MarcaAntesDadoTextBlock.Text = produto.Marca;
            CodigoAntesDadoTextBlock.Text = produto.Codigo;
            QuantidadeAntesDadoTextBlock.Text = produto.Quantidade.ToString();
            PrecoAntesDadoTextBlock.Text = produto.Preço.ToString("C");

            // Se quantidade e preço forem vazios, não atualiza valores depois
            if (string.IsNullOrEmpty(QuantidadeTextBox.Text) || string.IsNullOrEmpty(PrecoTextBox.Text))
            {
                return false;
            }

            // Mostrar os detalhes depois do produto (TextBlock2)
            TipoDepoisDadoTextBlock.Text = produto.Tipo;
            MarcaDepoisDadoTextBlock.Text = produto.Marca;
            CodigoDepoisDadoTextBlock.Text = produto.Codigo;

            // Se a quantidade e preço forem um número válido, atualiza a quantidade e o preço depois
            if (int.TryParse(QuantidadeTextBox.Text, out int quantidadeAlterada) && double.TryParse(PrecoTextBox.Text, out double precoAlterado))
            {
                int quantidadeFinal;
                if (usePositiveNumber)
                {
                    // Entrada
                    quantidadeFinal = produto.Quantidade + quantidadeAlterada;
                }
                else
                {
                    // Saída
                    quantidadeFinal = produto.Quantidade - quantidadeAlterada;
                }

                if (quantidadeFinal < 0)
                {
                    // Avisa que quantidade não pode ser negativa e retorna
                    //MessageBox.Show("Não existem produtos suficientes no Estoque");

                    // Adiciona alerta
                    AlertaCache.AdicionarAlerta("Erro",
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
                    // Mostra o preço alterado do produto (calcula média ponderada) com base na nova quantidade (QuantidadeDepoisDadoTextBlock)
                    double precoAtual = produto.Preço;
                    double precoNovo = double.Parse(PrecoTextBox.Text);
                    int quantidadeAtual = produto.Quantidade;
                    int quantidadeNova = int.Parse(QuantidadeTextBox.Text);
                    int quantidadeTotal = quantidadeAtual + quantidadeNova;

                    double precoPonderado = ((precoAtual * quantidadeAtual) + (precoNovo * quantidadeNova)) / quantidadeTotal;
                    PrecoDepoisDadoTextBlock.Text = precoPonderado.ToString("C");
                }
                else
                {
                    // Mantém o preço atual para saída
                    PrecoDepoisDadoTextBlock.Text = produto.Preço.ToString("C");
                }
            }
            else
            {
                MessageBox.Show("Por favor, insira um valor numérico válido para a quantidade.");
                return false;
            }

            return true;
        }

        private void RegistrarEntrada_Click(object sender, RoutedEventArgs e)
        {
            usePositiveNumber = true;

            bool detalhesAtualizados = false;

            if (produtoSelecionado != null)
            {
                detalhesAtualizados = AtualizarDetalhesProduto(produtoSelecionado);
            }

            if (!detalhesAtualizados)
            {
                return;
            }

            ToggleVisibility(true);
            ConfirmarRegistroButton.Visibility = Visibility.Visible;
            CancelarRegistroButton.Visibility = Visibility.Visible;
            RegistrarEntradaButton.Visibility = Visibility.Collapsed;
            RegistrarSaidaButton.Visibility = Visibility.Collapsed;

            // Desabilitar o ComboBox
            ProdutoComboBox.IsHitTestVisible = false;
            ProdutoComboBox.IsEnabled = true;
        }

        private void RegistrarSaida_Click(object sender, RoutedEventArgs e)
        {
            usePositiveNumber = false;

            bool detalhesAtualizados = false;

            if (produtoSelecionado != null)
            {
                detalhesAtualizados = AtualizarDetalhesProduto(produtoSelecionado);
            }

            if (!detalhesAtualizados)
            {
                return;
            }

            ToggleVisibility(true);
            ConfirmarRegistroButton.Visibility = Visibility.Visible;
            CancelarRegistroButton.Visibility = Visibility.Visible;
            RegistrarEntradaButton.Visibility = Visibility.Collapsed;
            RegistrarSaidaButton.Visibility = Visibility.Collapsed;

            // Desabilitar o ComboBox
            ProdutoComboBox.IsHitTestVisible = false;
            ProdutoComboBox.IsEnabled = true;
        }


        // Método para confirmar a ação de registro
        private async void ConfirmarAcao_Click(object sender, RoutedEventArgs e)
        {
            if (produtoSelecionado == null)
            {
                MessageBox.Show("Nenhum produto selecionado.");
                return;
            }

            if (!int.TryParse(QuantidadeTextBox.Text, out int quantidade) || !double.TryParse(PrecoTextBox.Text, out double preco))
            {
                MessageBox.Show("Por favor, insira valores válidos para quantidade e preço.");
                return;
            }

            if (usePositiveNumber)
            {
                await RegistrarMovimentacaoAsync(true, quantidade, preco);
            }
            else
            {
                await RegistrarMovimentacaoAsync(false, quantidade, preco);
            }

            ToggleVisibility(false);
            ConfirmarRegistroButton.Visibility = Visibility.Collapsed;
            CancelarRegistroButton.Visibility = Visibility.Collapsed;
            RegistrarEntradaButton.Visibility = Visibility.Visible;
            RegistrarSaidaButton.Visibility = Visibility.Visible;
            LimparCampos();
        }

        // Método para cancelar a ação de registro
        private void CancelarAcao_Click(object sender, RoutedEventArgs e)
        {
            ToggleVisibility(false);
            ConfirmarRegistroButton.Visibility = Visibility.Collapsed;
            CancelarRegistroButton.Visibility = Visibility.Collapsed;
            RegistrarEntradaButton.Visibility = Visibility.Visible;
            RegistrarSaidaButton.Visibility = Visibility.Visible;

            usePositiveNumber = true;

            // Habilitar o ComboBox
            ProdutoComboBox.IsHitTestVisible = true;
            ProdutoComboBox.IsEnabled = true;
        }

        // Método assíncrono para registrar a movimentação de produtos
        private async Task RegistrarMovimentacaoAsync(bool isEntrada, int quantidadeMovimentacao, double precoMovimentacao)
        {
            try
            {
                // Verificar se o produto selecionado é válido
                if (produtoSelecionado != null)
                {
                    if (produtoSelecionado == null)
                    {
                        MessageBox.Show("Nenhum produto selecionado.");
                        return;
                    }

                    // Atualiza a quantidade do produto
                    produtoSelecionado.Quantidade = isEntrada ? produtoSelecionado.Quantidade + quantidadeMovimentacao : produtoSelecionado.Quantidade - quantidadeMovimentacao;

                    // Atualiza o preço do produto com o valor calculado no PrecoDepoisDadoTextBlock
                    if (double.TryParse(PrecoDepoisDadoTextBlock.Text, System.Globalization.NumberStyles.Currency, null, out double precoAtualizado))
                    {
                        produtoSelecionado.Preço = precoAtualizado;
                    }

                    double preco = produtoSelecionado.Preço;


                    // Cria um objeto de movimentação com os dados fornecidos
                    var movimentacao = new MovimentacaoData
                    {
                        ProdutoId = produtoSelecionado.Id,
                        Quantidade = quantidadeMovimentacao,
                        Preço = precoMovimentacao,
                        Data = DateTime.UtcNow,
                        Tipo = isEntrada ? "Entrada" : "Saída"
                    };

                    // Adiciona a movimentação ao cache, no Firestore e no arquivo JSON
                    await MovimentacoesCache.RegistrarMovimentacaoAsync(movimentacao);

                    // Mostra mensagem de sucesso
                    MessageBox.Show($"{(isEntrada ? "Entrada" : "Saída")} registrada com sucesso");

                    // Adiciona log
                    var log = new LogData
                    {
                        Data = DateTime.UtcNow,
                        Tipo = "OPERACIONAL",
                        Nivel = "Usuário",
                        Detalhes = $"{(isEntrada ? "Entrada" : "Saída")} registrada: Produto - {produtoSelecionado.Nome}; Quantidade - {quantidadeMovimentacao}; Preço - {precoMovimentacao}",
                        Usuario = MainWindow.UsuarioLogado.Nome
                    };
                    await LogHistorico.RegistrarLogAsync(log);

                    // Limpar campos após o registro, mantendo o produto selecionado
                    LimparCampos();

                    // Atualizar detalhes do produto
                    AtualizarDetalhesProduto(produtoSelecionado);
                }
                else
                {
                    MessageBox.Show("Selecione um produto.");
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Erro ao registrar movimentação: {ex.Message}");

                // Adicionar alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Erro ao registrar movimentação de produtos. Possíveis motivos:\n" +
                                            "- Produto inválido ou não selecionado.\n" +
                                            "- Quantidade inválida ou insuficiente.\n" +
                                            "- Falha na conexão com o banco de dados.\n" +
                                            "- Não foi possível atualizar o produto no banco de dados.",
                                            "- Verifique se o produto foi selecionado corretamente.\n" +
                                            "- Verifique se a quantidade um valor é válido.\n" +
                                            "- Verifique a conexão com a internet.\n" +
                                            "- Reinicie o aplicativo.");
            }
        }

        // Método para limpar os campos de entrada
        private void LimparCampos()
        {
            QuantidadeTextBox.Clear();
            PrecoTextBox.Clear();
        }

        // Métodos para validar a entrada de texto na caixa de quantidade (apenas números inteiros)
        private void QuantidadeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }
        private void QuantidadeTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)) && !int.TryParse((string)e.DataObject.GetData(typeof(string)), out _))
            {
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
                if (int.TryParse(textBox.Text, out int quantidade))
                {
                    textBox.Text = quantidade.ToString("N0", new System.Globalization.CultureInfo("pt-BR"));
                }
                else
                {
                    MessageBox.Show("Quantidade inválida.");
                    textBox.Clear();
                }
            }
        }

        // Métodos para validar a entrada de texto na caixa de preço (apenas números decimais)
        private void PrecoTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == ",")
            {
                if (((TextBox)sender).Text.Contains(","))
                {
                    e.Handled = true;
                }
                return;
            }

            e.Handled = !double.TryParse(e.Text, out _);
        }
        private void PrecoTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)) && !double.TryParse((string)e.DataObject.GetData(typeof(string)), out _))
            {
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
                if (double.TryParse(textBox.Text, out double preco))
                {
                    textBox.Text = preco.ToString("N2", new System.Globalization.CultureInfo("pt-BR"));
                }
                else
                {
                    MessageBox.Show("Preço inválido.");
                    textBox.Clear();
                }
            }
        }
    }
}

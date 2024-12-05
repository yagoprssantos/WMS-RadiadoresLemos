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
    // Definição parcial da classe RegistroEntradaSaidaUserControl que herda de UserControl
    public partial class RegistroEntradaSaidaUserControl : UserControl
    {
        // Declaração de variáveis privadas para armazenar dados dos produtos
        private List<ProdutoData> produtos = new List<ProdutoData>();
        private ObservableCollection<string> produtosFiltrados = new ObservableCollection<string>();
        private Dictionary<string, string> produtoNomeParaId = new Dictionary<string, string>();
        private ProdutoData? produtoSelecionado;
        private bool usePositiveNumber = true;

        // Construtor da classe que inicializa os componentes e carrega os produtos
        public RegistroEntradaSaidaUserControl()
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

        // Método assíncrono para carregar produtos do cache
        private void CarregarProdutos()
        {
            try
            {
                if (DadosCache.Tabelas.TryGetValue("Produtos", out List<object>? produtosCache))
                {
                    produtos = produtosCache.Cast<ProdutoData>().ToList();
                    produtoNomeParaId = produtos.ToDictionary(p => p.Nome, p => p.Id);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar produtos: {ex.Message}");

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

            // Títulos
            AntesTextBlock.Visibility = visibility;
            DepoisTextBlock.Visibility = visibility;

            // Detalhes depois do produto
            DepoisGrid.Visibility = visibility;
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
                MessageBox.Show("Produto inválido.");
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
                    AtualizarDetalhesProduto(produtoSelecionado);
                }
                else
                {
                    MessageBox.Show("Produto não encontrado no cache.");
                }
            }
        }

        // Método para atualizar os detalhes do produto selecionado
        private void AtualizarDetalhesProduto(ProdutoData produto)
        {
            // Mostrar os detalhes atuais do produto (TextBlock1)
            NomeAtualDadoTextBlock.Text = produto.Nome;
            TipoAtualDadoTextBlock.Text = produto.Tipo;
            MarcaAtualDadoTextBlock.Text = produto.Marca;
            CodigoAtualDadoTextBlock.Text = produto.Codigo;
            QuantidadeAtualDadoTextBlock.Text = produto.Quantidade.ToString();
            PrecoAtualDadoTextBlock.Text = produto.Preco.ToString("C");

            // Se quantidade e preço forem vazios, não atualiza valores depois
            if (string.IsNullOrEmpty(QuantidadeTextBox.Text) || string.IsNullOrEmpty(PrecoTextBox.Text))
            {
                return;
            }

            // Mostrar os detalhes depois do produto (TextBlock2)
            NomeDepoisDadoTextBlock.Text = produto.Nome;
            TipoDepoisDadoTextBlock.Text = produto.Tipo;
            MarcaDepoisDadoTextBlock.Text = produto.Marca;
            CodigoDepoisDadoTextBlock.Text = produto.Codigo;

            // Se a quantidade e preço forem um número válido, atualiza a quantidade e o preço depois
            if (int.TryParse(QuantidadeTextBox.Text, out int quantidadeAlterada) && double.TryParse(PrecoTextBox.Text, out double precoAlterado))
            {
                // Mostrar a quantidade alterado do produto (calculado) dependendo se é entrada ou saída
                if (usePositiveNumber)
                {
                    // Entrada
                    QuantidadeDepoisDadoTextBlock.Text = (produto.Quantidade + quantidadeAlterada).ToString();
                }
                else
                {
                    // Saída
                    QuantidadeDepoisDadoTextBlock.Text = (produto.Quantidade - quantidadeAlterada).ToString();
                }

                // Mostra o preço alterado do produto (calcula média ponderada) com base na nova quantidade (QuantidadeDepoisDadoTextBlock)
                double preco = produto.Preco;
                double precoNovo = double.Parse(PrecoTextBox.Text);
                int quantidade = int.Parse(QuantidadeDepoisDadoTextBlock.Text);
                double precoDepois = (preco * produto.Quantidade + precoNovo * quantidade) / (produto.Quantidade + quantidade);
                PrecoDepoisDadoTextBlock.Text = precoDepois.ToString("C");
            }
            else
            {
                MessageBox.Show("Por favor, insira um valor numérico válido para a quantidade.");
            }
        }


        // Método assíncrono para registrar a entrada de produtos
        private void RegistrarEntrada_Click(object sender, RoutedEventArgs e)
        {
            usePositiveNumber = true;

            if (produtoSelecionado != null)
            {
                AtualizarDetalhesProduto(produtoSelecionado);
            }

            ToggleVisibility(true);
            ConfirmarRegistroButton.Visibility = Visibility.Visible;
            CancelarRegistroButton.Visibility = Visibility.Visible;
            RegistrarEntradaButton.Visibility = Visibility.Collapsed;
            RegistrarSaidaButton.Visibility = Visibility.Collapsed;
        }

        // Método assíncrono para registrar a saída de produtos
        private void RegistrarSaida_Click(object sender, RoutedEventArgs e)
        {
            usePositiveNumber = false;

            if (produtoSelecionado != null)
            {
                AtualizarDetalhesProduto(produtoSelecionado);
            }

            ToggleVisibility(true);
            ConfirmarRegistroButton.Visibility = Visibility.Visible;
            CancelarRegistroButton.Visibility = Visibility.Visible;
            RegistrarEntradaButton.Visibility = Visibility.Collapsed;
            RegistrarSaidaButton.Visibility = Visibility.Collapsed;
        }

        // Método para confirmar a ação de registro
        private async void ConfirmarAcao_Click(object sender, RoutedEventArgs e)
        {
            if (usePositiveNumber)
            {
                await RegistrarMovimentacaoAsync(true);
            }
            else
            {
                await RegistrarMovimentacaoAsync(false);
            }

            ToggleVisibility(false);
            ConfirmarRegistroButton.Visibility = Visibility.Collapsed;
            CancelarRegistroButton.Visibility = Visibility.Collapsed;
            RegistrarEntradaButton.Visibility = Visibility.Visible;
            RegistrarSaidaButton.Visibility = Visibility.Visible;

            usePositiveNumber = true;
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
        }

        // Método assíncrono para registrar a movimentação de produtos
        private async Task RegistrarMovimentacaoAsync(bool isEntrada)
        {
            try
            {
                if (produtoSelecionado != null)
                {
                    if (!int.TryParse(QuantidadeTextBox.Text, out int quantidade))
                    {
                        MessageBox.Show("Quantidade inválida.");
                        return;
                    }

                    if (!isEntrada && produtoSelecionado.Quantidade < quantidade)
                    {
                        MessageBox.Show("Quantidade insuficiente em estoque.");
                        return;
                    }

                    int quantidadeFinal = produtoSelecionado.Quantidade + (isEntrada ? quantidade : -quantidade);

                    // Garantir que a quantidade final nunca seja menor do que zero
                    if (quantidadeFinal < 0)
                    {
                        quantidadeFinal = 0;
                    }

                    double preco = produtoSelecionado.Preco;

                    // Garantir que o preço do produto não seja negativo
                    if (preco < 0)
                    {
                        preco = 0;
                    }

                    // Atualiza a quantidade do produto
                    produtoSelecionado.Quantidade = quantidadeFinal;

                    // Atualiza o produto no banco de dados
                    await AtualizarProdutoNoBanco(produtoSelecionado);
                    MessageBox.Show($"{(isEntrada ? "Entrada" : "Saída")} registrada com sucesso");

                    // Adiciona log
                    var log = new LogData
                    {
                        Data = DateTime.UtcNow,
                        Tipo = "OPERACIONAL",
                        Nivel = "Usuário",
                        Detalhes = $"{(isEntrada ? "Entrada" : "Saída")} registrada: Produto - {produtoSelecionado.Nome}; Quantidade adicionada - {quantidade};  Quantidade atual - {quantidadeFinal}",
                        Usuario = "NomeDoUsuario" // Substitua pelo nome do usuário real
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
                MessageBox.Show($"Erro ao registrar movimentação: {ex.Message}");

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

        // Método assíncrono para atualizar o produto no banco de dados
        private async Task AtualizarProdutoNoBanco(ProdutoData produto)
        {
            try
            {
                var db = DatabaseConnect.Database ?? throw new InvalidOperationException("Conexão com o banco de dados não estabelecida.");
                DocumentReference docRef = db.Collection("Produtos").Document(produto.Id);
                await docRef.SetAsync(produto, SetOptions.Overwrite);

                DadosCache.Tabelas["Produtos"] = produtos.Cast<object>().ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar produto no banco de dados: {ex.Message}");

                // Adicionar alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Erro ao atualizar produto no banco de dados. Possíveis motivos:\n" +
                                            "- Falha na conexão com o banco de dados.\n" +
                                            "- Não foi possível atualizar o produto no banco de dados.",
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

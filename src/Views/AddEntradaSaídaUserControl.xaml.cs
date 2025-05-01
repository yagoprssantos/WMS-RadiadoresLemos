using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;
using WMS_RadiadoresLemos_WPF.src.Views;
using LiteDB;

namespace WMS_RadiadoresLemos_WPF
{
    // Definição parcial da classe AddEntradaSaidaUserControl que herda de UserControl
    public partial class AddEntradaSaídaUserControl : UserControl
    {
        private List<ProdutoData> produtos = new List<ProdutoData>();
        private ObservableCollection<MovimentacaoData> carrinhoDeCompras = new ObservableCollection<MovimentacaoData>();
        private ProdutoData? produtoSelecionado;
        private bool usePositiveNumber = true;
        private static readonly string CollectionName = "produtos";

        // Construtor da classe que inicializa os componentes e carrega os produtos
        public AddEntradaSaídaUserControl()
        {
            InitializeComponent();
            Setup();

            // Vincular a coleção carrinhoDeCompras ao ItemsControl na interface do usuário
            ListaItemsControl.ItemsSource = carrinhoDeCompras;
        }

        private void Setup()
        {
            produtoSelecionado = null;
            usePositiveNumber = true;

            CarregarProdutos().Wait();
            ToggleVisibility(false);
        }

        // Método para carregar produtos do cache
        private async Task CarregarProdutos()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db != null)
                {
                    var collection = db.GetCollection<ProdutoData>("produtos");
                    produtos = collection.FindAll().ToList();

                    // Adiciona os produtos ao ComboBox
                    ProdutoComboBox.ItemsSource = produtos.Select(p => p.Nome).ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar produtos: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para alternar a visibilidade dos detalhes do produto
        private void ToggleVisibility(bool isVisible)
        {
            var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

            // Atualizar visibilidade dos elementos
            ProdutoAntesDepois.Visibility = visibility;
            ConfirmarRegistroButton.Visibility = visibility;
            CancelarRegistroButton.Visibility = visibility;
            RegistrarEntradaButton.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
            RegistrarSaidaButton.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;

            // Desabilitar ou habilitar o ComboBox
            ProdutoComboBox.IsHitTestVisible = !isVisible;
            ProdutoComboBox.IsEnabled = !isVisible;
        }


        // Produto ComboBox
        // Método que é chamado quando o texto da caixa de pesquisa é alterado
        private void ProdutoComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = ProdutoComboBox.Text.ToLower();
            var filteredProducts = produtos
                .Where(p => p.Nome.ToLower().Contains(searchText))
                .Select(p => p.Nome)
                .ToList();

            ProdutoComboBox.ItemsSource = filteredProducts;
            ProdutoComboBox.IsDropDownOpen = filteredProducts.Count > 0;
        }

        // Método para confirmar se o produto selecionado é válido
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
            }
            else
            {
                ProdutoComboBox.Text = string.Empty;
                ProdutoComboBox.SelectedItem = null;
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
                    MessageBox.Show("Produto não encontrado no cache.");
                }
            }
        }

        // Método chamado quando um produto é selecionado, altera as informações apresentadas na tela
        private void AtualizarProdutoSelecionado(ProdutoData produto)
        {
            // Atualizar os detalhes do produto selecionado
            NomeSelecionadoDadoTextBlock.Text = produto.Nome;
            TipoSelecionadoDadoTextBlock.Text = produto.Tipo;
            MarcaSelecionadoDadoTextBlock.Text = produto.Marca;
            CodigoSelecionadoDadoTextBlock.Text = produto.Codigo;
            QuantidadeSelecionadoDadoTextBlock.Text = produto.Quantidade.ToString();
            PrecoSelecionadoDadoTextBlock.Text = produto.Preco.ToString("C");

            // Alterar o texto do ComboBox para o nome do produto selecionado
            ProdutoComboBox.Text = produto.Nome;
        }


        // Botões de registrar entrada e saída
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

        // Método para atualizar os detalhes do produto selecionado
        private bool AtualizarDetalhesProduto(ProdutoData produto)
        {
            // Mostrar os detalhes atuais do produto (TextBlock1)
            TipoAntesDadoTextBlock.Text = produto.Tipo;
            MarcaAntesDadoTextBlock.Text = produto.Marca;
            CodigoAntesDadoTextBlock.Text = produto.Codigo;
            QuantidadeAntesDadoTextBlock.Text = produto.Quantidade.ToString();
            PrecoAntesDadoTextBlock.Text = produto.Preco.ToString("C");

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
                    // Mostra o preço alterado do produto (calcula média ponderada) com base na nova quantidade (QuantidadeDepoisDadoTextBlock)
                    double precoAtual = produto.Preco;
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
                    PrecoDepoisDadoTextBlock.Text = produto.Preco.ToString("C");
                }
            }
            else
            {
                MessageBox.Show("Por favor, insira um valor numérico válido para a quantidade.");
                return false;
            }

            return true;
        }


        // Botões de confirmar e cancelar movimentação
        private void ConfirmarAcao_Click(object sender, RoutedEventArgs e)
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

            var movimentacao = new MovimentacaoData
            {
                Id = 0, // O LiteDB irá gerar o ID automaticamente
                Tipo = usePositiveNumber ? "Entrada" : "Saída",
                Quantidade = quantidade,
                Preco = preco,
                ProdutoId = produtoSelecionado.Id,
                Data = DateTime.UtcNow,
            };

            carrinhoDeCompras.Add(movimentacao);

            // Atualizar a interface do usuário
            CarrinhoDeComprasItemsControl.ItemsSource = null;
            CarrinhoDeComprasItemsControl.ItemsSource = carrinhoDeCompras;

            ToggleVisibility(false);
            ConfirmarRegistroButton.Visibility = Visibility.Collapsed;
            CancelarRegistroButton.Visibility = Visibility.Collapsed;
            RegistrarEntradaButton.Visibility = Visibility.Visible;
            RegistrarSaidaButton.Visibility = Visibility.Visible;
            LimparCampos();

            // Ativa ComboBox para selecionar outro produto
            ProdutoComboBox.IsHitTestVisible = true;
            ProdutoComboBox.IsEnabled = true;
        }
        private void CancelarAcao_Click(object sender, RoutedEventArgs e)
        {
            ToggleVisibility(false);
            ConfirmarRegistroButton.Visibility = Visibility.Collapsed;
            CancelarRegistroButton.Visibility = Visibility.Collapsed;
            RegistrarEntradaButton.Visibility = Visibility.Visible;
            RegistrarSaidaButton.Visibility = Visibility.Visible;

            usePositiveNumber = true;

            // Ativa ComboBox para selecionar outro produto
            ProdutoComboBox.IsHitTestVisible = true;
            ProdutoComboBox.IsEnabled = true;
        }


        // Métodos para carrinho de compras
        private void CarrinhoDeComprasButton_Click(object sender, RoutedEventArgs e)
        {
            CarrinhoDeComprasPopup.IsOpen = true;
        }
        private void ExcluirItem_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.DataContext;
            if (item != null)
            {
                var items = CarrinhoDeComprasItemsControl.ItemsSource as IList;
                if (items != null)
                {
                    items.Remove(item);
                }
            }
        }
        private void ExcluirTodosItens_Click(object sender, RoutedEventArgs e)
        {
            carrinhoDeCompras.Clear();
        }
        private async void FinalizarCarrinhoDeCompras_Click(object sender, RoutedEventArgs e)
        {
            foreach (var movimentacao in carrinhoDeCompras)
            {
                await RegistrarMovimentacaoAsync(movimentacao.Tipo == "Entrada", movimentacao.Quantidade, movimentacao.Preco);
            }

            carrinhoDeCompras.Clear();
            CarrinhoDeComprasPopup.IsOpen = false;

            // Garante que o ComboBox está habilitado
            ProdutoComboBox.IsHitTestVisible = true;
            ProdutoComboBox.IsEnabled = true;
        }

        // Método assíncrono para registrar a movimentação de produtos
        private async Task RegistrarMovimentacaoAsync(bool isEntrada, int quantidadeMovimentacao, double precoMovimentacao)
        {
            try
            {
                if (produtoSelecionado == null)
                {
                    MessageBox.Show("Selecione um produto para registrar a movimentação.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var movimentacao = new MovimentacaoData
                {
                    Id = 0, // O LiteDB irá gerar o ID automaticamente
                    Data = DateTime.Now,
                    Tipo = isEntrada ? "Entrada" : "Saída",
                    Quantidade = quantidadeMovimentacao,
                    Preco = precoMovimentacao,
                    ProdutoId = produtoSelecionado.Id
                };

                if (DatabaseConnect.Database == null)
                    return;

                var collection = DatabaseConnect.Database.GetCollection<MovimentacaoData>("movimentacoes");
                collection.Insert(movimentacao);

                // Atualiza a quantidade do produto no banco de dados
                produtoSelecionado.Quantidade += isEntrada ? quantidadeMovimentacao : -quantidadeMovimentacao;
                var produtoCollection = DatabaseConnect.Database.GetCollection<ProdutoData>(CollectionName);
                produtoCollection.Update(produtoSelecionado);

                // Limpa os campos e atualiza a interface
                LimparCampos();
                await CarregarProdutos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao registrar movimentação: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para limpar os campos de entrada
        private void LimparCampos()
        {
            QuantidadeTextBox.Clear();
            PrecoTextBox.Clear();
        }





        // Todos os métodos de validação de entrada de texto
        // Quantidade
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
        // Preço
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

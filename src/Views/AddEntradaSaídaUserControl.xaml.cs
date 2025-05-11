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
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WMS_RadiadoresLemos_WPF
{
    // Definição parcial da classe AddEntradaSaidaUserControl que herda de UserControl
    public partial class AddEntradaSaídaUserControl : UserControl
    {
        private List<ProdutoData> produtos = new List<ProdutoData>();
        private ObservableCollection<MovimentacaoData> listaMovimentacoes = new ObservableCollection<MovimentacaoData>();
        private ProdutoData? produtoSelecionado;
        private bool usePositiveNumber = true;
        private static readonly string CollectionName = "produtos";

        // Construtor da classe que inicializa os componentes e carrega os produtos
        public AddEntradaSaídaUserControl()
        {
            InitializeComponent();
            Setup();

            // Vincular a coleção listaMovimentacoes ao ItemsControl na interface do usuário
            ListaItemsControl.ItemsSource = listaMovimentacoes;



            ProdutoComboBox.Focus();

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

            // Desabilitar ou habilitar o ComboBox
            ProdutoComboBox.IsHitTestVisible = !isVisible;
            ProdutoComboBox.IsEnabled = !isVisible;
        }

        // Método chamado quando o tipo de movimentação é alterado
        private void TipoMovimentacaoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TipoMovimentacaoComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string tipo = selectedItem.Content.ToString();
                usePositiveNumber = tipo == "Entrada";

                // Atualizar visibilidade dos elementos
                ProdutoAntesDepois.Visibility = Visibility.Visible;
            }
        }


        // Produto ComboBox
        // Método que é chamado quando o texto da caixa de pesquisa é alterado
        private void ProdutoComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Template.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
            {
                string searchText = textBox.Text.ToLower();

                // Filtrar os produtos com base no texto digitado
                var filteredProducts = produtos
                    .Where(p => p.Nome.ToLower().Contains(searchText))
                    .Select(p => p.Nome)
                    .ToList();

                // Atualizar os itens do ComboBox
                comboBox.ItemsSource = filteredProducts;

                // Manter o texto digitado
                textBox.Text = searchText;
                textBox.CaretIndex = searchText.Length;

                // Abrir o dropdown para mostrar as opções filtradas
                comboBox.IsDropDownOpen = true;
            }
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
                    AtualizarCamposProduto(produtoSelecionado);
                    DestacarMudancas();
                }
                else
                {
                    MessageBox.Show("Produto não encontrado no cache.");
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
                return true;
            }

            // Realizar cálculos com os valores inseridos
            if (int.TryParse(QuantidadeTextBox.Text, out int quantidadeAlterada) && double.TryParse(PrecoTextBox.Text, out double precoAlterado))
            {
                int quantidadeFinal = usePositiveNumber
                    ? produto.Quantidade + quantidadeAlterada // Entrada
                    : produto.Quantidade - quantidadeAlterada; // Saída

                if (quantidadeFinal < 0)
                {
                    // Quantidade final não pode ser negativa
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
                    // Calcular o preço médio ponderado para entrada
                    double precoAtual = produto.Preco;
                    int quantidadeAtual = produto.Quantidade;
                    int quantidadeNova = quantidadeAlterada;
                    int quantidadeTotal = quantidadeAtual + quantidadeNova;

                    double precoPonderado = ((precoAtual * quantidadeAtual) + (precoAlterado * quantidadeNova)) / quantidadeTotal;
                    PrecoDepoisDadoTextBlock.Text = precoPonderado.ToString("C");
                }
                else
                {
                    // Para saída, mantém o preço atual
                    PrecoDepoisDadoTextBlock.Text = produto.Preco.ToString("C");
                }
            }
            else
            {
                MessageBox.Show("Por favor, insira valores válidos para quantidade e preço.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Atualizar visibilidade do painel de detalhes
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

            QuantidadeDepoisDadoTextBlock.Foreground = QuantidadeDepoisDadoTextBlock.Text != QuantidadeAntesDadoTextBlock.Text
                ? (Brush)FindResource("AccentBrush")
                : (Brush)FindResource("TextBrush");

            PrecoDepoisDadoTextBlock.Foreground = PrecoDepoisDadoTextBlock.Text != PrecoAntesDadoTextBlock.Text
                ? (Brush)FindResource("AccentBrush")
                : (Brush)FindResource("TextBrush");
        }

        // Método para atualizar os detalhes do produto selecionado

        // Métodos para Lista de Compras
        private void ToggleListaCompras_Click(object sender, RoutedEventArgs e)
        {
            // Deixa lista visível
            ListaCompras.Visibility = ListaCompras.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

            // Oculta botão
            ToggleListaCompras.Visibility = ToggleListaCompras.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }
        private void AdicionarNaLista_Click(object sender, RoutedEventArgs e)
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
                ProdutoId = produtoSelecionado.Nome, // Exibe o nome do produto na lista
                Data = DateTime.UtcNow,
            };

            listaMovimentacoes.Add(movimentacao);

            // Atualizar a interface do usuário
            ListaItemsControl.ItemsSource = null;
            ListaItemsControl.ItemsSource = listaMovimentacoes;

            // Adiciona animação ao ToggleListaCompras
            AnimateToggleListaCompras();

            LimparCampos();
        }
        private void AnimateToggleListaCompras()
        {
            // Cria uma animação de cor piscando usando o AccentBrush
            ColorAnimation colorAnimation = new ColorAnimation
            {
                From = ((SolidColorBrush)FindResource("PanelBackgroundBrush")).Color,
                To = ((SolidColorBrush)FindResource("AccentBrush")).Color,
                Duration = TimeSpan.FromSeconds(0.3),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(2)
            };

            SolidColorBrush brush = new SolidColorBrush(((SolidColorBrush)FindResource("PanelBackgroundBrush")).Color);
            ToggleListaCompras.Background = brush;

            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation); // Inicia a animação
        }

        private void ExcluirItem_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.DataContext;
            if (item != null)
            {
                var items = ListaItemsControl.ItemsSource as IList;
                if (items != null)
                {
                    items.Remove(item);
                }
            }
        }
        private async void ConfimarPedido_Click(object sender, RoutedEventArgs e)
        {
            foreach (var movimentacao in listaMovimentacoes)
            {
                await RegistrarMovimentacaoAsync(movimentacao.Tipo == "Entrada", movimentacao.Quantidade, movimentacao.Preco);
            }

            // Limpa a lista
            listaMovimentacoes.Clear();

            // Atualizar a interface do usuário
            ListaItemsControl.ItemsSource = null;
            ListaItemsControl.ItemsSource = listaMovimentacoes;

            MessageBox.Show("Movimentações registradas com sucesso!");
        }
        private void FecharListaCompras_Click(object sender, RoutedEventArgs e)
        {
            // Deixa lista invisível
            ListaCompras.Visibility = Visibility.Collapsed;

            // Mostra botão
            ToggleListaCompras.Visibility = Visibility.Visible;
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

                    if (produtoSelecionado != null)
                    {
                        AtualizarCamposProduto(produtoSelecionado);
                        DestacarMudancas();
                    }
                    else
                    {
                        MessageBox.Show("Produto não encontrado.");
                    }
                }
                else
                {
                    MessageBox.Show("Quantidade inválida.");
                    textBox.Clear();
                }
            }
        }
        private void QuantidadeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (produtoSelecionado != null)
            {
                AtualizarCamposProduto(produtoSelecionado);
                DestacarMudancas();
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

                    if (produtoSelecionado != null)
                    {
                        AtualizarCamposProduto(produtoSelecionado);
                        DestacarMudancas();
                    }
                    else
                    {
                        MessageBox.Show("Produto não encontrado.");
                    }
                }
                else
                {
                    MessageBox.Show("Preço inválido.");
                    textBox.Clear();
                }
            }
        }
        private void PrecoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (produtoSelecionado != null)
            {
                AtualizarCamposProduto(produtoSelecionado);
                DestacarMudancas();
            }
        }

    }
}

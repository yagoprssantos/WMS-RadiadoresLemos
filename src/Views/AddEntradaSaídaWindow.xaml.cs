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
using LiteDB;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class AddEntradaSaídaWindow : Window
    {
        // Dados de produtos
        private List<ProdutoData> produtos = new List<ProdutoData>();
        // Dados de movimentações
        private ObservableCollection<MovimentacaoData> listaMovimentacoes = new ObservableCollection<MovimentacaoData>();
        private ProdutoData? produtoSelecionado;
        // Configurações e constantes
        private bool usePositiveNumber = true;
        private static readonly string CollectionName = "produtos";
        // Dados de fornecedores e clientes
        private List<string> fornecedores = new List<string>();
        private List<string> clientes = new List<string>();
        private string? fornecedorSelecionado;
        private string? clienteSelecionado;
        // Dados de pagamento
        private string? formaPagamentoSelecionada;
        private readonly List<string> opcoesFormaPagamento = new()
        {
            "Dinheiro",
            "Cartão de Crédito",
            "Cartão de Débito",
            "Transferência",
            "Boleto",
            "Pix"
        };

        public AddEntradaSaídaWindow()
        {
            InitializeComponent();
            Setup();

            ListaItemsControl.ItemsSource = listaMovimentacoes;

            ProdutoComboBox.Focus();
        }

        public AddEntradaSaídaWindow(bool isEntrada) : this()
        {
            usePositiveNumber = isEntrada;
            if (isEntrada)
            {
                Fornecedor.Visibility= Visibility.Visible;
                Cliente.Visibility= Visibility.Collapsed;
            }
            else
            {
                Fornecedor.Visibility = Visibility.Collapsed;
                Cliente.Visibility = Visibility.Visible;
            }
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


        // Método que é chamado quando o texto da caixa de pesquisa é alterado
        private void ProdutoComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Template.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
            {
                string searchText = textBox.Text;

                // Filtrar os produtos com base no texto digitado (case-insensitive)
                var filteredProducts = produtos
                    .Where(p => p.Nome.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.Nome)
                    .ToList();

                // Evita manipular Items quando ItemsSource está em uso
                comboBox.ItemsSource = null;
                comboBox.Items.Clear();

                // Apresenta apenas os produtos filtrados (com o nome original)
                foreach (var nome in filteredProducts)
                {
                    comboBox.Items.Add(nome);
                }

                // Atualiza o texto da caixa de pesquisa (mantém o texto original digitado)
                textBox.Text = searchText;
                textBox.CaretIndex = textBox.Text.Length;

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

        // Método que é chamado quando o texto da caixa de pesquisa do fornecedor é alterado
        private void FornecedorComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Template.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
            {
                string searchText = textBox.Text;

                var filteredFornecedores = fornecedores
                    .Where(f => f.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

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

            if (!string.IsNullOrEmpty(inputText) && fornecedores.Contains(inputText))
            {
                fornecedorSelecionado = inputText;
            }
            else
            {
                FornecedorComboBox.Text = string.Empty;
                FornecedorComboBox.SelectedItem = null;
                fornecedorSelecionado = null;
            }
        }

        private void FornecedorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FornecedorComboBox.SelectedItem is string selected)
            {
                fornecedorSelecionado = selected;
            }
        }

        // Método que é chamado quando o texto da caixa de pesquisa do cliente é alterado
        private void ClienteComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Template.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
            {
                string searchText = textBox.Text;

                var filteredClientes = clientes
                    .Where(c => c.Contains(searchText, StringComparison.OrdinalIgnoreCase))
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

            if (!string.IsNullOrEmpty(inputText) && clientes.Contains(inputText))
            {
                clienteSelecionado = inputText;
            }
            else
            {
                ClienteComboBox.Text = string.Empty;
                ClienteComboBox.SelectedItem = null;
                clienteSelecionado = null;
            }
        }

        private void ClienteComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClienteComboBox.SelectedItem is string selected)
            {
                clienteSelecionado = selected;
            }
        }

        // Forma de Pagamento ComboBox
        private void FormaPagamentoComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            string inputText = FormaPagamentoComboBox.Text;

            // Seleciona o item se for válido
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

            bool isEntrada = usePositiveNumber;

            if (!ValidarMovimentacao(produtoSelecionado, isEntrada, quantidade, preco))
                return;

            var movimentacao = new MovimentacaoData
            {
                Id = 0,
                Tipo = isEntrada ? "Entrada" : "Saída",
                Quantidade = quantidade,
                Preco = preco,
                ProdutoId = produtoSelecionado.Nome,
                Data = DateTime.UtcNow,
            };

            listaMovimentacoes.Add(movimentacao);

            ListaItemsControl.ItemsSource = null;
            ListaItemsControl.ItemsSource = listaMovimentacoes;

            AnimateToggleListaCompras();
            LimparCampos();
        }
        private bool ValidarMovimentacao(ProdutoData produto, bool isEntrada, int quantidade, double preco)
        {
            if (produto == null)
            {
                MessageBox.Show("Produto não selecionado.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (quantidade <= 0)
            {
                MessageBox.Show("A quantidade deve ser maior que zero.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (preco < 0)
            {
                MessageBox.Show("O preço não pode ser negativo.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (!isEntrada && quantidade > produto.Quantidade)
            {
                MessageBox.Show("Quantidade insuficiente em estoque para saída.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
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
                    Id = 0,
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

                // Atualiza o produto no banco de dados usando a função dedicada
                AtualizarProdutoNoBanco(produtoSelecionado, isEntrada, quantidadeMovimentacao, precoMovimentacao);

                LimparCampos();
                await CarregarProdutos();
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

            // Se for entrada,
            if (isEntrada)
            {
                // Atualiza quantidade e calcula novo preço médio ponderado
                double precoTotal = (produto.Preco * produto.Quantidade) + (preco * quantidade);
                int novaQuantidade = produto.Quantidade + quantidade;
                produto.Preco = novaQuantidade > 0 ? precoTotal / novaQuantidade : 0;
                produto.Quantidade = novaQuantidade;
            }
            // Se for saída,
            else
            {
                // Apenas reduz a quantidade, preço permanece
                produto.Quantidade -= quantidade;
                if (produto.Quantidade < 0) produto.Quantidade = 0;
            }

            var produtoCollection = DatabaseConnect.Database.GetCollection<ProdutoData>(CollectionName);
            produtoCollection.Update(produto);
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
        // Parcelas
        private void ParcelasTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }
        private void ParcelasTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
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
        private void ParcelasTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
            {
                if (int.TryParse(textBox.Text, out int parcelas))
                {
                    textBox.Text = parcelas.ToString("N0", new System.Globalization.CultureInfo("pt-BR"));
                }
                else
                {
                    MessageBox.Show("Parcelas inválidas.");
                    textBox.Clear();
                }
            }
        }
        private void ParcelasTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
            {
                if (int.TryParse(textBox.Text, out int parcelas))
                {
                    textBox.Text = parcelas.ToString("N0", new System.Globalization.CultureInfo("pt-BR"));
                }
                else
                {
                    MessageBox.Show("Parcelas inválidas.");
                    textBox.Clear();
                }
            }
        }
    }
}
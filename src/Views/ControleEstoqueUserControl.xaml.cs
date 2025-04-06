using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;
using WMS_RadiadoresLemos_WPF.src.Views;
using System.Text.Json;
using LiteDB;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class ControleEstoqueUserControl : UserControl
    {
        private static readonly string CollectionName = "produtos";
        private List<ProdutoData> produtos = new List<ProdutoData>();
        private bool produtosCarregados = false;
        private bool precisaAtualizarEstoque = true;

        public ControleEstoqueUserControl()
        {
            InitializeComponent();
            AtualizarTabelaEstoque();
        }

        public void AtualizarTabelaEstoque()
        {
            try
            {
                if (DatabaseConnect.Database != null)
                {
                    var collection = DatabaseConnect.Database.GetCollection<ProdutoData>("produtos");
                    var produtos = collection.FindAll().ToList();
                    EstoqueDataGrid.ItemsSource = produtos;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar produtos do banco: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CadastrarProduto_Click(object sender, RoutedEventArgs e)
        {
            var novoProduto = new ProdutoData
            {
                Id = Guid.NewGuid().ToString(),
                Nome = "",
                Codigo = "",
                Marca = "",
                Tipo = "",
                Quantidade = 0,
                Preco = 0
            };

            var window = new EditarProdutoWindow(novoProduto);
            if (window.ShowDialog() == true)
            {
                AtualizarTabelaEstoque();
            }
        }

        private void EditarProduto_Click(object sender, RoutedEventArgs e)
        {
            var produto = (sender as Button)?.DataContext as ProdutoData;
            if (produto == null) return;

            var window = new EditarProdutoWindow(produto);
            if (window.ShowDialog() == true)
            {
                AtualizarTabelaEstoque();
            }
        }

        private void DeletarProduto_Click(object sender, RoutedEventArgs e)
        {
            var produto = (sender as Button)?.DataContext as ProdutoData;
            if (produto == null) return;

            var result = MessageBox.Show(
                $"Tem certeza que deseja deletar o produto {produto.Nome}?",
                "Confirmar exclusão",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (DatabaseConnect.Database == null)
                    {
                        MessageBox.Show("Erro ao conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var collection = DatabaseConnect.Database.GetCollection<ProdutoData>(CollectionName);
                    collection.Delete(produto.Id);
                    AtualizarTabelaEstoque();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao deletar produto: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EstoqueDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var produto = EstoqueDataGrid.SelectedItem as ProdutoData;
            if (produto == null) return;

            var window = new EditarProdutoWindow(produto);
            if (window.ShowDialog() == true)
            {
                AtualizarTabelaEstoque();
            }
        }

        // Método para preencher os filtros de marca e tipo de produto
        private void PreencherFiltros()
        {
            try
            {
                var marcas = produtos.Select(p => p.Marca).Distinct().ToList();
                var tipos = produtos.Select(p => p.Tipo).Distinct().ToList();

                MarcaComboBox.ItemsSource = marcas;
                TipoComboBox.ItemsSource = tipos;
            }
            catch (InvalidOperationException ex)
            {
                Alerta.AdicionarAlerta("Erro",
                    ex.Message.ToString(),
                    "Erro ao preencher filtros de marca e tipo de produto no Controle de Estoque. Possíveis Motivos\n: " +
                    "- Não foi possível carregar os produtos;\n" +
                    "- Filtro de marca ou tipo não encontrado.",
                    "- Verifique se os produtos foram carregados corretamente;\n" +
                    "- Verifique se os filtros de marca e tipo existem;\n" +
                    "- Tente atualizar a tabela de estoque novamente.");
            }
        }

        // Método chamado ao alterar o texto da caixa de busca
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!produtosCarregados)
            {
                // Garante que produtos estejam sempre carregados
                AtualizarTabelaEstoqueCache();
            }
        }

        // Método para atualizar a tabela de estoque com os produtos do cache
        private void AtualizarTabelaEstoqueCache()
        {
            try
            {
                if (DatabaseConnect.Database == null)
                {
                    MessageBox.Show("Erro ao conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var collection = DatabaseConnect.Database.GetCollection<ProdutoData>(CollectionName);
                produtos = collection.FindAll().ToList();
                EstoqueDataGrid.ItemsSource = produtos;
                produtosCarregados = true;
                precisaAtualizarEstoque = false;
            }
            catch (Exception ex)
            {
                precisaAtualizarEstoque = true;
                MessageBox.Show($"Erro ao carregar produtos do cache: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método chamado ao clicar no botão de filtrar
        private void FiltrarButton_Click(object sender, RoutedEventArgs e)
        {
            FiltroPopup.IsOpen = true;
        }

        // Método chamado ao clicar no botão de aplicar filtro
        private void AplicarFiltroButton_Click(object sender, RoutedEventArgs e)
        {
            string produto = ProdutoComboBox.SelectedItem?.ToString();
            string tipo = TipoComboBox.SelectedItem?.ToString();
            string marca = MarcaComboBox.SelectedItem?.ToString();
            string codigo = CodigoComboBox.SelectedItem?.ToString();
            bool emEstoque = EmEstoqueCheckBox.IsChecked == true;

            AplicarFiltro(produto, tipo, marca, codigo, emEstoque);
            FiltroPopup.IsOpen = false;
        }

        // Método para aplicar os filtros na tabela de estoque
        private void AplicarFiltro(string produto, string tipo, string marca, string codigo, bool emEstoque)
        {
            try
            {
                var produtosFiltrados = produtos.Where(p =>
                    (string.IsNullOrEmpty(produto) || p.Nome.Contains(produto, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(tipo) || p.Tipo == tipo) &&
                    (string.IsNullOrEmpty(marca) || p.Marca == marca) &&
                    (string.IsNullOrEmpty(codigo) || p.Codigo.Contains(codigo, StringComparison.OrdinalIgnoreCase)) &&
                    (!emEstoque || p.Quantidade > 0)).ToList();

                EstoqueDataGrid.ItemsSource = produtosFiltrados;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao aplicar filtro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Evento para limpar os filtros
        private void LimparFiltroButton_Click(object sender, RoutedEventArgs e)
        {
            ProdutoComboBox.SelectedItem = null;
            TipoComboBox.SelectedItem = null;
            MarcaComboBox.SelectedItem = null;
            CodigoComboBox.SelectedItem = null;
            EmEstoqueCheckBox.IsChecked = false;

            // Recarregar todos os produtos
            AtualizarTabelaEstoque();
            FiltroPopup.IsOpen = false;
        }

        // Método chamado ao clicar no botão de alterar quantidade
        private async void AlterarQuantidade_Click(object sender, RoutedEventArgs e)
        {
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                AlterarQuantidadeWindow alterarQuantidadeWindow = new(produtoSelecionado);
                if (alterarQuantidadeWindow.ShowDialog() == true)
                {
                    // Obtém nova quantidade do produto
                    produtoSelecionado.Quantidade = alterarQuantidadeWindow.Quantidade;

                    // Atualiza o produto no banco de dados
                    await AtualizarProduto(produtoSelecionado);

                    // Atualiza a fonte de dados do DataGrid
                    EstoqueDataGrid.ItemsSource = null;
                    EstoqueDataGrid.ItemsSource = produtos;

                    // Avisa o usuário que a quantidade foi alterada
                    MessageBox.Show("Quantidade alterada com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // Método para atualizar um produto
        private async Task AtualizarProduto(ProdutoData produto)
        {
            try
            {
                if (DatabaseConnect.Database == null)
                {
                    MessageBox.Show("Erro ao conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var collection = DatabaseConnect.Database.GetCollection<ProdutoData>(CollectionName);
                collection.Update(produto);

                // Recarrega a lista de produtos
                AtualizarTabelaEstoque();
            }
            catch (Exception ex)
            {
                Alerta.AdicionarAlerta("Erro",
                    ex.Message.ToString(),
                    "Não foi possível atualizar o produto. Possíveis motivos:\n" +
                    "- Problemas de conexão com o banco;\n" +
                    "- Dados corrompidos;\n" +
                    "- Falha na operação de atualização.",
                    "- Verifique a conexão com o banco;\n" +
                    "- Tente novamente mais tarde.");
            }
        }

        // Tratamento de entradas

        // Quantidade
        // Método para validar a entrada de texto no TextBox de quantidade inicial
        private void QuantidadeInicial_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !IsTextAllowed(e.Text, "[^0-9]+");

        // Método para validar a colagem de texto no TextBox de quantidade inicial
        private void QuantidadeInicial_Pasting(object sender, DataObjectPastingEventArgs e) =>
            HandlePasting(e, "[^0-9]+");

        // Método para formatar o texto da caixa de quantidade ao perder o foco (1.000)
        private void QuantidadeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
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

        // Marca
        // Método para validar a entrada de texto no TextBox de marca do produto
        private void MarcaProduto_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !IsTextAllowed(e.Text, "[^a-zA-Z ]+");

        // Método para validar a colagem de texto no TextBox de marca do produto
        private void MarcaProduto_Pasting(object sender, DataObjectPastingEventArgs e) =>
            HandlePasting(e, "[^a-zA-Z ]+");

        // Preço
        // Método para validar a entrada de texto no TextBox de preço do produto (incluindo decimais e uma única vírgula)
        private void PrecoProduto_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !IsTextAllowed(e.Text, "[^0-9]+");

        // Método para validar a colagem de texto no TextBox de preço do produto (incluindo decimais e uma única vírgula)
        private void PrecoProduto_Pasting(object sender, DataObjectPastingEventArgs e) =>
            HandlePasting(e, "[^0-9]+");

        // Método para formatar o texto da caixa de preço ao perder o foco (1.000,00)
        private void PrecoTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (double.TryParse(textBox.Text.Trim().Replace(".", "").Replace(",", "."), out double preco))
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

        // Verifica se o texto é permitido baseado no padrão regex
        private static bool IsTextAllowed(string text, string pattern) =>
            !new Regex(pattern).IsMatch(text);

        // Método para lidar com a colagem de texto e validar se é permitido
        private static void HandlePasting(DataObjectPastingEventArgs e, string pattern)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextAllowed(text, pattern))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}
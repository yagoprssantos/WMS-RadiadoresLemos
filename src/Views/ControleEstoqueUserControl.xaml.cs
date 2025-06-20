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

        // Método para apresentar a tabela de estoque atualizada
        public void AtualizarTabelaEstoque()
        {
            try
            {
                if (DatabaseConnect.Database != null)
                {
                    var collection = DatabaseConnect.Database.GetCollection<ProdutoData>("produtos");
                    produtos = collection.FindAll().ToList();
                    EstoqueDataGrid.ItemsSource = produtos;
                    produtosCarregados = true;

                    // Preencher os filtros após carregar os produtos
                    PreencherFiltros();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar produtos do banco: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para preencher os filtros
        private void PreencherFiltros()
        {
            try
            {
                if (produtos != null && produtos.Any())
                {
                    var marcas = produtos.Select(p => p.Marca).Where(m => !string.IsNullOrEmpty(m)).Distinct().ToList();
                    var tipos = produtos.Select(p => p.Tipo).Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList();
                    var codigos = produtos.Select(p => p.Codigo).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
                    var nomes = produtos.Select(p => p.Nome).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();

                    MarcaComboBox.ItemsSource = marcas;
                    TipoComboBox.ItemsSource = tipos;
                    CodigoComboBox.ItemsSource = codigos;
                    ProdutoComboBox.ItemsSource = nomes;
                }
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


        // Botões de ação
        private void CadastrarProduto_Click(object sender, RoutedEventArgs e)
        {
            // Chamar a janela de cadastro de produto
            var window = new EditarProdutoWindow(null);
            if (window.ShowDialog() == true)
            {
                // Atualizar a tabela de estoque após o cadastro
                AtualizarTabelaEstoque();
            }
        }

        private void EditarProduto_Click(object sender, RoutedEventArgs e)
        {
            // Chama janela com produto selecionado
            var produto = EstoqueDataGrid.SelectedItem as ProdutoData;
            if (produto == null)
            {
                MessageBox.Show("Selecione um produto para editar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var window = new EditarProdutoWindow(produto);
            if (window.ShowDialog() == true)
            {
                AtualizarTabelaEstoque();
            }
        }

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
                    var index = produtos.FindIndex(p => p.Id == produtoSelecionado.Id);
                    if (index >= 0)
                    {
                        produtos[index] = produtoSelecionado;
                        EstoqueDataGrid.ItemsSource = null;
                        EstoqueDataGrid.ItemsSource = produtos;
                    }

                    // Avisa o usuário que a quantidade foi alterada
                    MessageBox.Show("Quantidade alterada com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
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

        private void DeletarProduto_Click(object sender, RoutedEventArgs e)
        {
            var produto = EstoqueDataGrid.SelectedItem as ProdutoData;
            if (produto == null)
            {
                MessageBox.Show("Selecione um produto para deletar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Solicita a senha
            var confirmarSenhaWindow = new ConfirmarSenhaWindow();
            confirmarSenhaWindow.ShowDialog();

            if (!confirmarSenhaWindow.IsConfirmed)
            {
                return;
            }

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

        // Métodos diversos
        // Método chamado ao alterar o texto da caixa de busca
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (produtosCarregados)
            {
                string searchText = SearchBox.Text.ToLower();

                // 1. Filtra para apresentar produtos que contenham o texto de busca (de qualquer parte do nome, tipo, marca ou código)
                var produtosFiltrados = produtos.Where(p =>
                    p.Nome.ToLower().Contains(searchText) ||
                    p.Tipo.ToLower().Contains(searchText) ||
                    p.Marca.ToLower().Contains(searchText) ||
                    p.Codigo.ToLower().Contains(searchText)).ToList();

                // 2. Reordena os produtos filtrados para apresentar itens com o começo dos campos mais próximo do texto de busca
                produtosFiltrados = produtosFiltrados.OrderBy(p =>
                    p.Nome.ToLower().StartsWith(searchText) ? 0 :
                    p.Tipo.ToLower().StartsWith(searchText) ? 1 :
                    p.Marca.ToLower().StartsWith(searchText) ? 2 :
                    p.Codigo.ToLower().StartsWith(searchText) ? 3 : 4).ToList();

                EstoqueDataGrid.ItemsSource = produtosFiltrados;
            }
        }

        // Filtro
        private void FiltrarButton_Click(object sender, RoutedEventArgs e)
        {
            FiltroPopup.IsOpen = true;
        }
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


        // Método para lidar com o duplo clique em um item no DataGrid
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
    }
}
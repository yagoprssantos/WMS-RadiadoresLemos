using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WMS_RadiadoresLemos_WPF.Classes;
using Google.Cloud.Firestore;
using System.Windows.Media;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class ControleEstoqueUserControl : UserControl
    {
        // Lista para armazenar os produtos carregados do banco de dados
        private readonly List<ProdutoData> produtos = new();
        // Flag para verificar se os produtos já foram carregados
        private bool produtosCarregados = false;
        // Flag para verificar se a tabela de estoque precisa ser atualizada
        private bool precisaAtualizarEstoque = true;

        public ControleEstoqueUserControl()
        {
            InitializeComponent();
            CarregarDadosIniciais();
        }

        // Método para carregar os dados iniciais
        private async void CarregarDadosIniciais()
        {
            await AtualizarTabelaEstoque();
        }

        // Método para atualizar a tabela de estoque com os produtos do banco de dados
        private async Task AtualizarTabelaEstoque()
        {
            try
            {
                // Se os produtos já foram carregados e não precisam ser atualizados, não faz nada
                if (produtosCarregados && !precisaAtualizarEstoque) return;

                // Limpa a lista de produtos
                produtos.Clear();
                // Conecta ao banco de dados
                var db = DatabaseConnect.Database;
                if (db == null) throw new InvalidOperationException("Conexão com o banco de dados não estabelecida.");
                var produtosRef = db.Collection("Produtos");
                // Obtém o snapshot dos documentos na coleção "Produtos"
                var snapshot = await produtosRef.GetSnapshotAsync();

                // Converte cada documento para ProdutoData e adiciona à lista de produtos
                foreach (var doc in snapshot.Documents)
                {
                    var produto = doc.ConvertTo<ProdutoData>();
                    produtos.Add(produto);
                }

                // Atualiza a fonte de dados do DataGrid
                EstoqueDataGrid.ItemsSource = null;
                EstoqueDataGrid.ItemsSource = produtos;
                // Marca que os produtos foram carregados e não precisam ser atualizados
                produtosCarregados = true;
                precisaAtualizarEstoque = false;
            }
            catch (Exception ex)
            {
                // Exibe mensagem de erro caso ocorra uma exceção
                MessageBox.Show($"Erro ao atualizar a tabela de estoque: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método chamado quando um TextBox ganha foco
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && IsPlaceholderText(textBox.Text))
            {
                textBox.Text = string.Empty;
            }
        }

        // Método chamado quando um TextBox perde foco
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = GetPlaceholderText(textBox.Name);
            }
        }

        // Verifica se o texto é um texto de placeholder
        private static bool IsPlaceholderText(string text) =>
            text is "Nome do Produto" or "Tipo do Produto" or "Marca do Produto" or "Quantidade";

        // Retorna o texto de placeholder baseado no nome do TextBox
        private static string GetPlaceholderText(string textBoxName) => textBoxName switch
        {
            "NomeProduto" => "Nome do Produto",
            "TipoProduto" => "Tipo do Produto",
            "MarcaProduto" => "Marca do Produto",
            "QuantidadeInicial" => "Quantidade",
            _ => string.Empty
        };

        // Método para validar a entrada de texto no TextBox de quantidade inicial
        private void QuantidadeInicial_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !IsTextAllowed(e.Text, "[^0-9]+");

        // Método para validar a colagem de texto no TextBox de quantidade inicial
        private void QuantidadeInicial_Pasting(object sender, DataObjectPastingEventArgs e) =>
            HandlePasting(e, "[^0-9]+");

        // Método para validar a entrada de texto no TextBox de marca do produto
        private void MarcaProduto_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !IsTextAllowed(e.Text, "[^a-zA-Z ]+");

        // Método para validar a colagem de texto no TextBox de marca do produto
        private void MarcaProduto_Pasting(object sender, DataObjectPastingEventArgs e) =>
            HandlePasting(e, "[^a-zA-Z ]+");

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

        // Método para obter os dados do produto a partir dos TextBoxes
        private ProdutoData DadosDoProduto() => new()
        {
            Nome = NomeProduto.Text.Trim(),
            Tipo = TipoProduto.Text.Trim(),
            Marca = MarcaProduto.Text.Trim(),
            Codigo = CodigoProduto.Text.Trim(),
            Quantidade = int.Parse(QuantidadeInicial.Text.Trim())
        };

        // Método para cadastrar um novo produto no banco de dados
        private async void CadastrarProdutoNoBanco()
        {
            var db = DatabaseConnect.Database;
            if (db == null) throw new InvalidOperationException("Conexão com o banco de dados não estabelecida.");
            var data = DadosDoProduto();
            var docRef = db.Collection("Produtos").Document(data.Codigo);
            await docRef.SetAsync(data);
            // Marca que a tabela de estoque precisa ser atualizada
            precisaAtualizarEstoque = true;
        }

        // Método chamado ao clicar no botão de cadastrar produto
        private async void CadastrarProduto_Click(object sender, RoutedEventArgs e)
        {
            if (CamposPreenchidos())
            {
                CadastrarProdutoNoBanco();
                await AtualizarTabelaEstoque();
                MessageBox.Show("Produto cadastrado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                LimparCamposCadastro();
            }
            else
            {
                MessageBox.Show("Preencha todos os campos para cadastrar o produto.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Verifica se todos os campos necessários estão preenchidos
        private bool CamposPreenchidos() =>
            !string.IsNullOrEmpty(NomeProduto.Text) &&
            !string.IsNullOrEmpty(TipoProduto.Text) &&
            !string.IsNullOrEmpty(MarcaProduto.Text) &&
            !string.IsNullOrEmpty(CodigoProduto.Text) &&
            !string.IsNullOrEmpty(QuantidadeInicial.Text);

        // Método para limpar os campos de cadastro
        private void LimparCamposCadastro()
        {
            NomeProduto.Text = string.Empty;
            TipoProduto.Text = string.Empty;
            MarcaProduto.Text = string.Empty;
            CodigoProduto.Text = string.Empty;
            QuantidadeInicial.Text = string.Empty;
        }

        // Método chamado ao carregar a aba de estoque
        private async void AbaEstoque_Loaded(object sender, RoutedEventArgs e)
        {
            await AtualizarTabelaEstoque();
        }

        // Método chamado ao alterar o texto da caixa de busca
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!produtosCarregados)
            {
                await AtualizarTabelaEstoque();
            }

            string searchText = SearchBox.Text.ToLower();
            var filteredProducts = produtos.Where(p =>
                p.Nome.ToLower().Contains(searchText) ||
                p.Tipo.ToLower().Contains(searchText) ||
                p.Marca.ToLower().Contains(searchText) ||
                p.Codigo.ToLower().Contains(searchText)).ToList();

            EstoqueDataGrid.ItemsSource = filteredProducts;
        }

        // Método chamado ao clicar no botão de editar produto
        private async void EditarProduto_Click(object sender, RoutedEventArgs e)
        {
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                EditarProdutoWindow editarProdutoWindow = new EditarProdutoWindow(produtoSelecionado);
                if (editarProdutoWindow.ShowDialog() == true)
                {
                    // Atualiza o produto na lista local
                    var produtoEditado = editarProdutoWindow.Produto;
                    var index = produtos.FindIndex(p => p.Id == produtoEditado.Id);
                    if (index >= 0)
                    {
                        produtos[index] = produtoEditado;
                    }

                    // Atualiza o banco de dados
                    await AtualizarProdutoNoBanco(produtoEditado);

                    // Atualiza a fonte de dados do DataGrid
                    EstoqueDataGrid.ItemsSource = null;
                    EstoqueDataGrid.ItemsSource = produtos;
                }
            }
            else
            {
                MessageBox.Show("Selecione um produto para editar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Método para atualizar um produto no banco de dados
        private async Task AtualizarProdutoNoBanco(ProdutoData produto)
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db == null) throw new InvalidOperationException("Conexão com o banco de dados não estabelecida.");

                DocumentReference docRef = db.Collection("Produtos").Document(produto.Id);
                await docRef.SetAsync(produto, SetOptions.Overwrite);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar produto no banco de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método chamado ao clicar no botão de atualizar tabela de estoque
        private async void AtualizarDataGrid_Click(object sender, RoutedEventArgs e)
        {
            await AtualizarTabelaEstoque();
            MessageBox.Show("Tabela de estoque atualizada.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Método chamado ao clicar no botão de alterar quantidade
        private async void AlterarQuantidade_Click(object sender, RoutedEventArgs e)
        {
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                var alterarQuantidadeWindow = new AlterarQuantidadeWindow(produtoSelecionado);
                if (alterarQuantidadeWindow.ShowDialog() == true)
                {
                    produtoSelecionado.Quantidade = alterarQuantidadeWindow.Quantidade;
                    await AtualizarProdutoNoBanco(produtoSelecionado);

                    // Avisa o usuário que a quantidade foi alterada
                    MessageBox.Show("Quantidade alterada com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Marca que a tabela de estoque precisa ser atualizada
                    precisaAtualizarEstoque = true;
                    await AtualizarTabelaEstoque();
                }
            }
        }

        // Método chamado ao clicar no botão de deletar produto
        private async void DeletarProduto_Click(object sender, RoutedEventArgs e)
        {
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                // Exibe confirmação
                var result = MessageBox.Show("Tem certeza que deseja deletar este produto?", "Confirmação", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    var db = DatabaseConnect.Database;
                    if (db == null) throw new InvalidOperationException("Conexão com o banco de dados não estabelecida.");
                    var docRef = db.Collection("Produtos").Document(produtoSelecionado.Codigo);
                    await docRef.DeleteAsync();

                    // Avisa o usuário que o produto foi deletado
                    MessageBox.Show("Produto deletado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Marca que a tabela de estoque precisa ser atualizada
                    precisaAtualizarEstoque = true;
                    await AtualizarTabelaEstoque();
                }
             }
        }

        // Método que atualiza tabela de estoque ao trocar o TabItem
        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tabControl && tabControl.SelectedItem is TabItem tabItem && tabItem.Header.ToString() == "Estoque")
            {
                // Caso o TabItem selecionado seja o de estoque, atualiza a tabela de estoque
                if (produtosCarregados && precisaAtualizarEstoque)
                {
                    await AtualizarTabelaEstoque();
                }
            }
        }
    }
}

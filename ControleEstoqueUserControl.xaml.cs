using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WMS_RadiadoresLemos_WPF.Classes;
using Google.Cloud.Firestore;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class ControleEstoqueUserControl : UserControl
    {
        // Lista para armazenar os produtos carregados do banco de dados
        private List<ProdutoData> produtos = [];
        // Flag para verificar se os produtos já foram carregados
        private bool produtosCarregados = false;
        // Flag para verificar se a tabela de estoque precisa ser atualizada
        private bool precisaAtualizarEstoque = true;

        public ControleEstoqueUserControl()
        {
            InitializeComponent();
            CarregarDadosIniciais();
        }

        private void CarregarDadosIniciais()
        {
            if (Cache.Tabelas.TryGetValue("Produtos", out List<object>? value))
            {
                produtos = value.Cast<ProdutoData>().ToList();
                EstoqueDataGrid.ItemsSource = produtos;
            }
        }

        // Método para atualizar a tabela de estoque com os produtos do cache
        private void AtualizarTabelaEstoqueCache()
        {
            if (Cache.Tabelas.TryGetValue("Produtos", out List<object>? value))
            {
                produtos = value.Cast<ProdutoData>().ToList();
                EstoqueDataGrid.ItemsSource = produtos;
                produtosCarregados = true;
                precisaAtualizarEstoque = false;
            }
        }

        // Método para atualizar a tabela de estoque com os produtos do banco de dados
        private async Task AtualizarTabelaEstoqueBanco()
        {
            try
            {
                var db = DatabaseConnect.Database ?? throw new InvalidOperationException("Conexão com o banco de dados não estabelecida.");
                var produtosSnapshot = await db.Collection("Produtos").GetSnapshotAsync();
                produtos = produtosSnapshot.Documents.Select(doc =>
                {
                    var produto = doc.ConvertTo<ProdutoData>();
                    produto.Id = doc.Id;
                    return produto;
                }).ToList();

                Cache.Tabelas["Produtos"] = produtos.Cast<object>().ToList();
                EstoqueDataGrid.ItemsSource = produtos;
                produtosCarregados = true;
                precisaAtualizarEstoque = false;
            }
            catch (Exception ex)
            {
                precisaAtualizarEstoque = true;
                MessageBox.Show($"Erro ao carregar produtos do banco de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (DatabaseConnect.Database == null)
            {
                MessageBox.Show("Conexão com o banco de dados não estabelecida.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var db = DatabaseConnect.Database;
            var data = DadosDoProduto();
            var docRef = db.Collection("Produtos").Document(data.Codigo);
            await docRef.SetAsync(data);

            // Atualiza o cache local
            if (!Cache.Tabelas.TryGetValue("Produtos", out List<object>? value))
            {
                value = [];
                Cache.Tabelas["Produtos"] = value;
            }

            value.Add(data);
            produtos.Add(data);
            EstoqueDataGrid.ItemsSource = null;
            EstoqueDataGrid.ItemsSource = produtos;
        }

        // Método chamado ao clicar no botão de cadastrar produto
        private void CadastrarProduto_Click(object sender, RoutedEventArgs e)
        {
            if (CamposPreenchidos())
            {
                if (!precisaAtualizarEstoque)
                {
                    CadastrarProdutoNoBanco();
                    MessageBox.Show("Produto cadastrado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    LimparCamposCadastro();
                }
                else
                {
                    MessageBox.Show("Não é possível cadastrar o produto. Atualize a tabela de estoque primeiro.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
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
        private void AbaEstoque_Loaded(object sender, RoutedEventArgs e)
        {
            AtualizarTabelaEstoqueCache();
        }

        // Método chamado ao alterar o texto da caixa de busca
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!produtosCarregados)
            {
                AtualizarTabelaEstoqueCache();
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
                EditarProdutoWindow editarProdutoWindow = new(produtoSelecionado);
                if (editarProdutoWindow.ShowDialog() == true)
                {
                    // Atualiza o produto na lista local
                    var produtoEditado = editarProdutoWindow.Produto;
                    var index = produtos.FindIndex(p => p.Id == produtoEditado.Id);
                    if (index >= 0)
                    {
                        produtos[index] = produtoEditado;
                    }

                    // Atualiza o cache local
                    Cache.Tabelas["Produtos"] = produtos.Cast<object>().ToList();

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
        private static async Task AtualizarProdutoNoBanco(ProdutoData produto)
        {
            try
            {
                var db = DatabaseConnect.Database ?? throw new InvalidOperationException("Conexão com o banco de dados não estabelecida.");

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
            await AtualizarTabelaEstoqueBanco();
            MessageBox.Show("Tabela de estoque atualizada.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Método chamado ao clicar no botão de alterar quantidade
        private async void AlterarQuantidade_Click(object sender, RoutedEventArgs e)
        {
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                AlterarQuantidadeWindow alterarQuantidadeWindow = new(produtoSelecionado);
                if (alterarQuantidadeWindow.ShowDialog() == true)
                {
                    produtoSelecionado.Quantidade = alterarQuantidadeWindow.Quantidade;
                    await AtualizarProdutoNoBanco(produtoSelecionado);

                    // Atualiza o cache local
                    Cache.Tabelas["Produtos"] = produtos.Cast<object>().ToList();

                    // Avisa o usuário que a quantidade foi alterada
                    MessageBox.Show("Quantidade alterada com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Atualiza a fonte de dados do DataGrid
                    EstoqueDataGrid.ItemsSource = null;
                    EstoqueDataGrid.ItemsSource = produtos;
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
                    // Atualiza a lista e Cache local
                    produtos.Remove(produtoSelecionado);
                    Cache.Tabelas["Produtos"] = produtos.Cast<object>().ToList();

                    // Deleta o produto do banco de dados
                    await DeletarProdutoNoBanco(produtoSelecionado);

                    // Atualiza a fonte de dados do DataGrid
                    EstoqueDataGrid.ItemsSource = null;
                    EstoqueDataGrid.ItemsSource = produtos;

                    MessageBox.Show("Produto deletado com sucesso", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Selecione um produto para deletar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Método para deletar um produto no banco de dados
        private async Task DeletarProdutoNoBanco(ProdutoData produto)
        {
            try
            {
                var db = DatabaseConnect.Database ?? throw new InvalidOperationException("Conexão com o banco de dados não estabelecida.");
                DocumentReference docRef = db.Collection("Produtos").Document(produto.Id);
                await docRef.DeleteAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao deletar produto no banco de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

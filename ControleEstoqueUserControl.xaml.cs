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

        public ControleEstoqueUserControl()
        {
            InitializeComponent();
        }

        // Método para atualizar a tabela de estoque com os produtos do banco de dados
        private async void AtualizarTabelaEstoque()
        {
            try
            {
                // Se os produtos já foram carregados, não faz nada
                if (produtosCarregados) return;

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
                // Marca que os produtos foram carregados
                produtosCarregados = true;
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
        }

        // Método chamado ao clicar no botão de cadastrar produto
        private void CadastrarProduto_Click(object sender, RoutedEventArgs e)
        {
            if (CamposPreenchidos())
            {
                CadastrarProdutoNoBanco();
                AtualizarTabelaEstoque();
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
        private void AbaEstoque_Loaded(object sender, RoutedEventArgs e)
        {
            if (!produtosCarregados)
            {
                AtualizarTabelaEstoque();
            }
        }

        // Método chamado ao alterar o texto da caixa de busca
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!produtosCarregados)
            {
                AtualizarTabelaEstoque();
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
                var editarProdutoWindow = new EditarProdutoWindow(produtoSelecionado);
                if (editarProdutoWindow.ShowDialog() == true)
                {
                    await AtualizarProdutoNoBanco(editarProdutoWindow.Produto);
                    AtualizarTabelaEstoque();
                    MessageBox.Show("Produto atualizado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                var editarProdutoWindow = new EditarProdutoWindow();
                if (editarProdutoWindow.ShowDialog() == true)
                {
                    CadastrarProdutoNoBanco();
                    AtualizarTabelaEstoque();
                    MessageBox.Show("Produto cadastrado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    LimparCamposCadastro();
                }
            }
        }

        // Método para atualizar um produto no banco de dados
        private async Task AtualizarProdutoNoBanco(ProdutoData produto)
        {
            var db = DatabaseConnect.Database;
            if (db == null) throw new InvalidOperationException("Conexão com o banco de dados não estabelecida.");
            var docRef = db.Collection("Produtos").Document(produto.Id);
            await docRef.SetAsync(produto, SetOptions.Overwrite);
        }

        // Método chamado quando a seleção do TabControl é alterada
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tabControl)
            {
                AtualizarTabelaEstoque();
            }
        }


        // Método chamado quando a seleção do ComboBox de Tipo de Produto é alterada
        private void TipoProduto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lógica para lidar com a mudança de seleção do tipo de produto
            // Por exemplo, você pode querer filtrar os produtos exibidos com base no tipo selecionado
        }

        // Método chamado ao clicar no botão de alterar quantidade
        private async void AlterarQuantidade_Click(object sender, RoutedEventArgs e)
        {
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                var alterarQuantidadeWindow = new AlterarQuantidadeWindow(produtoSelecionado.Quantidade);
                if (alterarQuantidadeWindow.ShowDialog() == true)
                {
                    produtoSelecionado.Quantidade = alterarQuantidadeWindow.Quantidade;
                    await AtualizarProdutoNoBanco(produtoSelecionado);
                    AtualizarTabelaEstoque();
                    MessageBox.Show("Quantidade do produto alterada com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // Método chamado ao clicar no botão de deletar produto
        private async void DeletarProduto_Click(object sender, RoutedEventArgs e)
        {
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                var result = MessageBox.Show("Tem certeza que deseja deletar este produto?", "Confirmação", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    var db = DatabaseConnect.Database;
                    if (db == null) throw new InvalidOperationException("Conexão com o banco de dados não estabelecida.");
                    var docRef = db.Collection("Produtos").Document(produtoSelecionado.Id);
                    await docRef.DeleteAsync();
                    AtualizarTabelaEstoque();
                    MessageBox.Show("Produto deletado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // Método chamado ao clicar no botão de incrementar quantidade
        private async void IncrementarQuantidade_Click(object sender, RoutedEventArgs e)
        {
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                produtoSelecionado.Quantidade++;
                await AtualizarProdutoNoBanco(produtoSelecionado);
                AtualizarTabelaEstoque();
            }
        }

        // Método chamado ao clicar no botão de decrementar quantidade
        private async void DecrementarQuantidade_Click(object sender, RoutedEventArgs e)
        {
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                if (produtoSelecionado.Quantidade > 0)
                {
                    produtoSelecionado.Quantidade--;
                    await AtualizarProdutoNoBanco(produtoSelecionado);
                    AtualizarTabelaEstoque();
                }
            }
        }
    }
}

using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using WMS_RadiadoresLemos_WPF.Classes;
using Google.Cloud.Firestore;
using System.Threading.Tasks;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class ControleEstoqueUserControl : UserControl
    {
        private List<ProdutoData> produtos = new List<ProdutoData>();

        public ControleEstoqueUserControl()
        {
            InitializeComponent();
            CarregarProdutosAsync(); // Agora é uma função assíncrona
        }

        // Função para atualizar a tabela de estoque
        private async void AtualizarTabelaEstoqueAsync()
        {
            try
            {
                produtos.Clear();
                var db = DatabaseConnect.Database;
                var produtosRef = db.Collection("Produtos");
                var snapshot = await produtosRef.GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    var produto = doc.ConvertTo<ProdutoData>();
                    produtos.Add(produto);
                }

                EstoqueDataGrid.ItemsSource = null;
                EstoqueDataGrid.ItemsSource = produtos;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar a tabela de estoque: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Função foco e perda de foco dos TextBoxes
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && (textBox.Text == "Nome do Produto" || textBox.Text == "Tipo do Produto" || textBox.Text == "Marca do Produto" || textBox.Text == "Quantidade"))
            {
                textBox.Text = string.Empty;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrEmpty(textBox.Text))
            {
                switch (textBox.Name)
                {
                    case "NomeProduto":
                        textBox.Text = "Nome do Produto";
                        break;
                    case "TipoProduto":
                        textBox.Text = "Tipo do Produto";
                        break;
                    case "MarcaProduto":
                        textBox.Text = "Marca do Produto";
                        break;
                    case "QuantidadeInicial":
                        textBox.Text = "Quantidade";
                        break;
                }
            }
        }

        // Restrições de entrada de texto nos TextBoxes
        private void QuantidadeInicial_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text, "[^0-9]+"); // Apenas números
        }

        private void QuantidadeInicial_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            HandlePasting(e, "[^0-9]+"); // Apenas números
        }

        private void MarcaProduto_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text, "[^a-zA-Z ]+"); // Apenas letras e espaços
        }

        private void MarcaProduto_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            HandlePasting(e, "[^a-zA-Z ]+"); // Apenas letras e espaços
        }

        private static bool IsTextAllowed(string text, string pattern)
        {
            return !new Regex(pattern).IsMatch(text);
        }

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

        // Função para cadastrar produto no banco de dados Firestore
        private async Task CadastrarProdutoNoBancoAsync()
        {
            try
            {
                var db = DatabaseConnect.Database;
                var data = DadosDoProduto();
                DocumentReference docRef = db.Collection("Produtos").Document(data.Codigo);
                await docRef.SetAsync(data);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao cadastrar produto: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Função para obter dados do produto dos TextBoxes
        private ProdutoData DadosDoProduto()
        {
            return new ProdutoData
            {
                Nome = NomeProduto.Text.Trim(),
                Tipo = TipoProduto.Text.Trim(),
                Marca = MarcaProduto.Text.Trim(),
                Codigo = CodigoProduto.Text.Trim(),
                Quantidade = int.Parse(QuantidadeInicial.Text.Trim())
            };
        }

        // Botões
        private async void CadastrarProduto_Click(object sender, RoutedEventArgs e)
        {
            if (NomeProduto.Text != "" && TipoProduto.Text != "" && MarcaProduto.Text != "" && CodigoProduto.Text != "" && QuantidadeInicial.Text != "")
            {
                await CadastrarProdutoNoBancoAsync();

                // Atualiza a tabela de estoque
                AtualizarTabelaEstoqueAsync();

                // Registra o evento no Firestore
                await LogEventos.RegistrarEventoAsync($"Produto '{NomeProduto.Text}' cadastrado com sucesso.");

                // Avisa o usuário que o produto foi cadastrado
                MessageBox.Show("Produto cadastrado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

                // Limpa os campos de cadastro
                LimparCamposCadastro();
            }
            else
            {
                MessageBox.Show("Preencha todos os campos para cadastrar o produto.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LimparCamposCadastro()
        {
            NomeProduto.Text = "";
            TipoProduto.Text = "";
            MarcaProduto.Text = "";
            CodigoProduto.Text = "";
            QuantidadeInicial.Text = "";
        }

        // Função que carrega produtos do Firestore
        private async void CarregarProdutosAsync()
        {
            try
            {
                produtos.Clear();
                var db = DatabaseConnect.Database;
                var produtosRef = db.Collection("Produtos");
                var snapshot = await produtosRef.GetSnapshotAsync();
                foreach (var doc in snapshot.Documents)
                {
                    var produto = doc.ConvertTo<ProdutoData>();
                    produtos.Add(produto);
                }
                EstoqueDataGrid.ItemsSource = produtos;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar produtos: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchBox.Text.ToLower();
            var filteredProducts = produtos.Where(p => p.Nome.ToLower().Contains(searchText) ||
                                                       p.Tipo.ToLower().Contains(searchText) ||
                                                       p.Marca.ToLower().Contains(searchText) ||
                                                       p.Codigo.ToLower().Contains(searchText)).ToList();
            EstoqueDataGrid.ItemsSource = filteredProducts;
        }

        private async void EditarProduto_Click(object sender, RoutedEventArgs e)
        {
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                var editarProdutoWindow = new EditarProdutoWindow(produtoSelecionado);
                editarProdutoWindow.ShowDialog();

                if (editarProdutoWindow.DialogResult == true)
                {
                    await AtualizarProdutoNoBancoAsync(produtoSelecionado);
                    AtualizarTabelaEstoqueAsync();
                    MessageBox.Show("Produto atualizado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Selecione um produto para editar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task AtualizarProdutoNoBancoAsync(ProdutoData produto)
        {
            try
            {
                var db = DatabaseConnect.Database;
                await db.Collection("Produtos").Document(produto.Codigo).SetAsync(produto);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar produto: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AlterarQuantidade_Click(object sender, RoutedEventArgs e)
        {
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                var alterarQuantidadeWindow = new AlterarQuantidadeWindow(produtoSelecionado.Quantidade);

                if (alterarQuantidadeWindow.ShowDialog() == true)
                {
                    produtoSelecionado.Quantidade = alterarQuantidadeWindow.Quantidade;
                    await AtualizarProdutoNoBancoAsync(produtoSelecionado);

                    EstoqueDataGrid.Items.Refresh();
                    MessageBox.Show("Quantidade alterada com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Selecione um produto para alterar a quantidade.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void DeletarProduto_Click(object sender, RoutedEventArgs e)
        {
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Tem certeza que deseja deletar o produto '{produtoSelecionado.Nome}'?",
                    "Confirmação de Exclusão",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    produtos.Remove(produtoSelecionado);
                    await DeletarProdutoDoBancoAsync(produtoSelecionado);
                    AtualizarTabelaEstoqueAsync();
                    MessageBox.Show("Produto deletado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Selecione um produto para deletar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task DeletarProdutoDoBancoAsync(ProdutoData produtoSelecionado)
        {
            try
            {
                var db = DatabaseConnect.Database;
                DocumentReference docRef = db.Collection("Produtos").Document(produtoSelecionado.Codigo);
                await docRef.DeleteAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao deletar produto: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TipoProduto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TipoProduto.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedTipo = selectedItem.Content.ToString();
                var filteredProducts = produtos.Where(p => p.Tipo == selectedTipo).ToList();
                EstoqueDataGrid.ItemsSource = filteredProducts;
            }
        }
    }
}

using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using WMS_RadiadoresLemos_WPF.Classes;
using Google.Cloud.Firestore;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class ControleEstoqueUserControl : UserControl
    {
        private List<ProdutoData> produtos = new List<ProdutoData>();

        public ControleEstoqueUserControl()
        {
            InitializeComponent();
            CarregarProdutos();
        }

        // Função para atualizar a tabela de estoque
        private void AtualizarTabelaEstoque()
        {
            produtos.Clear();
            var db = DatabaseConnect.Database;
            var produtosRef = db.Collection("Produtos");
            var snapshot = produtosRef.GetSnapshotAsync().Result;
            foreach (var doc in snapshot.Documents)
            {
                var produto = doc.ConvertTo<ProdutoData>();
                produtos.Add(produto);
            }
            EstoqueDataGrid.ItemsSource = null;
            EstoqueDataGrid.ItemsSource = produtos;
        }

        // Aba de Cadastro de Produtos

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
            if (sender is TextBox textBox && textBox.Text == string.Empty)
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
        // Quantidade inicial
        private void QuantidadeInicial_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text, "[^0-9]+"); // Apenas números
        }
        private void QuantidadeInicial_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            HandlePasting(e, "[^0-9]+"); // Apenas números
        }

        // Nome do Produto
        private void NomeProduto_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text, "[^a-zA-Z ]+"); // Apenas letras e espaços
        }
        private void NomeProduto_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            HandlePasting(e, "[^a-zA-Z ]+"); // Apenas letras e espaços
        }

        // Tipo do Produto
        private void MarcaProduto_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text, "[^a-zA-Z ]+"); // Apenas letras e espaços
        }
        private void MarcaProduto_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            HandlePasting(e, "[^a-zA-Z ]+"); // Apenas letras e espaços
        }

        // Marca do Produto
        private void CodigoProduto_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text, "[^a-zA-Z0-9]+"); // Letras e números
        }
        private void CodigoProduto_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            HandlePasting(e, "[^a-zA-Z0-9]+"); // Letras e números
        }

        // Função para verificar se o texto é permitido
        private static bool IsTextAllowed(string text, string pattern)
        {
            Regex regex = new Regex(pattern);
            return !regex.IsMatch(text);
        }

        // Função para lidar com a colagem de texto
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

        // Puxar produtos preenchidos no banco de dados Firestore
        private ProdutoData DadosDoProduto()
        {
            string nome = NomeProduto.Text.Trim();
            string tipo = TipoProduto.Text.Trim();
            string marca = MarcaProduto.Text.Trim();
            string codigo = CodigoProduto.Text.Trim();
            int quantidade = int.Parse(QuantidadeInicial.Text.Trim());

            return new ProdutoData()
            {
                Nome = nome,
                Tipo = tipo,
                Marca = marca,
                Codigo = codigo,
                Quantidade = quantidade
            };
        }

        // Função cadastrar produto no banco de dados Firestore
        private void CadastrarProdutoNoBanco()
        {
            // Lógica para cadastrar o produto no banco de dados
            // Conecta com o banco de dados Firestore
            var db = DatabaseConnect.Database;
            var data = DadosDoProduto();
            DocumentReference docRef = db.Collection("Produtos").Document(data.Codigo);
            docRef.SetAsync(data);
        }

        // Botões
        private void CadastrarProduto_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para cadastrar o produto no banco de dados
            // Verificar se todos os campos estão preenchidos
            if (NomeProduto.Text != "" && TipoProduto.Text != "" && MarcaProduto.Text != "" && CodigoProduto.Text != "" && QuantidadeInicial.Text != "")
            {
                // Lógica para cadastrar o produto no banco de dados
                CadastrarProdutoNoBanco();

                // Atualiza a tabela de estoque
                AtualizarTabelaEstoque();

                // Avisa o usuário que o produto foi cadastrado
                MessageBox.Show("Produto cadastrado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

                // Limpar os campos após o cadastro
                LimparCamposCadastro();
            }
            else
            {
                // Avisar o usuário para preencher todos os campos
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

        // Aba de Estoque

        // Função que atualiza tabela de estoque
        private void AbaEstoque_Loaded(object sender, RoutedEventArgs e)
        {
            AtualizarTabelaEstoque();
        }


        private void CarregarProdutos()
        {
            // Carrega produtos do banco de dados Firestore
            var db = DatabaseConnect.Database;
            var produtosRef = db.Collection("Produtos");
            var snapshot = produtosRef.GetSnapshotAsync().Result;
            foreach (var doc in snapshot.Documents)
            {
                var produto = doc.ConvertTo<ProdutoData>();
                produtos.Add(produto);
            }
            EstoqueDataGrid.ItemsSource = produtos;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Lógica para filtrar produtos na tabela de estoque, pesquisando por nome, tipo, marca ou código simultaneamente
            string searchText = SearchBox.Text.ToLower();
            var filteredProducts = produtos.Where(p => p.Nome.ToLower().Contains(searchText) ||
                                                       p.Tipo.ToLower().Contains(searchText) ||
                                                       p.Marca.ToLower().Contains(searchText) ||
                                                       p.Codigo.ToLower().Contains(searchText)).ToList();
            EstoqueDataGrid.ItemsSource = filteredProducts;
        }

        private void EditarProduto_Click(object sender, RoutedEventArgs e)
        {
            if (produtos.Any())
            {
                if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
                {
                    var editarProdutoWindow = new EditarProdutoWindow(produtoSelecionado);
                    editarProdutoWindow.ShowDialog();

                    if (editarProdutoWindow.DialogResult == true)
                    {
                        AtualizarProdutoNoBanco(produtoSelecionado);
                        AtualizarTabelaEstoque();
                        MessageBox.Show("Produto atualizado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Selecione um produto para editar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Não há produtos para editar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AtualizarProdutoNoBanco(ProdutoData produto)
        {
            var db = DatabaseConnect.Database;
            db.Collection("Produtos").Document(produto.Codigo).SetAsync(produto);
        }

        private void AlterarQuantidade_Click(object sender, RoutedEventArgs e)
        {
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                // Abre o modal para alterar a quantidade, passando a quantidade atual do produto
                AlterarQuantidadeWindow alterarQuantidadeWindow = new AlterarQuantidadeWindow(produtoSelecionado.Quantidade);

                // Exibe o modal e verifica se o usuário confirmou a alteração
                if (alterarQuantidadeWindow.ShowDialog() == true)
                {
                    // Atualiza a quantidade do produto com o novo valor no banco de dados
                    produtoSelecionado.Quantidade = alterarQuantidadeWindow.Quantidade;
                    AtualizarProdutoNoBanco(produtoSelecionado);

                    // Atualiza a tabela de estoque
                    EstoqueDataGrid.Items.Refresh();
                    
                    MessageBox.Show("Quantidade alterada com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Selecione um produto para alterar a quantidade.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeletarProduto_Click(object sender, RoutedEventArgs e)
        {
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                // Exibe uma janela de confirmação
                MessageBoxResult result = MessageBox.Show(
                    $"Tem certeza que deseja deletar o produto '{produtoSelecionado.Nome}'?",
                    "Confirmação de Exclusão",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                // Se o usuário confirmar a exclusão
                if (result == MessageBoxResult.Yes)
                {
                    // Remove o produto da lista
                    produtos.Remove(produtoSelecionado);

                    // Remove o produto do banco de dados
                    DeletarProdutoDoBanco(produtoSelecionado);

                    // Atualiza o DataGrid para refletir a exclusão
                    AtualizarTabelaEstoque();

                    MessageBox.Show("Produto deletado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Selecione um produto para deletar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeletarProdutoDoBanco(object produtoSelecionado)
        {
            // Encontra o produto selecionado
            var db = DatabaseConnect.Database;
            DocumentReference docRef = db.Collection("Produtos").Document(((ProdutoData)produtoSelecionado).Codigo);

            // Deleta o produto do banco de dados
            docRef.DeleteAsync();

            // Atualiza a tabela
            AtualizarTabelaEstoque();
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

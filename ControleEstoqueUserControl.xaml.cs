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
        private void CarregarProdutos()
        {
            // Carrega produtos do banco de dados Firestore
            // TODO: Implementar lógica para carregar produtos do banco de dados
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
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                // Converte ProdutoData para Produto
                ProdutoData produto = new ProdutoData
                {
                    Nome = produtoSelecionado.Nome,
                    Tipo = produtoSelecionado.Tipo,
                    Marca = produtoSelecionado.Marca,
                    Codigo = produtoSelecionado.Codigo,
                    Quantidade = produtoSelecionado.Quantidade
                };

                // Cria uma nova instância do modal passando o produto convertido
                EditarProdutoWindow editarWindow = new EditarProdutoWindow(produto);

                // Exibe o modal e verifica se o usuário confirmou a edição
                if (editarWindow.ShowDialog() == true)
                {
                    // Atualiza os dados do produto selecionado com os novos valores
                    produtoSelecionado.Nome = editarWindow.ProdutoEditado.Nome;
                    produtoSelecionado.Tipo = editarWindow.ProdutoEditado.Tipo;
                    produtoSelecionado.Marca = editarWindow.ProdutoEditado.Marca;
                    produtoSelecionado.Codigo = editarWindow.ProdutoEditado.Codigo;
                    produtoSelecionado.Quantidade = editarWindow.ProdutoEditado.Quantidade;

                    // Atualiza a tabela após a edição
                    EstoqueDataGrid.Items.Refresh();

                    // Atualiza o produto no banco de dados Firestore
                    AtualizarProdutoNoBanco(produtoSelecionado);

                    MessageBox.Show("Produto atualizado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Selecione um produto para editar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AtualizarProdutoNoBanco(ProdutoData produto)
        {
            var db = DatabaseConnect.Database;
            DocumentReference docRef = db.Collection("Produtos").Document(produto.Codigo);
            docRef.SetAsync(produto);
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
                    // Atualiza a quantidade do produto com o novo valor
                    produtoSelecionado.Quantidade = alterarQuantidadeWindow.Quantidade;
                    EstoqueDataGrid.Items.Refresh(); // Atualiza o DataGrid para exibir a nova quantidade
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

                    // Atualiza o DataGrid para refletir a exclusão
                    EstoqueDataGrid.Items.Refresh();

                    MessageBox.Show("Produto deletado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Selecione um produto para deletar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AtualizarTabelaEstoque()
        {
            // TODO
            // Lógica para atualizar a tabela de estoque
        }

        private void DeletarProdutoDoBanco(object produtoSelecionado)
        {
            // TODO
            // Lógica para deletar o produto do banco de dados
        }

        private void TipoProduto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: Implementar lógica para seleção de tipo de produto
        }
    }
}

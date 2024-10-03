using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class ControleEstoqueUserControl : UserControl
    {
        private List<Produto> produtos;

        public ControleEstoqueUserControl()
        {
            InitializeComponent();
            CarregarProdutos();
        }


        // Aba de Cadastro de Produtos

        // Função foco e perda de foco dos TextBoxes
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && (textBox.Text == "Nome do Produto" || textBox.Text == "Tipo do Produto" || textBox.Text == "Marca do Produto" || textBox.Text == "Quantidade"))
            {
                textBox.Text = string.Empty;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && textBox.Text == string.Empty)
            {
                if (textBox.Name == "NomeProduto")
                {
                    textBox.Text = "Nome do Produto";
                }
                else if (textBox.Name == "TipoProduto")
                {
                    textBox.Text = "Tipo do Produto";
                }
                else if (textBox.Name == "MarcaProduto")
                {
                    textBox.Text = "Marca do Produto";
                }
                else if (textBox.Name == "QuantidadeInicial")
                {
                    textBox.Text = "Quantidade";
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


        // Botões

        private void CadastrarProduto_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para cadastrar o produto no banco de dados
            // Verificar se todos os campos estão preenchidos
            if (NomeProduto.Text != "" && TipoProduto.Text != "" && MarcaProduto.Text != "" && CodigoProduto.Text != "" && QuantidadeInicial.Text != "")
            {
                // Lógica para cadastrar o produto no banco de dados
                // CadastrarProdutoNoBanco();

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
            // Carregar produtos do banco de dados ou outra fonte
            produtos = new List<Produto>
            {
                new Produto { Nome = "Produto 1", Tipo = "Caixa", Marca = "Marca A", Codigo = "001", Quantidade = 10 },
                new Produto { Nome = "Produto 2", Tipo = "Radiador", Marca = "Marca B", Codigo = "002", Quantidade = 5 }
                // Adicione mais produtos conforme necessário
            };

            EstoqueDataGrid.ItemsSource = produtos;
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

        private void EditarProduto_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para editar o produto selecionado
            if (EstoqueDataGrid.SelectedItem != null)
            {
                // Obter o produto selecionado e abrir uma nova janela ou diálogo para edição
            }
            else
            {
                MessageBox.Show("Selecione um produto para editar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AlterarQuantidade_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para adicionar ou remover quantidade do produto selecionado
            if (EstoqueDataGrid.SelectedItem != null)
            {
                // Obter o produto selecionado e abrir uma nova janela ou diálogo para editar quantidade 
            }
            else
            {
                MessageBox.Show("Selecione um produto para adicionar/remover quantidade.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeletarProduto_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para deletar o produto selecionado
            if (EstoqueDataGrid.SelectedItem != null)
            {
                // Obter o produto selecionado
                var produtoSelecionado = EstoqueDataGrid.SelectedItem;
                // Confirmar a exclusão
                MessageBoxResult result = MessageBox.Show("Tem certeza que deseja deletar este produto?", "Confirmação", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    // Lógica para deletar o produto do banco de dados
                    DeletarProdutoDoBanco(produtoSelecionado);
                    // Atualizar a tabela após a exclusão
                    AtualizarTabelaEstoque();
                }
            }
            else
            {
                MessageBox.Show("Selecione um produto para deletar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AtualizarTabelaEstoque()
        {
            // Lógica para atualizar a tabela de estoque
        }

        private void DeletarProdutoDoBanco(object produtoSelecionado)
        {
            // Lógica para deletar o produto do banco de dados
        }
    }

    public class Produto
    {
        public string Nome { get; set; }
        public string Tipo { get; set; }
        public string Marca { get; set; }
        public string Codigo { get; set; }
        public int Quantidade { get; set; }
    }
}

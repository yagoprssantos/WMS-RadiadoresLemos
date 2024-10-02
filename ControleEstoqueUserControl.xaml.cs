using System.Windows;
using System.Windows.Controls;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class ControleEstoqueUserControl : UserControl
    {
        public ControleEstoqueUserControl()
        {
            InitializeComponent();
        }

        // Essa função é chamada quando o TextBox recebe o foco
        // Serve para limpar o texto padrão do TextBox
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && (textBox.Text == "Nome do Produto" || textBox.Text == "Tipo do Produto" || textBox.Text == "Marca do Produto" || textBox.Text == "Quantidade"))
            {
                textBox.Text = string.Empty;
            }
        }

        // Essa função é chamada quando o TextBox perde o foco
        // Serve para adicionar o texto padrão ao TextBox
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && string.IsNullOrWhiteSpace(textBox.Text))
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
                else if (textBox.Name == "CodigoProduto")
                {
                    textBox.Text = "Código do Produto";
                }
                else if (textBox.Name == "QuantidadeProduto")
                {
                    textBox.Text = "Quantidade";
                }
            }
        }

        private void CadastrarProduto_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para cadastrar o produto no banco de dados
        }

        private void AdicionarQuantidade_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para adicionar quantidade ao produto selecionado
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
}

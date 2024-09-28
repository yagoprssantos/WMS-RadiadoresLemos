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
    }
}

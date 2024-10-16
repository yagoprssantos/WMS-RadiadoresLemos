using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class EditarProdutoWindow : Window
    {
        public EditarProdutoWindow()
        {
            InitializeComponent();
        }

        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para salvar o produto
            string nome = NomeTextBox.Text;
            string tipo = TipoTextBox.Text;
            string marca = MarcaTextBox.Text;
            string codigo = CodigoTextBox.Text;
            int quantidade;

            if (int.TryParse(QuantidadeTextBox.Text, out quantidade))
            {
                // Suponha que você tenha um método para salvar o produto
                // SalvarProduto(nome, tipo, marca, codigo, quantidade);
                MessageBox.Show("Produto salvo com sucesso!");
            }
            else
            {
                MessageBox.Show("Quantidade inválida!");
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para cancelar a edição
            this.Close();
        }

        private void QuantidadeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Permitir apenas entrada numérica
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void QuantidadeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Lógica para quando o texto da quantidade mudar
        }

        private void CodigoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Lógica para quando o texto do código mudar
        }
    }
}

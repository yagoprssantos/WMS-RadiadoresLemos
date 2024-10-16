using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class EditarProdutoWindow : Window
    {
        //public Produto Produto { get; private set; }

        public EditarProdutoWindow()
        {
            //InitializeComponent();
            //Produto = produto;

            //// Preenche os campos com os dados atuais do produto
            //NomeTextBox.Text = Produto.Nome;
            //TipoTextBox.Text = Produto.Tipo;
            //MarcaTextBox.Text = Produto.Marca;
            //CodigoTextBox.Text = Produto.Codigo;
            //QuantidadeTextBox.Text = Produto.Quantidade.ToString();
        }

        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            // Valida e salva as alterações no produto
        //    if (int.TryParse(QuantidadeTextBox.Text, out int quantidade))
        //    {
        //        Produto.Nome = NomeTextBox.Text;
        //        Produto.Tipo = TipoTextBox.Text;
        //        Produto.Marca = MarcaTextBox.Text;
        //        Produto.Codigo = CodigoTextBox.Text;
        //        Produto.Quantidade = quantidade;

        //        DialogResult = true; // Define o resultado como "OK" para confirmar o salvamento
        //        Close();
        //    }
        //    else
        //    {
        //        MessageBox.Show("Por favor, insira um valor numérico para a quantidade.", "Erro de Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
        //    }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            //DialogResult = false; // Cancela a edição
            //Close();
        }

        private void QuantidadeTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            //e.Handled = !int.TryParse(e.Text, out _); // Apenas números são permitidos
        }

        private void QuantidadeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void CodigoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}

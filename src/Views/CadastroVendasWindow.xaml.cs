using System;
using System.Windows;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class CadastroVendasWindow : Window
    {
        public CadastroVendasWindow()
        {
            InitializeComponent();

            // Inicializa os DatePickers com a data atual
            dateCompra.SelectedDate = DateTime.Today;
            datePagamento.SelectedDate = DateTime.Today;
        }

        private void BtnFechar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnCadastrar_Click(object sender, RoutedEventArgs e)
        {
            // Validar os campos
            if (string.IsNullOrWhiteSpace(txtProduto.Text))
            {
                MessageBox.Show("Por favor, informe o produto.", "Campo obrigatório", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtProduto.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtCliente.Text))
            {
                MessageBox.Show("Por favor, informe o cliente.", "Campo obrigatório", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCliente.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPreco.Text) || !decimal.TryParse(txtPreco.Text, out _))
            {
                MessageBox.Show("Por favor, informe um preço válido.", "Campo obrigatório", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPreco.Focus();
                return;
            }

            if (!datePagamento.SelectedDate.HasValue)
            {
                MessageBox.Show("Por favor, informe a data de pagamento.", "Campo obrigatório", MessageBoxButton.OK, MessageBoxImage.Warning);
                datePagamento.Focus();
                return;
            }

            if (!dateCompra.SelectedDate.HasValue)
            {
                MessageBox.Show("Por favor, informe a data da compra.", "Campo obrigatório", MessageBoxButton.OK, MessageBoxImage.Warning);
                dateCompra.Focus();
                return;
            }

            // Aqui você implementaria o código para salvar a transação
            MessageBox.Show("Venda cadastrada com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
    }
}
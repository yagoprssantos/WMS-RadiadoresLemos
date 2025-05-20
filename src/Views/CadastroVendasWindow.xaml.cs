using System;
using System.Windows;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

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

        private async void BtnCadastrar_Click(object sender, RoutedEventArgs e)
        {
            try
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

                if (!decimal.TryParse(txtPreco.Text, out decimal preco))
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

                // Gerar número de pedido (pode ser personalizado conforme sua necessidade)
                string numeroPedido = $"PED-{DateTime.Now:yyyyMMddHHmmss}";

                // Criar objeto de venda
                VendaData novaVenda = new VendaData
                {
                    Produto = txtProduto.Text,
                    Cliente = txtCliente.Text,
                    ValorTotal = preco,
                    Pedido = numeroPedido,
                    DataPagamento = datePagamento.SelectedDate.Value,
                    DataCompra = dateCompra.SelectedDate.Value
                };

                // Salvar a venda usando o serviço
                bool sucesso = await VendaService.SalvarVenda(novaVenda);

                if (sucesso)
                {
                    MessageBox.Show("VendaData cadastrada com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Notificar que uma venda foi adicionada (para atualizar a interface)
                    if (VendaAdicionada != null)
                        VendaAdicionada.Invoke(this, novaVenda);

                    this.Close();
                }
                else
                {
                    MessageBox.Show("Ocorreu um erro ao cadastrar a venda. Por favor, tente novamente.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocorreu um erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Evento para notificar quando uma venda for adicionada
        public static event EventHandler<VendaData> VendaAdicionada;
    }
}
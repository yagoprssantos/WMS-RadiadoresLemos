using System;
using System.Windows;
using System.Windows.Controls;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class DetalhesUserControl : UserControl
    {
        private CompraData? _compraAtual;
        private VendaData? _vendaAtual;
        private bool _isCompra;

        public DetalhesUserControl()
        {
            InitializeComponent();
        }

        public DetalhesUserControl(CompraData compra)
        {
            InitializeComponent();
            _compraAtual = compra;
            _isCompra = true;
            DataContext = compra;
            
            // Configura a exibição para compras
            if (FindName("FornecedorLabel") is TextBlock fornecedorLabel)
                fornecedorLabel.Text = "Fornecedor:";
            if (FindName("FornecedorTextBox") is TextBox fornecedorBox)
                fornecedorBox.Text = compra.FornecedorNome;
        }

        public DetalhesUserControl(VendaData venda)
        {
            InitializeComponent();
            _vendaAtual = venda;
            _isCompra = false;
            DataContext = venda;

            // Ajusta labels específicos para venda
            if (FindName("FornecedorLabel") is TextBlock fornecedorLabel)
                fornecedorLabel.Text = "Cliente:";
            if (FindName("FornecedorTextBox") is TextBox fornecedorBox)
                fornecedorBox.Text = venda.ClienteCNPJ;
        }

        private void ImprimirPDF_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidade de impressão em desenvolvimento", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Editar_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidade de edição em desenvolvimento", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void GerarNotaFiscal_Click(object sender, RoutedEventArgs e)
        {
            string tipo = _isCompra ? "compra" : "venda";
            MessageBox.Show($"Funcionalidade de geração de nota fiscal de {tipo} em desenvolvimento", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
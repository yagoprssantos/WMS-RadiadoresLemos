using System;
using System.Windows;
using System.Windows.Controls;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class DetalhesUserControl : UserControl
    {
        private CompraData _compraAtual;

        public DetalhesUserControl()
        {
            InitializeComponent();
        }

        public DetalhesUserControl(CompraData compra)
        {
            InitializeComponent();
            _compraAtual = compra;
            DataContext = compra;
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
            MessageBox.Show("Funcionalidade de geração de nota fiscal em desenvolvimento", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
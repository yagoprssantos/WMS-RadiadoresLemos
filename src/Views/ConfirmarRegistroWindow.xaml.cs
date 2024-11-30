using System.Windows;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class ConfirmarRegistroWindow : Window
    {
        public ProdutoData ProdutoAntes { get; set; }
        int QuantidadeFinal { get; set; }
        public bool IsEntrada { get; set; }

        public ConfirmarRegistroWindow(ProdutoData produtoAntes, int quantidadeFinal, bool isEntrada)
        {
            ProdutoAntes = produtoAntes;
            QuantidadeFinal = quantidadeFinal;
            IsEntrada = isEntrada;
            DataContext = this;
        }

        private void ConfirmarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

using System.Windows;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class DetalhesProdutoWindow : Window
    {
        public DetalhesProdutoWindow(ProdutoData produto)
        {
            InitializeComponent();
            DataContext = produto;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

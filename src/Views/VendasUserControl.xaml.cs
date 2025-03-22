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
using System.Windows.Navigation;
using System.Windows.Shapes;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    /// <summary>
    /// Interação lógica para VendasUserControl.xam
    /// </summary>
    public partial class VendasUserControl : UserControl
    {
        public VendasUserControl()
        {
            InitializeComponent();
        }

        private void DetalhesButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtenha os dados da venda correspondente
            var venda = (sender as Button).DataContext as Venda;

            // Crie uma nova instância do UserControl de detalhes
            var detalhesVendaUserControl = new DetalhesVendaUserControl
            {
                DataContext = venda
            };

            // Exiba a tela de detalhes usando o ContentControl
            var contentControl = (Parent as ContentControl);
            contentControl.Content = detalhesVendaUserControl;
        }
    }
}

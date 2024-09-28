using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WMS_RadiadoresLemos_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ControleEstoque_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new ControleEstoqueUserControl();
        }

        private void Notificacoes_Click(object sender, RoutedEventArgs e)
        {
            //ContentArea.Content = new NotificacoesUserControl();
        }

        private void Relatorios_Click(object sender, RoutedEventArgs e)
        {
            //ContentArea.Content = new RelatoriosUserControl();
        }

        private void BancoDados_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new BancoDadosUserControl();
        }

        private void Usuarios_Click(object sender, RoutedEventArgs e)
        {
            //ContentArea.Content = new UsuariosUserControl();
        }
    }
}


using Oracle.ManagedDataAccess.Client;
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

            // Conectar com o banco de dados
            DatabaseConnect db = new DatabaseConnect();
            db.Connect();
        }

        private void ControleEstoque_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new ControleEstoqueUserControl();
        }

        private void BancoDados_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new BancoDadosUserControl();
        }
    }
}


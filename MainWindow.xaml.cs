using Google.Cloud.Firestore;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WMS_RadiadoresLemos_WPF.Classes;

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
            SetupDatabaseConnection();
            SetupStatusBar();
        }


        private void SetupDatabaseConnection()
        {
            UpdateStatusBar("Estabelecendo conexão com o banco de dados...", Colors.DarkOrange);

            // Estabelece a conexão com o banco de dados
            DatabaseConnect.SetEnvironmentVarible();
            if (DatabaseConnect.Database != null)
            {
                UpdateStatusBar("Conexão com o banco de dados estabelecida", Colors.DarkGreen);
                VerifyConnectionButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                UpdateStatusBar("Erro ao conectar com o banco de dados", Colors.DarkRed);
                VerifyConnectionButton.Visibility = Visibility.Visible;
            }
        }

                
        private void SetupStatusBar()
        {
            UpdateDateTime();
            StartDateTimeUpdater();
        }


        // Função para abrir a aba de Controle de Estoque
        private void ControleEstoque_Click(object sender, RoutedEventArgs e)
        {
            // Fecha qualquer aba aberta
            ContentArea.Content = null;
            // Abre a aba de Controle de Estoque
            ContentArea.Content = new ControleEstoqueUserControl();
        }

        // Função para abrir a aba de Controle de Vendas
        private void BancoDados_Click(object sender, RoutedEventArgs e)
        {
            // Fecha qualquer aba aberta
            ContentArea.Content = null;
            // Abre a aba de Controle de Vendas
            ContentArea.Content = new BancoDadosUserControl();
        }



        // Barra de Status
        // Botão para verificar a conexão com o banco de dados novamente
        private void VerifyConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            SetupDatabaseConnection();
            SetupStatusBar();
        }

        private void UpdateStatusBar(string message, Color color)
        {
            StatusBarItem.Content = message;
            StatusBar.Background = new SolidColorBrush(color);
        }

        private void UpdateDateTime()
        {
            StatusBarDateTime.Content = $"{DateTime.Now.ToLongDateString()}  |  {DateTime.Now.ToLongTimeString()}  ";
        }

        private void StartDateTimeUpdater()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (sender, args) => UpdateDateTime();
            timer.Start();
        }

    }

}


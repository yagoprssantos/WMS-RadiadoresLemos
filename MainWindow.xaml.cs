using Google.Cloud.Firestore;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WMS_RadiadoresLemos_WPF.Classes;

namespace WMS_RadiadoresLemos_WPF
{
    // Interaction logic for MainWindow.xaml
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SetupDatabaseConnection();
            SetupStatusBar();
        }

        // Configura a conexão com o banco de dados.
        private void SetupDatabaseConnection()
        {
            // Atualiza a barra de status para indicar que a conexão está sendo estabelecida
            UpdateStatusBar("Estabelecendo conexão com o banco de dados...", Colors.DarkOrange);

            // Estabelece a conexão com o banco de dados
            DatabaseConnect.SetEnvironmentVarible();
            if (DatabaseConnect.Database != null)
            {
                // Conexão bem-sucedida
                UpdateStatusBar("Conexão com o banco de dados estabelecida", Colors.DarkGreen);
                VerifyConnectionButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Falha na conexão
                UpdateStatusBar("Erro ao conectar com o banco de dados", Colors.DarkRed);
                VerifyConnectionButton.Visibility = Visibility.Visible;
            }
        }

        // Configura a barra de status.
        private void SetupStatusBar()
        {
            UpdateDateTime();
            StartDateTimeUpdater();
        }

        // Abre a aba de Controle de Estoque.
        private void ControleEstoque_Click(object sender, RoutedEventArgs e)
        {
            // Fecha qualquer aba aberta
            ContentArea.Content = null;
            // Abre a aba de Controle de Estoque
            ContentArea.Content = new ControleEstoqueUserControl();
        }

        // Abre a aba de Controle de Vendas.
        private void BancoDados_Click(object sender, RoutedEventArgs e)
        {
            // Fecha qualquer aba aberta
            ContentArea.Content = null;
            // Abre a aba de Controle de Vendas
            ContentArea.Content = new BancoDadosUserControl();
        }

        // Verifica a conexão com o banco de dados novamente.
        private void VerifyConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            SetupDatabaseConnection();
            SetupStatusBar();
        }

        // Atualiza a barra de status com uma mensagem e cor especificadas.
        private void UpdateStatusBar(string message, Color color)
        {
            StatusBarItem.Content = message;
            StatusBar.Background = new SolidColorBrush(color);
        }

        // Atualiza a data e hora na barra de status.
        private void UpdateDateTime()
        {
            StatusBarDateTime.Content = $"{DateTime.Now.ToLongDateString()}  |  {DateTime.Now.ToLongTimeString()}  ";
        }

        /// Inicia um temporizador para atualizar a data e hora a cada segundo.
        private void StartDateTimeUpdater()
        {
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += (sender, args) => UpdateDateTime();
            timer.Start();
        }
    }
}

using Google.Cloud.Firestore;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
            AbrirDashboard();
        }

        private void SetupDatabaseConnection()
        {
            UpdateStatusBar("Estabelecendo conexão com o banco de dados...", Colors.DarkOrange);

            // Estabelece a conexão com o banco de dados Firestore
            DatabaseConnect.SetEnvironmentVarible(); // Certifique-se de que essa função configura corretamente a variável do banco

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

        // Atualiza a barra de status com a data e hora atual
        private void UpdateDateTime()
        {
            StatusBarDateTime.Content = $"{DateTime.Now.ToLongDateString()}  |  {DateTime.Now.ToLongTimeString()}  ";
        }

        // Inicia o temporizador que atualiza a data e hora a cada segundo
        private void StartDateTimeUpdater()
        {
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += (sender, args) => UpdateDateTime();
            timer.Start();
        }

        // Função para abrir a aba de Controle de Estoque
        private void ControleEstoque_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = null;
            ContentArea.Content = new ControleEstoqueUserControl();
            UpdateStatusBar("Exibindo o Controle de Estoque", Colors.DarkBlue);
        }

        // Função para abrir o Dashboard
        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            AbrirDashboard();
        }

        // Função que abre o Dashboard
        private void AbrirDashboard()
        {
            ContentArea.Content = null;
            ContentArea.Content = new DashboardUserControl();
            UpdateStatusBar("Exibindo o Dashboard", Colors.DarkBlue);
        }

        // Função para abrir a aba de Usuários
        private void Usuarios_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = null;
            ContentArea.Content = new UsuariosUserControl();
            UpdateStatusBar("Exibindo a aba de Usuários", Colors.DarkBlue);
        }

        // Verifica a conexão com o banco de dados e tenta reconectar
        private void VerifyConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            SetupDatabaseConnection();
        }

        // Atualiza a barra de status com uma mensagem e uma cor
        private void UpdateStatusBar(string message, Color color)
        {
            StatusBarItem.Content = message;
            StatusBar.Background = new SolidColorBrush(color);
        }
    }
}

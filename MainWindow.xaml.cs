using Google.Cloud.Firestore;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WMS_RadiadoresLemos_WPF.Classes;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // Inicia processo de login
            InitializeComponent();
            ShowLoginWindow();
            SetupDatabaseConnection();
            SetupStatusBar();
        }

        // Exibe a janela de login
        private void ShowLoginWindow()
        {
            LoginWindow loginWindow = new LoginWindow();
            bool? result = loginWindow.ShowDialog();

            // Se o login aceitar, exibe a janela principal
            if (result.HasValue && result.Value)
            {
                this.Show();
            }
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

        // Abas
        // Função para abrir a aba de Registro de Entrada e Saída
        private void RegistroEntradaSaida_Click(object sender, RoutedEventArgs e)
        {
            Log("Abrindo aba de Registro de Entrada e Saída");
            ContentArea.Content = null;
            ContentArea.Content = new RegistroEntradaSaidaUserControl();
        }

        // Função para abrir a aba de Controle de Estoque
        private void ControleEstoque_Click(object sender, RoutedEventArgs e)
        {
            Log("Abrindo aba de Controle de Estoque");
            ContentArea.Content = null;
            ContentArea.Content = new ControleEstoqueUserControl();
        }

        // Função para abrir o Dashboard
        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            Log("Abrindo Dashboard");
            ContentArea.Content = null;
            ContentArea.Content = new DashboardUserControl();
        }

        // Função para abrir a aba de Usuários
        private void Usuarios_Click(object sender, RoutedEventArgs e)
        {
            Log("Abrindo aba de Usuários");
            ContentArea.Content = null;
            ContentArea.Content = new UsuariosUserControl();
        }

        // Botão de logout para retornar à janela de login
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Exibe uma caixa de diálogo de confirmação
            MessageBoxResult result = MessageBox.Show("Você tem certeza que deseja sair?", "Confirmar Logout", MessageBoxButton.YesNo, MessageBoxImage.Question);

            // Se o usuário confirmar, realiza o logout
            if (result == MessageBoxResult.Yes)
            {
                // Oculta a janela principal
                this.Hide();

                // Reabre a janela de login
                ShowLoginWindow();
            }
        }

        // Verifica a conexão com o banco de dados e tenta reconectar
        private void VerifyConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            Log("Verificando conexão com o banco de dados");
            SetupDatabaseConnection();
            SetupStatusBar();
        }

        // Atualiza a barra de status com uma mensagem e uma cor
        private void UpdateStatusBar(string message, Color color)
        {
            StatusBarItem.Content = message;
            StatusBar.Background = new SolidColorBrush(color);
        }

        // Função para logar mensagens
        private void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now}: {message}");
        }
    }
}

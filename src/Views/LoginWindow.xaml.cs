using System;
using System.Windows;
using System.Windows.Input;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class LoginWindow : Window

    {
        public string Username { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
        }

        // Botões
        // Botão de login
        private void ConfirmarLogin_Click(object sender, RoutedEventArgs e)
        {
            TentarLogin();
        }

        // Botão de sair
        private void Sair_Click(object sender, RoutedEventArgs e)
        {
            // Fechar a aplicação
            Application.Current.Shutdown();
        }

        // Manipulador de eventos para a tecla "Enter" no campo de usuário
        private void UsernameField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TentarLogin();
            }
        }

        // Manipulador de eventos para a tecla "Enter" no campo de senha
        private void PasswordField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TentarLogin();
            }
        }

        private void TentarLogin()
        {
            string username = UsernameField.Text;
            string password = PasswordField.Password;

            // Aqui você pode adicionar a lógica de autenticação
            if (username == "admin" && password == "admin")
            {
                // Login bem-sucedido
                Username = username;
                this.Hide();

                // Inicia a janela principal
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();

                // Fecha o login
                this.Close();
            }
            else
            {
                // Login falhou
                MessageBox.Show("Usuário ou senha inválidos. Tente novamente.", "Erro de Login", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

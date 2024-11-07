using System;
using System.Windows;
using System.Windows.Input;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class LoginWindow : Window
    {
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

        // Definir condição de fechamento
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Fechar a aplicação se a janela de login for fechada sem sucesso no login
            if (DialogResult != true)
            {
                Application.Current.Shutdown();
            }
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

        // Método para tentar o login
        private void TentarLogin()
        {
            string username = UsernameField.Text;
            string password = PasswordField.Password;

            // Aqui você pode adicionar a lógica de autenticação
            if (username == "admin" && password == "admin")
            {
                // Login bem-sucedido
                DialogResult = true;
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

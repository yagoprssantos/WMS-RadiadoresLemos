using System.Windows;
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
    }
}

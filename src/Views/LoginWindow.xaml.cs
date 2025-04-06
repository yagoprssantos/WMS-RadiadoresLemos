using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;
using System.Windows.Threading;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class LoginWindow : Window
    {
        private static LoginWindow? _instance;

        public LoginWindow()
        {
            InitializeComponent();
            _instance = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UsernameField.Focus();
        }

        private void ConfirmarLogin_Click(object sender, RoutedEventArgs e)
        {
            TentarLogin();
        }

        private void Sair_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void UsernameField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TentarLogin();
            }
        }

        private void PasswordField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TentarLogin();
            }
        }

        private async void TentarLogin()
        {
            string username = UsernameField.Text;
            string password = PasswordField.Password;

            LoadingGrid.Visibility = Visibility.Visible;
            TextoCarregamento.Text = "Verificando usuário...";

            try
            {
                var usuarioValido = await VerificarUsuario(username, password);

                if (usuarioValido != null)
                {
                    TextoCarregamento.Text = "Sucesso!";
                    LoginBemSucedido(usuarioValido);
                }
                else
                {
                    MessageBox.Show("Usuário ou senha inválidos. Tente novamente.", "Erro de Login", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                LoadingGrid.Visibility = Visibility.Collapsed;
            }
        }

        private async Task<UsuarioData?> VerificarUsuario(string username, string password)
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db != null)
                {
                    var usuariosCollection = db.GetCollection<UsuarioData>("usuarios");
                    var usuarios = usuariosCollection.FindAll().ToList();

                    return usuarios.Find(u => (u.Nome == username || u.Matricula == username) && u.Senha == password);
                }
                return null;
            }
            catch (Exception ex)
            {
                Alerta.AdicionarAlerta("Erro",
                    ex.Message.ToString(),
                    "Não foi possível verificar o usuário no banco de dados. Possíveis motivos:\n" +
                    "- Problemas de conexão com o banco;\n" +
                    "- Dados corrompidos;\n" +
                    "- Falha na operação de verificação.",
                    "- Verifique a conexão com o banco;\n" +
                    "- Tente novamente mais tarde.");
                return null;
            }
        }

        private void LoginBemSucedido(UsuarioData usuario)
        {
            MainWindow.UsuarioLogado = usuario;
            MainWindow.isSincronized = true;
            this.Hide();

            var mainWindow = new MainWindow();
            mainWindow.Show();

            this.Close();
        }
    }
}

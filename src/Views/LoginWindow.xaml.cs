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
        private bool isLogoutInitiated = false;
        private DispatcherTimer _connectDatabaseTimer;

        public LoginWindow()
        {
            InitializeComponent();
            _instance = this;

            // Configura timer para conectar com banco periodicamente
            _connectDatabaseTimer = new DispatcherTimer();
            _connectDatabaseTimer.Interval = TimeSpan.FromMinutes(1);
            _connectDatabaseTimer.Tick += ConnectDatabaseTimer_Tick;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UsernameField.Focus();
        }

        private void StartApplication()
        {
            try
            {
                // Tenta conectar ao banco de dados
                DatabaseConnect.SetEnvironmentVarible();
                var db = DatabaseConnect.Database;
                if (db == null)
                {
                    Alerta.AdicionarAlerta("Erro",
                        "Não foi possível conectar ao banco de dados",
                        "O banco de dados não pôde ser criado ou conectado. Possíveis motivos:\n" +
                        "- Permissões insuficientes;\n" +
                        "- Diretório não existe;\n" +
                        "- Erro na criação do banco.",
                        "- Verifique as permissões do diretório;\n" +
                        "- Tente executar como administrador;\n" +
                        "- Verifique se o diretório existe.");
                    return;
                }

                // Tenta inserir dados iniciais
                try
                {
                    DadosIniciais.InserirDadosIniciais();
                }
                catch (Exception ex)
                {
                    Alerta.AdicionarAlerta("Aviso",
                        "Não foi possível inserir dados iniciais",
                        $"Erro ao inserir dados iniciais: {ex.Message}\n" +
                        "A aplicação continuará, mas alguns dados podem estar faltando.",
                        "- Verifique se o banco está acessível;\n" +
                        "- Tente novamente mais tarde.");
                }

                // Abre a janela principal
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                Alerta.AdicionarAlerta("Erro",
                    "Erro ao iniciar a aplicação",
                    $"Ocorreu um erro ao iniciar a aplicação: {ex.Message}",
                    "- Verifique as permissões;\n" +
                    "- Tente executar como administrador;\n" +
                    "- Verifique se todos os arquivos necessários existem.");
            }
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

        private async void ConnectDatabaseTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                DatabaseConnect.SetEnvironmentVarible();

                if (DatabaseConnect.Database != null)
                {
                    MainWindow.isSincronized = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao conectar ao banco de dados: {ex.Message}");
                MainWindow.isSincronized = false;
                await Task.Delay(3000);
            }
        }
    }
}

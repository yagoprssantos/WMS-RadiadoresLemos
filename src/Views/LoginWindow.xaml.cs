using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class LoginWindow : Window
    {
        public string Username { get; private set; } = string.Empty;
        private DatabaseFileManager _databaseFileManager;

        public LoginWindow()
        {
            InitializeComponent();
            _databaseFileManager = new DatabaseFileManager();
        }

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

        private async void TentarLogin()
        {
            string username = UsernameField.Text;
            string password = PasswordField.Password;

            // 1. Verificar se o usuário existe nos arquivos JSON
            List<UsuarioData> usuarios = await CarregarUsuariosDoArquivoAsync();
            UsuarioData? usuarioValido = usuarios.Find(u => u.Nome == username && u.Senha == password);

            if (usuarioValido != null)
            {
                LoginBemSucedido(usuarioValido);
                return;
            }

            // 2. Tentar conectar com o banco de dados e verificar lá
            usuarioValido = await VerificarUsuarioNoBancoDeDadosAsync(username, password);

            if (usuarioValido != null)
            {
                // 3. Atualizar arquivos JSON
                await AtualizarUsuariosNoArquivoAsync(usuarios);
                LoginBemSucedido(usuarioValido);
                return;
            }

            // 4. Exibir mensagem de erro
            MessageBox.Show("Usuário ou senha inválidos, usuário não encontrado. Tente novamente.", "Erro de Login", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async Task<List<UsuarioData>> CarregarUsuariosDoArquivoAsync()
        {
            string caminhoArquivoUsuarios = _databaseFileManager.CaminhoArquivoUsuarios;

            if (File.Exists(caminhoArquivoUsuarios))
            {
                string json = await File.ReadAllTextAsync(caminhoArquivoUsuarios);
                return JsonSerializer.Deserialize<List<UsuarioData>>(json) ?? new List<UsuarioData>();
            }

            return new List<UsuarioData>();
        }

        private async Task<UsuarioData?> VerificarUsuarioNoBancoDeDadosAsync(string username, string password)
        {
            List<UsuarioData> usuarios = await _databaseFileManager.ObterColecaoDoBancoDeDadosAsync<UsuarioData>("usuarios");
            return usuarios.Find(u => u.Nome == username && u.Senha == password);
        }

        private async Task AtualizarUsuariosNoArquivoAsync(List<UsuarioData> usuarios)
        {
            string caminhoArquivoUsuarios = _databaseFileManager.CaminhoArquivoUsuarios;
            string json = JsonSerializer.Serialize(usuarios);
            await File.WriteAllTextAsync(caminhoArquivoUsuarios, json);
        }

        private void LoginBemSucedido(UsuarioData usuario)
        {
            // Login bem-sucedido
            MainWindow.UsuarioLogado = usuario;
            this.Hide();

            // Inicia a janela principal
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            // Fecha o login
            this.Close();
        }
    }
}

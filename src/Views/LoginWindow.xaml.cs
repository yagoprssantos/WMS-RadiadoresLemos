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

            // Mostrar LoadingGrid e atualizar o texto
            LoadingGrid.Visibility = Visibility.Visible;
            TextoCarregamento.Text = "Verificando arquivos locais...";

            try
            {
                // Verifica se os arquivos JSON existem
                bool arquivosExistem = File.Exists(_databaseFileManager.CaminhoArquivoUsuarios) &&
                                       File.Exists(_databaseFileManager.CaminhoArquivoProdutos) &&
                                       File.Exists(_databaseFileManager.CaminhoArquivoLogs) &&
                                       File.Exists(_databaseFileManager.CaminhoArquivoMovimentacoes);

                UsuarioData? usuarioValido = null;

                if (arquivosExistem)
                {
                    // Atualizar o texto
                    TextoCarregamento.Text = "Verificando usuário nos arquivos locais...";

                    // 1. Verificar se o usuário existe nos arquivos JSON
                    usuarioValido = await VerificarUsuarioArquivosJSON(username, password);

                    // se os dados do usuário forem encontrados nos arquivos JSON,
                    if (usuarioValido != null)
                    {
                        TextoCarregamento.Text = "Sucesso!";
                        LoginBemSucedido(usuarioValido);
                        return;
                    }
                }

                // Atualizar o texto
                TextoCarregamento.Text = "Verificando usuário no banco de dados...";

                // 2. Tentar conectar com o banco de dados e verificar lá
                usuarioValido = await VerificarUsuarioFirebaseDB(username, password);

                // se o usuário for encontrado no banco de dados,
                if (usuarioValido != null)
                {
                    // Atualizar o texto
                    TextoCarregamento.Text = "Sincronizando arquivos locais...";

                    // 3. Cria os arquivos JSON (se não existirem) e recarrega os usuários
                    await SincronizarArquivos(username, password, arquivosExistem);

                    TextoCarregamento.Text = "Sucesso!";
                    LoginBemSucedido(usuarioValido);
                    return;
                }

                // 4. Exibir mensagem de erro
                MessageBox.Show("Usuário ou senha inválidos, usuário não encontrado. Tente novamente.", "Erro de Login", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Ocultar LoadingGrid
                LoadingGrid.Visibility = Visibility.Collapsed;
            }
        }

        private async Task<UsuarioData?> VerificarUsuarioArquivosJSON(string username, string password)
        {
            // Carrega os usuários dos arquivos JSON
            List<UsuarioData> usuarios = await CarregarUsuariosDoArquivoAsync();

            // Para cada usuário, verifica se o nome ou matrícula e senha correspondem
            return usuarios.Find(u => (u.Nome == username || u.Matrícula == username) && u.Senha == password);
        }

        private async Task<UsuarioData?> VerificarUsuarioFirebaseDB(string username, string password)
        {
            // Captura tabela de usuários do banco de dados
            List<UsuarioData> usuarios = await _databaseFileManager.ObterColecaoFirebaseDB<UsuarioData>("Usuarios");

            // Para cada usuário, verifica se o nome ou matrícula e senha correspondem
            return usuarios.Find(u => (u.Nome == username || u.Matrícula == username) && u.Senha == password);
        }

        private async Task SincronizarArquivos(string username, string password, bool arquivosExistirem)
        {
            try
            {
                // Se os arquivos não existirem, cria-os
                if (!arquivosExistirem)
                {
                    await _databaseFileManager.InicializarArquivosAsync();
                }

                // Atualiza TODOS os arquivos locais com os dados mais recentes do banco de dados
                await _databaseFileManager.AtualizarArquivosAsync();
            }
            catch (Exception ex)
            {
                // Log de erro
                Console.WriteLine($"Erro ao atualizar arquivos e recarregar usuários: {ex.Message}");
            }
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

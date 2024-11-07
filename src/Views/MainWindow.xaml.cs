using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

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

        private async void SetupDatabaseConnection()
        {
            UpdateStatusBar("Estabelecendo conexão com o banco de dados...", Colors.DarkOrange);

            // Estabelece a conexão com o banco de dados Firestore
            DatabaseConnect.SetEnvironmentVarible(); // Certifique-se de que essa função configura corretamente a variável do banco

            if (DatabaseConnect.Database != null)
            {
                UpdateStatusBar("Conexão com o banco de dados estabelecida", Colors.DarkGreen);
                VerifyConnectionButton.Visibility = Visibility.Collapsed;

                // Carrega todas as tabelas no cache
                await CarregarTodasTabelasNoCache();

            }
            else
            {
                UpdateStatusBar("Erro ao conectar com o banco de dados", Colors.DarkRed);
                AlertaCache.AdicionarAlerta("Erro",
                                            "Não foi possível conectar ao banco de dados. Possíveis motivos:\n" +
                                            "- Problemas de conexão com a internet\n" +
                                            "- Configurações incorretas do banco de dados\n" +
                                            "- Serviço do banco de dados indisponível",
                                            "- Verifique sua conexão com a internet\n" +
                                            "- Verifique as configurações do banco de dados\n" +
                                            "- Tente reconectar ou contate o suporte.");
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
            ContentArea.Content = null;
            ContentArea.Content = new RegistroEntradaSaidaUserControl();
        }

        // Função para abrir a aba de Controle de Estoque
        private void ControleEstoque_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = null;
            ContentArea.Content = new ControleEstoqueUserControl();
        }

        // Função para abrir o Dashboard
        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = null;
            ContentArea.Content = new DashboardUserControl();
        }

        // Função para abrir a aba de Banco de Dados
        private void BancoDados_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = null;
            ContentArea.Content = new BancoDadosUserControl();
        }

        // Função para abrir a aba de Usuários
        private void Usuarios_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = null;
            ContentArea.Content = new UsuariosUserControl();
        }
        // Função para abrir a aba de Notificações
        private void Notificacoes_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = null;
            ContentArea.Content = new NotificacoesUserControl();
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
            SetupDatabaseConnection();
            SetupStatusBar();
        }

        // Atualiza a barra de status com uma mensagem e uma cor
        private void UpdateStatusBar(string message, Color color)
        {
            StatusBarItem.Content = message;
            StatusBar.Background = new SolidColorBrush(color);
        }

        // Função para carregar todas as tabelas no cache
        private async Task CarregarTodasTabelasNoCache()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db == null) throw new InvalidOperationException("Conexão com o banco de dados não estabelecida.");

                UpdateStatusBar("Carregando dados no cache...", Colors.DarkOrange);

                // Lista de tabelas a serem carregadas no cache
                var tabelas = new List<string>
                    {
                        "Produtos",
                        "Usuarios",
                        "Historico"
                    };

                // Para cada tabela, 
                foreach (var tabela in tabelas)
                {
                    // Pega a referência da tabela
                    var tabelaRef = db.Collection(tabela);
                    // Pega o snapshot da tabela
                    var snapshot = await tabelaRef.GetSnapshotAsync();

                    // Lista de objetos para armazenar os dados da tabela
                    var listaObjetos = new List<object>();
                    foreach (var doc in snapshot.Documents)
                    {
                        // Se a tabela for de produtos, converte o documento para ProdutoData e adiciona à lista
                        if (tabela == "Produtos")
                        {
                            var produto = doc.ConvertTo<ProdutoData>();
                            listaObjetos.Add(produto);
                        }
                        // Se a tabela for de usuários, converte o documento para UsuarioData e adiciona à lista
                        else if (tabela == "Usuários")
                        {
                            var usuario = doc.ConvertTo<UsuarioData>();
                            listaObjetos.Add(usuario);
                        }

                        else
                        {
                            // Se a tabela não for reconhecida, retorna uma exceção
                            throw new InvalidOperationException($"Tabela '{tabela}' não reconhecida.");
                        }
                    }

                    // Adiciona a lista de objetos ao cache
                    DadosCache.Tabelas[tabela] = listaObjetos;
                }

                // Todo esse código resulta em um cache com todas as tabelas carregadas
                // O cache é um dicionário onde a chave é o nome da tabela e o valor é uma lista de objetos,
                // onde cada objeto é um documento da tabela. Isso permite um acesso rápido e offline aos dados.

                UpdateStatusBar("Dados carregados no cache com sucesso - Pronto para uso", Colors.DarkGreen);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar as tabelas no cache: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatusBar("Erro ao carregar dados no cache - Verifique a conexão com o banco de dados", Colors.DarkRed);
                AlertaCache.AdicionarAlerta("Erro",
                                            $"Não foi possível carregar os dados no cache. Possíveis motivos:\n" +
                                            "- Problemas de conexão com a internet\n" +
                                            "- Configurações incorretas do banco de dados\n" +
                                            "- Serviço do banco de dados indisponível",
                                            "- Verifique sua conexão com a internet\n" +
                                            "- Verifique as configurações do banco de dados\n" +
                                            "- Tente reconectar ou contate o suporte.");
                VerifyConnectionButton.Visibility = Visibility.Visible;
            }
        }
    }
}

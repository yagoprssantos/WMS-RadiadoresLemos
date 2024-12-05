using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class MainWindow : Window
    {
        private bool isLogoutInitiated = false;
        private int _notificationCount = 0;

        // Variável para armazenar o usuário logado
        public static UsuarioData UsuarioLogado { get; set; }

        public MainWindow()
        {
            // Inicia processo de login
            InitializeComponent();
            StartApplication();

            // Adiciona o evento de alerta adicionado
            AlertaCache.AlertaAdicionado += OnAlertaAdicionado;

            this.Closing += Window_Closing;
        }

        private void StartApplication()
        {
            // Inicializa a conexão com o banco de dados
            SetupDatabaseConnection();

            // Registra a entrada do usuário no log
            RegistrarEntradaLog();

            // Adiciona o usuário logado
            SetupUsuarioLogado();

            // Configura a barra de status
            SetupStatusBar();
        }

        // Registra log de saída caso a janela seja fechada ou a aplicação seja encerrada
        private async void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Verifica se o logout foi iniciado pelo botão
            if (isLogoutInitiated) return;

            try
            {
                // Adiciona log
                var log = new LogData
                {
                    Data = DateTime.UtcNow,
                    Tipo = "OPERACIONAL",
                    Nivel = "Usuário",
                    Detalhes = $"Usuário 'NomeDoUsuario' realizou logout", // Substitua pelo nome do usuário real
                    Usuario = "NomeDoUsuario" // Substitua pelo nome do usuário real
                };
                await LogHistorico.RegistrarLogAsync(log);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao registrar a saída do usuário no log: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Não foi possível registrar a saída do usuário no log. Possíveis motivos:\n" +
                                            "- Problemas de conexão com o sistema;\n" +
                                            "- Configurações incorretas do sistema;\n" +
                                            "- Serviço do sistema indisponível.",
                                            "- Tente novamente;\n" +
                                            "- Feche a aplicação e abra novamente.");
            }
        }

        // Registra a entrada do usuário no log
        private async void RegistrarEntradaLog()
        {
            try
            {
                // Verifica se o usuário logado é nulo
                if (UsuarioLogado == null) return;

                // Adiciona log
                var log = new LogData
                {
                    Data = DateTime.UtcNow,
                    Tipo = "OPERACIONAL",
                    Nivel = "Usuário",
                    Detalhes = $"Usuário {UsuarioLogado?.Nome} entrou no sistema",
                    Usuario = UsuarioLogado?.Nome ?? "Usuário não identificado"
                };
                await LogHistorico.RegistrarLogAsync(log);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao registrar a entrada do usuário no log: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Não foi possível registrar a entrada do usuário no log. Possíveis motivos:\n" +
                                            "- Problemas de conexão com o sistema;\n" +
                                            "- Configurações incorretas do sistema;\n" +
                                            "- Serviço do sistema indisponível.",
                                            "- Tente novamente;\n" +
                                            "- Feche a aplicação e abra novamente.");
            }
        }

        // Adiciona o usuário logado na tela
        private void SetupUsuarioLogado()
        {
            if (UsuarioLogado != null)
            {
                // Atualiza o TextBlock com o nome do usuário
                var nomeTextBlock = (TextBlock)Perfil.FindName("NomeUsuarioTextBlock");
                if (nomeTextBlock != null)
                {
                    nomeTextBlock.Text = UsuarioLogado.Nome;
                }

                // Atualiza o TextBlock com a matrícula do usuário
                var matriculaTextBlock = (TextBlock)Perfil.FindName("MatriculaTextBlock");
                if (matriculaTextBlock != null)
                {
                    matriculaTextBlock.Text = UsuarioLogado.Matrícula;
                }
            }
        }

        private async void SetupDatabaseConnection()
        {
            try
            {
                UpdateStatusBar("Estabelecendo conexão com o banco de dados...", Colors.DarkOrange);

                // Estabelece a conexão com o banco de dados Firestore
                DatabaseConnect.SetEnvironmentVarible();

                // Carrega todas as tabelas no cache
                await CarregarTodasTabelasNoCache();

                // Inicializa os arquivos locais com dados do banco de dados, se ainda não existirem
                DatabaseFileManager gerenciadorDeArquivos = new DatabaseFileManager();
                await gerenciadorDeArquivos.InicializarArquivosAsync();

                // Finaliza a conexão com o banco de dados
                UpdateStatusBar("Conexão com o banco de dados estabelecida", Colors.DarkGreen);
                VerifyConnectionButton.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                UpdateStatusBar("Erro ao conectar com o banco de dados", Colors.DarkRed);
                MessageBox.Show($"Erro ao conectar com o banco de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Não foi possível conectar ao banco de dados. Possíveis motivos:\n" +
                                            "- Problemas de conexão com a internet;\n" +
                                            "- Configurações incorretas do banco de dados;\n" +
                                            "- Serviço do banco de dados indisponível.",
                                            "- Verifique sua conexão com a internet;\n" +
                                            "- Verifique as configurações do banco de dados;\n" +
                                            "- Tente reconectar ou contate o suporte.");

                VerifyConnectionButton.Visibility = Visibility.Visible;
            }
        }

        private void SetupStatusBar()
        {
            try
            {
                UpdateDateTime();
                StartDateTimeUpdater();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao configurar a barra de status: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Não foi possível configurar a barra de status. Possíveis motivos:\n" +
                                            "- Problemas de conexão com a internet;\n" +
                                            "- Configurações incorretas do sistema;\n" +
                                            "- Serviço do sistema indisponível.",
                                            "- Verifique sua conexão com a internet;\n" +
                                            "- Verifique as configurações do sistema;\n" +
                                            "- Tente reconectar ou contate o suporte.");
            }
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
        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Exibe uma caixa de diálogo de confirmação
                MessageBoxResult result = MessageBox.Show("Você tem certeza que deseja sair?", "Confirmar Logout", MessageBoxButton.YesNo, MessageBoxImage.Question);

                // Se o usuário confirmar, realiza o logout
                if (result == MessageBoxResult.Yes)
                {
                    // Define a variável de controle como true
                    isLogoutInitiated = true;

                    // Oculta a janela principal
                    this.Hide();

                    // Adiciona log
                    var log = new LogData
                    {
                        Data = DateTime.UtcNow,
                        Tipo = "OPERACIONAL",
                        Nivel = "Usuário",
                        Detalhes = $"Usuário 'NomeDoUsuario' realizou logout", // Substitua pelo nome do usuário real
                        Usuario = "NomeDoUsuario" // Substitua pelo nome do usuário real
                    };
                    await LogHistorico.RegistrarLogAsync(log);

                    // Reabre a janela de login
                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.Show();

                    // Fecha a janela principal
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao realizar logout: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Não foi possível realizar o logout. Possíveis motivos:\n" +
                                            "- Problemas de conexão com o sistema;\n" +
                                            "- Configurações incorretas do sistema;\n" +
                                            "- Serviço do sistema indisponível.",
                                            "- Tente novamente;\n" +
                                            "- Feche a aplicação e abra novamente.");
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
                var db = DatabaseConnect.Database ?? throw new InvalidOperationException("Conexão com o banco de dados não estabelecida.");
                UpdateStatusBar("Carregando dados no cache...", Colors.DarkOrange);

                // Lista de tabelas a serem carregadas no cache
                var tabelas = new List<string>
                    {
                        "Produtos",
                        "Usuarios",
                        "Historico",
                        "Movimentacoes"
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
                        else if (tabela == "Usuarios")
                        {
                            var usuario = doc.ConvertTo<UsuarioData>();
                            listaObjetos.Add(usuario);
                        }
                        // Se a tabela for de histórico, converte o documento para LogData e adiciona à lista
                        else if (tabela == "Historico")
                        {
                            var log = doc.ConvertTo<LogData>();
                            listaObjetos.Add(log);
                        }
                        // Se a tabela for de movimentações, converte o documento para MovimentacaoData e adiciona à lista
                        else if (tabela == "Movimentacoes")
                        {
                            var movimentacao = doc.ConvertTo<MovimentacaoData>();
                            listaObjetos.Add(movimentacao);
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

                UpdateStatusBar("Dados carregados no cache - Pronto para uso", Colors.DarkGreen);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar as tabelas no cache: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatusBar("Erro ao carregar dados no cache - Verifique a conexão com o banco de dados", Colors.DarkRed);

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            $"Não foi possível carregar os dados no cache. Possíveis motivos:\n" +
                                            "- Problemas de conexão com a internet;\n" +
                                            "- Configurações incorretas do banco de dados;\n" +
                                            "- Serviço do banco de dados indisponível.",
                                            "- Verifique sua conexão com a internet;\n" +
                                            "- Verifique as configurações do banco de dados;\n" +
                                            "- Tente reconectar ou contate o suporte.");

                VerifyConnectionButton.Visibility = Visibility.Visible;
            }
        }

        // Função que representa a animação de notificação de alerta
        private void OnAlertaAdicionado(AlertaData alerta)
        {
            // Incrementa a contagem de notificações
            _notificationCount++;

            // Tornar o ícone de notificação visível
            NotificationButton.Visibility = Visibility.Visible;

            // Altera a cor do ícone de notificação para vermelho por 2 segundos e depois fica vermelho
            ColorAnimation colorAnimation = new ColorAnimation
            {
                From = Colors.Transparent,
                To = (Color)ColorConverter.ConvertFromString("#990000"),
                Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(4) // Pisca 4 vezes (2 segundos)
            };

            // Aplica a animação ao fundo do botão de notificação
            NotificationButton.Background = new SolidColorBrush(Colors.Transparent);
            NotificationButton.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);

            // Define a cor final como vermelho após a animação
            colorAnimation.AutoReverse = false;
            NotificationButton.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);

            // Atualizar o ToolTip com a quantidade de notificações
            NotificationToolTip.Content = $"Você tem {_notificationCount} novas notificações";
        }

        // Função para abrir a aba de notificações
        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            // Limpar a contagem de notificações
            _notificationCount = 0;

            // Tornar o fundo do botão de notificação transparente
            NotificationButton.Background = new SolidColorBrush(Colors.Transparent);

            // Abrir a aba de notificações
            ContentArea.Content = null;
            ContentArea.Content = new NotificacoesUserControl();
        }

        private void NotificationButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Atualizar o ToolTip com a quantidade de notificações
            NotificationToolTip.Content = $"Você tem {_notificationCount} novas notificações";
        }

    }
}

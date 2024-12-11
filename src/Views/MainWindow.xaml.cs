using System.IO;
using System.Text.Json;
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
        private DispatcherTimer _saveCacheTimer;
        private DispatcherTimer _connectDatabaseTimer;

        // Variável para armazenar o usuário logado
        public static UsuarioData? UsuarioLogado { get; set; }

        public MainWindow()
        {
            // Inicia processo de login
            InitializeComponent();
            StartApplication();

            // Adiciona o evento de alerta adicionado
            AlertaCache.AlertaAdicionado += OnAlertaAdicionado;

            this.Closing += Window_Closing;

            // Configura o timer para salvar o cache periodicamente
            _saveCacheTimer = new DispatcherTimer();
            _saveCacheTimer.Interval = TimeSpan.FromMinutes(5); // Salva o cache a cada 5 minutos
            _saveCacheTimer.Tick += SaveCacheTimer_Tick;

            // Configura cache para conectar com banco periodicamente
            _connectDatabaseTimer = new DispatcherTimer();
            _connectDatabaseTimer.Interval = TimeSpan.FromMinutes(1); // Tenta conectar a cada 1 minuto
            _connectDatabaseTimer.Tick += ConnectDatabaseTimer_Tick;
        }
        private async void SaveCacheTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // Sincroniza arquivos
                DatabaseFileManager gerenciadorDeArquivos = new DatabaseFileManager();
                await gerenciadorDeArquivos.SalvarCacheNosArquivosAsync();

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Aviso",
                                            "Cache salvo com sucesso" + DateOnly.FromDateTime(DateTime.Now).ToString(),
                                            "Os dados foram salvos nos arquivos locais com sucesso.",
                                            "É possível sair da aplicação com segurança");
            }
            catch (Exception ex)
            {
                AlertaCache.AdicionarAlerta("Aviso",
                                            ex.Message.ToString(),
                                            "Não foi possível salvar alterações. Possíveis motivos:\n" +
                                            "- Arquivo corrompido;\n" +
                                            "- Falta de permissões;\n" +
                                            "- Espaço em disco insuficiente;\n" +
                                            "- Erro de rede;\n" +
                                            "- Problema de compatibilidade;",
                                            "- Recomendasse ficar na aplicação até que tudo fique sincronizado");
            }
        }
        private async void ConnectDatabaseTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // Tenta conectar ao banco de dados
                DatabaseConnect.SetEnvironmentVarible();
                DatabaseConnect.TestConnection();

                // Sincroniza arquivos cache enviando para o banco de dados
                DatabaseFileManager gerenciadorDeArquivos = new DatabaseFileManager();
                await gerenciadorDeArquivos.SalvarCacheNosArquivosAsync();
                await gerenciadorDeArquivos.SincronizarDadosComBancoAsync();

                // Atualiza a barra de status
                UpdateStatusBar("Dados carregados no Firebase - Banco de Dados Online", Colors.DarkGreen);
                UpdateConnectionStatus("Conectado");

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Aviso",
                                            "Conexão com banco de dados estabelecida" + DateOnly.FromDateTime(DateTime.Now).ToString(),
                                            "Os dados foram carregados do banco de dados com sucesso.",
                                            "É possível sair da aplicação com segurança");

                // Para os timers
                _saveCacheTimer.Stop();
                _connectDatabaseTimer.Stop();
            }
            catch (Exception ex)
            {
                // Log de erro
                Console.WriteLine($"Erro ao conectar ao banco de dados: {ex.Message}");
            }
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
                // Verifica se o usuário logado é nulo
                if (UsuarioLogado == null) return;

                // Desconecta do banco de dados
                DatabaseConnect.Disconnect();

                // Adiciona log
                var log = new LogData
                {
                    Data = DateTime.UtcNow,
                    Tipo = "OPERACIONAL",
                    Nivel = "Usuário",
                    Detalhes = $"Usuário {UsuarioLogado.Nome} realizou logout",
                    Usuario = UsuarioLogado.Nome
                };
                await LogHistorico.RegistrarLogAsync(log);

                // Remove usuário logado
                UsuarioLogado = null;

                // Para timers
                _saveCacheTimer.Stop();
                _connectDatabaseTimer.Stop();

                // Salva o cache nos arquivos JSON
                DatabaseFileManager gerenciadorDeArquivos = new DatabaseFileManager();
                await gerenciadorDeArquivos.SalvarCacheNosArquivosAsync();
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

                // Testa a conexão com o banco de dados
                DatabaseConnect.TestConnection();

                // Carrega todas as tabelas no cache
                await CarregarTodasTabelasNoCache();

                // Inicializa os arquivos locais com dados do banco de dados, se ainda não existirem
                // A PRIMEIRA CONEXÃO DEVE SER FEITA USANDO INTERNET, CASO CONTRÁRIO, NÃO SERÁ POSSÍVEL SINCRONIZAR OS DADOS
                DatabaseFileManager gerenciadorDeArquivos = new DatabaseFileManager();
                await gerenciadorDeArquivos.InicializarArquivosAsync();
            }
            catch (Exception ex)
            {
                _connectDatabaseTimer.Start();
                UpdateStatusBar("Erro ao carregar dados", Colors.DarkRed);
                MessageBox.Show($"Erro ao carregar dados, com banco de dados e com arquivos locais: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Não foi possível carregar dados com sucesso. Possíveis motivos:\n" +
                                            "- Problemas de conexão com a internet;\n" +
                                            "- Configurações incorretas do banco de dados;\n" +
                                            "- Arquivos locais corrompidos ou ausentes.",
                                            "- Verifique sua conexão com a internet;\n" +
                                            "- Verifique as configurações do banco de dados;\n" +
                                            "- Reinicie a aplicação");
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
        private void RegistroEntradaSaida_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = null;
            ContentArea.Content = new RegistroEntradaSaidaUserControl();
        }

        private void ControleEstoque_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = null;
            ContentArea.Content = new ControleEstoqueUserControl();
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = null;
            ContentArea.Content = new DashboardUserControl();
        }

        private void BancoDados_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = null;
            ContentArea.Content = new BancoDadosUserControl();
        }

        private void Usuarios_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = null;
            ContentArea.Content = new UsuariosUserControl();
        }

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

                    // Desconecta do banco de dados
                    DatabaseConnect.Disconnect();

                    // Oculta a janela principal
                    this.Hide();

                    // Verifica se o usuário logado é nulo
                    if (UsuarioLogado != null)
                    {
                        // Adiciona log
                        var log = new LogData
                        {
                            Data = DateTime.UtcNow,
                            Tipo = "OPERACIONAL",
                            Nivel = "Usuário",
                            Detalhes = $"Usuário {UsuarioLogado.Nome} realizou logout",
                            Usuario = UsuarioLogado.Nome
                        };
                        await LogHistorico.RegistrarLogAsync(log);

                        // Remove usuário logado
                        UsuarioLogado = null;

                        // Para timers
                        _saveCacheTimer.Stop();
                        _connectDatabaseTimer.Stop();

                        // Salva o cache nos arquivos JSON
                        DatabaseFileManager gerenciadorDeArquivos = new DatabaseFileManager();
                        await gerenciadorDeArquivos.SalvarCacheNosArquivosAsync();
                    }

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


        // Atualiza a barra de status com uma mensagem e uma cor
        private void UpdateStatusBar(string message, Color color)
        {
            StatusBarItem.Content = message;
            StatusBar.Background = new SolidColorBrush(color);
        }

        // Atualiza o status da conexão com o banco de dados
        private void UpdateConnectionStatus(string status)
        {
            ConnectionStatus.Text = status;
        }


        // Função para carregar todas as tabelas no cache
        private async Task CarregarTodasTabelasNoCache()
        {
            try
            {
                var db = DatabaseConnect.Database;
                UpdateStatusBar("Carregando dados no cache...", Colors.DarkOrange);

                // Lista de tabelas a serem carregadas no cache
                var tabelas = new List<string>
                {
                    "Produtos",
                    "Usuarios",
                    "Historico",
                    "Movimentacoes"
                };

                var dbFileManager = new DatabaseFileManager();

                // Para cada tabela, 
                foreach (var tabela in tabelas)
                {
                    var listaObjetos = new List<object>();

                    // Se houver conexão com o banco de dados, carrega os dados do banco
                    if (db != null && DatabaseConnect.IsConnected)
                    {
                        // Pega a referência da tabela
                        var tabelaRef = db.Collection(tabela);
                        // Pega o snapshot da tabela
                        var snapshot = await tabelaRef.GetSnapshotAsync();

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

                        UpdateStatusBar("Dados carregados no Firebase - Banco de Dados Online", Colors.DarkGreen);
                        UpdateConnectionStatus("Conectado");
                    }
                    else
                    {
                        // Se não houver conexão com o banco de dados, carrega os dados dos arquivos locais
                        string caminhoArquivo = tabela switch
                        {
                            "Produtos" => dbFileManager.CaminhoArquivoProdutos,
                            "Usuarios" => dbFileManager.CaminhoArquivoUsuarios,
                            "Historico" => dbFileManager.CaminhoArquivoLogs,
                            "Movimentacoes" => dbFileManager.CaminhoArquivoMovimentacoes,
                            _ => throw new InvalidOperationException($"Tabela '{tabela}' não reconhecida.")
                        };

                        if (File.Exists(caminhoArquivo))
                        {
                            var json = await File.ReadAllTextAsync(caminhoArquivo);
                            var objetos = tabela switch
                            {
                                "Produtos" => JsonSerializer.Deserialize<List<ProdutoData>>(json)?.Cast<object>().ToList(),
                                "Usuarios" => JsonSerializer.Deserialize<List<UsuarioData>>(json)?.Cast<object>().ToList(),
                                "Historico" => JsonSerializer.Deserialize<List<LogData>>(json)?.Cast<object>().ToList(),
                                "Movimentacoes" => JsonSerializer.Deserialize<List<MovimentacaoData>>(json)?.Cast<object>().ToList(),
                                _ => throw new InvalidOperationException($"Tabela '{tabela}' não reconhecida.")
                            };

                            if (objetos != null)
                            {
                                listaObjetos.AddRange(objetos);
                            }
                        }

                        UpdateStatusBar("Dados carregados em Arquivos Locais - Banco de Dados Offline", Colors.Purple);
                        UpdateConnectionStatus("Desconectado");

                        // Inicia timers
                        _saveCacheTimer.Start();
                        _connectDatabaseTimer.Start();
                    }

                    // Adiciona a lista de objetos ao cache
                    DadosCache.Tabelas[tabela] = listaObjetos;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar as tabelas no cache: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatusBar("Erro ao carregar dados", Colors.DarkRed);

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


        // Função para abrir conexão com banco de dados
        private void ConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            // Abre a aba de banco de dados
            ContentArea.Content = null;
            ContentArea.Content = new BancoDadosUserControl();
        }
        private void ConnectionButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Atualizar o ToolTip com base na conexão com o banco de dados
            if (DatabaseConnect.Database != null)
            {
                ConnectionToolTip.Content = "Conectado ao banco de dados - usando dados online";
            }
            else
            {
                ConnectionToolTip.Content = "Desconectado do banco de dados - usando arquivos locais offline. Reconecte para sincronizar informações";
            }
        }
    }
}
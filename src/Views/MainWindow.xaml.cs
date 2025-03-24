using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;
using WMS_RadiadoresLemos_WPF.src.Views;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class MainWindow : Window
    {
        public static MainWindow? _instance;
        private bool isLogoutInitiated = false;
        private int _notificationCount = 0;
        private DispatcherTimer _saveCacheTimer;
        private DispatcherTimer _connectDatabaseTimer;

        // Variável para armazenar o usuário logado
        public static UsuarioData? UsuarioLogado { get; set; }

        // Variáveis para controle de conexão com o banco de dados
        public static bool isSincronized;
        public static bool isAppOffline;

        // Variáveis de controle
        private List<UserControl> _userControls;
        private int _currentIndex;

        public MainWindow()
        {
            // Inicia processo de login
            InitializeComponent();
            _instance = this;
            StartApplication();

            // Adiciona o evento de alerta adicionado
            // AlertaCache.AlertaAdicionado += OnAlertaAdicionado;

            this.Closing += Window_Closing;

            // Configura o timer para salvar o cache periodicamente
            _saveCacheTimer = new DispatcherTimer();
            _saveCacheTimer.Interval = TimeSpan.FromMinutes(5); // Salva o cache a cada 5 minutos
            _saveCacheTimer.Tick += SaveCacheTimer_Tick;

            // Configura cache para conectar com banco periodicamente
            _connectDatabaseTimer = new DispatcherTimer();
            _connectDatabaseTimer.Interval = TimeSpan.FromMinutes(1); // Tenta conectar a cada 1 minuto
            _connectDatabaseTimer.Tick += ConnectDatabaseTimer_Tick;

            // Inicializa a lista de UserControls
            _userControls = new List<UserControl>
        {
            new AddEntradaSaidaUserControl(),
            new VendasUserControl(),
            new RegistroUserControl(),
            new ControleEstoqueUserControl(),
            new DashboardUserControl(),
            new NotificacoesUserControl(),
            new ConfiguracaoUserControl()
        };

            // Define o índice inicial
            _currentIndex = 0;

            // Exibe o primeiro controle
            ContentArea.Content = _userControls[_currentIndex];
            UpdateTitle();
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
                                            "Arquivos locais sincornizados - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
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

                // Verifica se a conexão foi estabelecida
                if (DatabaseConnect.IsConnected)
                {
                    // Sincroniza dados com banco de dados
                    SincronizarBancoDados();
                    isAppOffline = false;
                    isSincronized = true;
                }
                else
                {
                    throw new Exception("Não foi possível estabelecer conexão com o banco de dados.");
                }
            }
            catch (Exception ex)
            {
                // Log de erro
                Console.WriteLine($"Erro ao conectar ao banco de dados: {ex.Message}");

                isSincronized = false;
                isAppOffline = true;

                // Espera 3 segundos
                await Task.Delay(3000);

                // Altera a barra de status
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

            // Configura a visibilidade dos botões de acordo com o tipo de usuário
            ConfigurarVisibilidadeBotoes();
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

                // Desconecta do banco de dados
                DatabaseConnect.Disconnect();
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Erro ao registrar a saída do usuário no log: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

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
                //MessageBox.Show($"Erro ao registrar a entrada do usuário no log: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

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

                // Atualiza o TextBlock com o cargo do usuário
                var cargoTextBlock = (TextBlock)Perfil.FindName("CargoTextBlock");
                if (cargoTextBlock != null)
                {
                    cargoTextBlock.Text = UsuarioLogado.Cargo;
                }

                // Atualiza o TextBlock com a matrícula do usuário
                var matriculaTextBlock = (TextBlock)Perfil.FindName("MatriculaTextBlock");
                if (matriculaTextBlock != null)
                {
                    matriculaTextBlock.Text = UsuarioLogado.Matrícula;
                }
            }
        }

        public async void SetupDatabaseConnection()
        {
            try
            {

                // Estabelece a conexão com o banco de dados Firestore
                DatabaseConnect.SetEnvironmentVarible();

                // Testa a conexão com o banco de dados
                DatabaseConnect.TestConnection();

                // Carrega todas as tabelas no cache
                await CarregarTodasTabelasNoCache(DatabaseConnect.IsConnected);
            }
            catch (Exception ex)
            {
                _connectDatabaseTimer.Start();
                //MessageBox.Show($"Erro ao carregar dados, com banco de dados e com arquivos locais: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

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

        public async void SincronizarBancoDados()
        {
            // Fecha qualquer aba que esteja aberta
            ContentArea.Content = null;

            // Deixa carregamento visível
            LoadingScreen.Visibility = Visibility.Visible;

            // Sincroniza arquivos cache enviando para o banco de dados
            DatabaseFileManager gerenciadorDeArquivos = new DatabaseFileManager();
            await gerenciadorDeArquivos.SincronizarDadosComBancoAsync();

            // Desativa o modo offline
            desativarModoOffline();

            // Oculta barra de carregamento
            LoadingScreen.Visibility = Visibility.Collapsed;
        }



        // Abas
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            // Altera a cor do botão clicado nos itens do menu
            foreach (var child in MenuItemsPanel.Children)
            {
                if (child is Button button)
                {
                    button.Style = (Style)FindResource("MenuButtonStyle");
                    // Altera a cor do texto e do ícone do botão
                    foreach (var innerChild in ((StackPanel)button.Content).Children)
                    {
                        if (innerChild is TextBlock textBlock)
                        {
                            textBlock.Foreground = (Brush)FindResource("TextBrush");
                        }
                        else if (innerChild is System.Windows.Shapes.Path path)
                        {
                            path.Fill = (Brush)FindResource("TextBrush");
                        }
                    }
                }
            }

            // Altera a cor do botão clicado no rodapé do menu
            foreach (var child in MenuItemsFooterPanel.Children)
            {
                if (child is Button button)
                {
                    button.Style = (Style)FindResource("MenuButtonStyle");
                    // Altera a cor do texto e do ícone do botão
                    foreach (var innerChild in ((StackPanel)button.Content).Children)
                    {
                        if (innerChild is TextBlock textBlock)
                        {
                            textBlock.Foreground = (Brush)FindResource("TextBrush");
                        }
                        else if (innerChild is System.Windows.Shapes.Path path)
                        {
                            path.Fill = (Brush)FindResource("TextBrush");
                        }
                    }
                }
            }

            // Se o botão clicado for um botão
            if (sender is Button clickedButton)
            {
                // Altera o estilo do botão clicado
                clickedButton.Style = (Style)FindResource("MenuItemSelectedStyle");

                // e define a aba correspondente
                if (clickedButton == BtnAdicionar)
                {
                    ContentArea.Content = new AddEntradaSaidaUserControl();
                    TitleTextBlock.Text = "Entrada/Saída";
                }
                else if (clickedButton == BtnVendas)
                {
                    ContentArea.Content = new VendasUserControl();
                    TitleTextBlock.Text = "Vendas";
                }
                else if (clickedButton == BtnRegistro)
                {
                    ContentArea.Content = new RegistroUserControl();
                    TitleTextBlock.Text = "Registro";
                }
                else if (clickedButton == BtnEstoque)
                {
                    ContentArea.Content = new ControleEstoqueUserControl();
                    TitleTextBlock.Text = "Estoque";
                }
                else if (clickedButton == BtnDashboard)
                {
                    ContentArea.Content = new DashboardUserControl();
                    TitleTextBlock.Text = "Relatório";
                }
                else if (clickedButton == BtnNotificacoes)
                {
                    ContentArea.Content = new NotificacoesUserControl();
                    TitleTextBlock.Text = "Notificações";
                }
                else if (clickedButton == BtnConfiguracoes)
                {
                    ContentArea.Content = new ConfiguracaoUserControl();
                    TitleTextBlock.Text = "Configurações";
                }

                // Altera a cor do texto e do ícone do botão clicado
                foreach (var innerChild in ((StackPanel)clickedButton.Content).Children)
                {
                    if (innerChild is TextBlock textBlock)
                    {
                        textBlock.Foreground = (Brush)FindResource("AccentBrush");
                    }
                    else if (innerChild is System.Windows.Shapes.Path path)
                    {
                        path.Fill = (Brush)FindResource("AccentBrush");
                    }
                }
            }
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

                        // Desconecta do banco de dados
                        DatabaseConnect.Disconnect();
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
                //MessageBox.Show($"Erro ao realizar logout: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

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

        // Função para carregar todas as tabelas no cache
        private async Task CarregarTodasTabelasNoCache(bool isConnected)
        {
            try
            {
                var db = DatabaseConnect.Database;

                // Lista de tabelas a serem carregadas no cache
                var tabelas = new List<string>
                {
                    "Produtos",
                    "Usuarios",
                    "Historico",
                    "Movimentacoes"
                };

                var dbFileManager = new DatabaseFileManager();

                // Se conectado
                if (isConnected)
                {
                    // Carrega cache com dados do banco de dados

                    // Se não estiver sincronizado
                    if (!isSincronized)
                    {
                        // Sincroniza dados com banco de dados
                        SincronizarBancoDados();
                    }

                    // Carrega todas as tabelas no cache usando o banco
                    foreach (var tabela in tabelas)
                    {
                        var listaObjetos = new List<object>();

                        // Pega a referência da tabela
                        var tabelaRef = db.Collection(tabela);

                        // Pega o snapshot da tabela
                        var snapshot = await tabelaRef.GetSnapshotAsync();

                        // Para cada documento no snapshot
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

                        // Salva a lista de objetos no cache
                        DadosCache.Tabelas[tabela] = listaObjetos;
                    }

                    // Garantir que o app esteja online
                    desativarModoOffline();
                }
                // Se não está conectado
                else
                {
                    // Carrega cache com dados dos arquivos locais

                    // Carrega todas as tabelas no cache usando os arquivos locais
                    foreach (var tabela in tabelas)
                    {
                        var listaObjetos = new List<object>();

                        // Carrega os dados dos arquivos locais
                        string caminhoArquivo = tabela switch
                        {
                            "Produtos" => dbFileManager.CaminhoArquivoProdutos,
                            "Usuarios" => dbFileManager.CaminhoArquivoUsuarios,
                            "Historico" => dbFileManager.CaminhoArquivoLogs,
                            "Movimentacoes" => dbFileManager.CaminhoArquivoMovimentacoes,
                            _ => throw new InvalidOperationException($"Tabela '{tabela}' não reconhecida.")
                        };

                        // Se o arquivo existir
                        if (File.Exists(caminhoArquivo))
                        {
                            // Lê o arquivo e desserializa os dados
                            var json = await File.ReadAllTextAsync(caminhoArquivo);
                            var objetos = tabela switch
                            {
                                "Produtos" => JsonSerializer.Deserialize<List<ProdutoData>>(json)?.Cast<object>().ToList(),
                                "Usuarios" => JsonSerializer.Deserialize<List<UsuarioData>>(json)?.Cast<object>().ToList(),
                                "Historico" => JsonSerializer.Deserialize<List<LogData>>(json)?.Cast<object>().ToList(),
                                "Movimentacoes" => JsonSerializer.Deserialize<List<MovimentacaoData>>(json)?.Cast<object>().ToList(),
                                _ => throw new InvalidOperationException($"Tabela '{tabela}' não reconhecida.")
                            };

                            // Se os objetos não forem nulos, adiciona à lista de objetos
                            if (objetos != null)
                            {
                                listaObjetos.AddRange(objetos);
                            }
                        }

                        // Com os objetos carregados, salva a lista de objetos no cache
                        DadosCache.Tabelas[tabela] = listaObjetos;
                    }

                    // Inicia "Modo Offline"
                    ativarModoOffline();
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Erro ao carregar as tabelas no cache: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

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


        // Função que altera visibilidade dos botões de acordo com o tipo de usuário
        private void ConfigurarVisibilidadeBotoes()
        {
            if (UsuarioLogado == null)
            {
                return;
            }

            // Exemplo de cargos e visibilidade dos botões
            switch (UsuarioLogado.Cargo)
            {
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            // Navega para a aba anterior
            if (_currentIndex > 0)
            {
                _currentIndex--;
                ContentArea.Content = _userControls[_currentIndex];
                UpdateTitle();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            // Navega para a próxima aba
            if (_currentIndex < _userControls.Count - 1)
            {
                _currentIndex++;
                ContentArea.Content = _userControls[_currentIndex];
                UpdateTitle();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Atualiza a aba atual
            if (ContentArea.Content is UserControl currentControl)
            {
                ContentArea.Content = Activator.CreateInstance(currentControl.GetType());
            }
        }

        private void UpdateTitle()
        {
            // Atualiza o título com base no controle atual
            if (ContentArea.Content is AddEntradaSaidaUserControl)
            {
                TitleTextBlock.Text = "Entrada/Saída";
            }
            else if (ContentArea.Content is VendasUserControl)
            {
                TitleTextBlock.Text = "Vendas";
            }
            else if (ContentArea.Content is RegistroUserControl)
            {
                TitleTextBlock.Text = "Registro";
            }
            else if (ContentArea.Content is ControleEstoqueUserControl)
            {
                TitleTextBlock.Text = "Estoque";
            }
            else if (ContentArea.Content is DashboardUserControl)
            {
                TitleTextBlock.Text = "Relatório";
            }
            else if (ContentArea.Content is NotificacoesUserControl)
            {
                TitleTextBlock.Text = "Notificações";
            }
            else if (ContentArea.Content is ConfiguracaoUserControl)
            {
                TitleTextBlock.Text = "Configurações";
            }
        }

        // Função que representa a animação de notificação de alerta
        //private void OnAlertaAdicionado(AlertaData alerta)
        //{
        //    // Incrementa a contagem de notificações
        //    _notificationCount++;

        //    // Tornar o ícone de notificação visível
        //    NotificationButton.Visibility = Visibility.Visible;

        //    // Altera a cor do ícone de notificação para vermelho por 2 segundos e depois fica vermelho
        //    ColorAnimation colorAnimation = new ColorAnimation
        //    {
        //        From = Colors.Transparent,
        //        To = (Color)ColorConverter.ConvertFromString("#990000"),
        //        Duration = new Duration(TimeSpan.FromSeconds(0.5)),
        //        AutoReverse = true,
        //        RepeatBehavior = new RepeatBehavior(4) // Pisca 4 vezes (2 segundos)
        //    };

        //    // Aplica a animação ao fundo do botão de notificação
        //    NotificationButton.Background = new SolidColorBrush(Colors.Transparent);
        //    NotificationButton.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);

        //    // Define a cor final como vermelho após a animação
        //    colorAnimation.AutoReverse = false;
        //    NotificationButton.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);

        //    // Atualizar o ToolTip com a quantidade de notificações
        //    NotificationToolTip.Content = $"Você tem {_notificationCount} novas notificações";
        //}


        // Função para iniciar processo de "Modo Offline"
        public void ativarModoOffline()
        {
            // Inicia o processo de "Modo Offline"

            // Inicia timers caso não estejam ativos
            if (!_saveCacheTimer.IsEnabled)
            {
                // _saveCacheTimer.Start();
            }
            if (!_connectDatabaseTimer.IsEnabled)
            {
                // _connectDatabaseTimer.Start();
            }

            // Adiciona alerta
            AlertaCache.AdicionarAlerta("Aviso",
                                        "Falha na conexão com o banco de dados",
                                        "Não foi possível conectar ao banco de dados. No entanto, os dados foram carregados dos arquivos locais.",
                                        "Reconecte para sincronizar informações (existe uma tentativa de conexão a cada 1 minuto)");

            // Desliga conexão
            DatabaseConnect.Disconnect();
            isSincronized = false;
            isAppOffline = true;
        }
        public void desativarModoOffline()
        {
            // Finaliza o processo de "Modo Offline"
            _saveCacheTimer.Stop();
            _connectDatabaseTimer.Stop();

            // Adiciona alerta
            AlertaCache.AdicionarAlerta("Aviso",
                                        "Sincronização Completa - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                                        "Os dados foram carregados do banco de dados com sucesso.",
                                        "Aplicação está online - pronta para ser usada");

            // Atualiza variáveis
            isSincronized = true;
            isAppOffline = false;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;
using WMS_RadiadoresLemos_WPF.src.Views;
using WMS_RadiadoresLemos_WPF;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class MainWindow : Window
    {
        public static MainWindow? _instance;
        private bool isLogoutInitiated = false;
        private int _notificationCount = 0;
        private DispatcherTimer _connectDatabaseTimer;
        private string _caminhoArquivoUsuarios = Path.Combine("DadosBancoDeDadosOffline", "usuarios.json");

        // Variável para armazenar o usuário logado
        public static UsuarioData? UsuarioLogado { get; set; }

        // Variáveis para controle de conexão com o banco de dados
        public static bool isSincronized;

        // Variáveis de controle
        private List<UserControl> _userControls;
        private int _currentIndex;

        // Elementos da interface
        private Button? NotificationButton;
        private ToolTip? NotificationToolTip;

        private readonly UsuariosUserControl usuariosUserControl;
        private readonly ControleEstoqueUserControl controleEstoqueUserControl;
        private readonly RegistroUserControl registroUserControl;
        private DispatcherTimer timer;

        public MainWindow()
        {
            InitializeComponent();
            _instance = this;

            // Configura timer para conectar com banco periodicamente
            _connectDatabaseTimer = new DispatcherTimer();
            _connectDatabaseTimer.Interval = TimeSpan.FromMinutes(1);
            _connectDatabaseTimer.Tick += ConnectDatabaseTimer_Tick;

            // Adiciona o evento de alerta adicionado
            Alerta.AlertaAdicionado += OnAlertaAdicionado;

            this.Closing += Window_Closing;

            // Inicializa a lista de UserControls
            _userControls = new List<UserControl>
        {
                new AddEntradaSaídaUserControl(),
            new VendasUserControl(),
            new RegistroUserControl(),
            new ControleEstoqueUserControl(),
            new DashboardUserControl(),
            new NotificacoesUserControl(),
            new ConfiguracaoUserControl()
        };

            // Define o índice inicial
            _currentIndex = 0;
            UpdateTitle();

            // Inicializa elementos da interface
            try
            {
                NotificationButton = this.FindName("NotificationButton") as Button;
                NotificationToolTip = this.FindName("NotificationToolTip") as ToolTip;

                if (NotificationButton != null)
                {
                    NotificationButton.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao inicializar elementos da interface: {ex.Message}");
            }

            // Inicializa os UserControls
            usuariosUserControl = new UsuariosUserControl();
            controleEstoqueUserControl = new ControleEstoqueUserControl();
            registroUserControl = new RegistroUserControl();

            // Inicia a aplicação
            IniciarTimer();
            CarregarTodasTabelasNoCache().Wait();
        }

        private async void ConnectDatabaseTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                DatabaseConnect.SetEnvironmentVarible();

                if (DatabaseConnect.Database != null)
                {
                    isSincronized = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao conectar ao banco de dados: {ex.Message}");
                isSincronized = false;
                await Task.Delay(3000);
            }
        }

        private void StartApplication()
        {
            SetupDatabaseConnection();
            RegistrarEntradaLog();
            SetupUsuarioLogado();
            ConfigurarVisibilidadeBotoes();
        }

        private async void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isLogoutInitiated) return;

            try
            {
                if (UsuarioLogado == null) return;

                var log = new LogData
                {
                    Data = DateTime.UtcNow,
                    Tipo = "OPERACIONAL",
                    Nivel = "Usuário",
                    Detalhes = $"Usuário {UsuarioLogado.Nome} realizou logout",
                    Usuario = UsuarioLogado.Nome
                };
                await LogHistorico.SalvarLog(log);

                UsuarioLogado = null;
                _connectDatabaseTimer.Stop();

                DatabaseConnect.Disconnect();
            }
            catch (Exception ex)
            {
                Alerta.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Não foi possível registrar a saída do usuário no log. Possíveis motivos:\n" +
                                            "- Problemas de conexão com o sistema;\n" +
                                            "- Configurações incorretas do sistema;\n" +
                                            "- Serviço do sistema indisponível.",
                                            "- Tente novamente;\n" +
                                            "- Feche a aplicação e abra novamente.");
            }
        }

        private async void RegistrarEntradaLog()
        {
            try
            {
                if (UsuarioLogado == null) return;

                var log = new LogData
                {
                    Data = DateTime.UtcNow,
                    Tipo = "OPERACIONAL",
                    Nivel = "Usuário",
                    Detalhes = $"Usuário {UsuarioLogado?.Nome} entrou no sistema",
                    Usuario = UsuarioLogado?.Nome ?? "Usuário não identificado"
                };
                await LogHistorico.SalvarLog(log);
            }
            catch (Exception ex)
            {
                Alerta.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Não foi possível registrar a entrada do usuário no log. Possíveis motivos:\n" +
                                            "- Problemas de conexão com o sistema;\n" +
                                            "- Configurações incorretas do sistema;\n" +
                                            "- Serviço do sistema indisponível.",
                                            "- Tente novamente;\n" +
                                            "- Feche a aplicação e abra novamente.");
            }
        }

        private void SetupUsuarioLogado()
        {
            if (UsuarioLogado != null)
            {
                var nomeTextBlock = (TextBlock)Perfil.FindName("NomeUsuarioTextBlock");
                if (nomeTextBlock != null)
                {
                    nomeTextBlock.Text = UsuarioLogado.Nome;
                }

                var cargoTextBlock = (TextBlock)Perfil.FindName("CargoTextBlock");
                if (cargoTextBlock != null)
                {
                    cargoTextBlock.Text = UsuarioLogado.Cargo;
                }

                var matriculaTextBlock = (TextBlock)Perfil.FindName("MatriculaTextBlock");
                if (matriculaTextBlock != null)
                {
                    matriculaTextBlock.Text = UsuarioLogado.Matricula;
                }
            }
        }

        public async void SetupDatabaseConnection()
        {
            try
            {
                DatabaseConnect.SetEnvironmentVarible();
                await CarregarTodasTabelasNoCache();
            }
            catch (Exception ex)
            {
                _connectDatabaseTimer.Start();
                Alerta.AdicionarAlerta("Erro",
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
            ContentArea.Content = null;
            LoadingScreen.Visibility = Visibility.Visible;

            try
            {
                var db = DatabaseConnect.Database;
                if (db != null)
                {
                    var produtosCollection = db.GetCollection<ProdutoData>("produtos");
                    var usuariosCollection = db.GetCollection<UsuarioData>("usuarios");
                    var historicoCollection = db.GetCollection<LogData>("historico");
                    var movimentacoesCollection = db.GetCollection<MovimentacaoData>("movimentacoes");

                    produtosCollection.Update(produtosCollection.FindAll().ToList());
                    usuariosCollection.Update(usuariosCollection.FindAll().ToList());
                    historicoCollection.Update(historicoCollection.FindAll().ToList());
                    movimentacoesCollection.Update(movimentacoesCollection.FindAll().ToList());
                }

                isSincronized = true;
                LoadingScreen.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Alerta.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                    "Não foi possível sincronizar os dados com o banco. Possíveis motivos:\n" +
                    "- Problemas de conexão com o banco;\n" +
                    "- Dados corrompidos;\n" +
                    "- Falha na operação de sincronização.",
                    "- Verifique a conexão com o banco;\n" +
                    "- Tente novamente mais tarde.");
            }
        }

        private async Task CarregarTodasTabelasNoCache()
        {
            try
            {
                if (DatabaseConnect.Database != null)
                {
                    usuariosUserControl.AtualizarTabelaUsuarios();
                    controleEstoqueUserControl.AtualizarTabelaEstoque();
                    registroUserControl.CarregarEntradas();
                    registroUserControl.CarregarSaidas();
                    registroUserControl.CarregarHistorico();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar dados do banco: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigurarVisibilidadeBotoes()
        {
            if (UsuarioLogado == null)
            {
                return;
            }

            switch (UsuarioLogado.Cargo)
            {
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
                ContentArea.Content = _userControls[_currentIndex];
                UpdateTitle();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex < _userControls.Count - 1)
            {
                _currentIndex++;
                ContentArea.Content = _userControls[_currentIndex];
                UpdateTitle();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (ContentArea.Content is UserControl currentControl)
            {
                ContentArea.Content = Activator.CreateInstance(currentControl.GetType());
            }
        }

        private void UpdateTitle()
        {
            if (ContentArea.Content is AddEntradaSaídaUserControl)
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

        public void Reload()
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void OnAlertaAdicionado(AlertaData alerta)
        {
            _notificationCount++;

            if (NotificationButton == null)
            {
                NotificationButton = (Button)FindName("NotificationButton");
            }

            if (NotificationToolTip == null)
            {
                NotificationToolTip = (ToolTip)FindName("NotificationToolTip");
            }

            if (NotificationButton == null) return;

            NotificationButton.Visibility = Visibility.Visible;

            ColorAnimation colorAnimation = new ColorAnimation
            {
                From = Colors.Transparent,
                To = (Color)ColorConverter.ConvertFromString("#990000"),
                Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(4)
            };

            NotificationButton.Background = new SolidColorBrush(Colors.Transparent);
            NotificationButton.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);

            colorAnimation.AutoReverse = false;
            NotificationButton.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);

            if (NotificationToolTip != null)
            {
                NotificationToolTip.Content = $"Você tem {_notificationCount} novas notificações";
            }
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBoxResult result = MessageBox.Show("Você tem certeza que deseja sair?", "Confirmar Logout", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    isLogoutInitiated = true;
                    this.Hide();

                    if (UsuarioLogado != null)
                    {
                        var log = new LogData
                        {
                            Data = DateTime.UtcNow,
                            Tipo = "OPERACIONAL",
                            Nivel = "Usuário",
                            Detalhes = $"Usuário {UsuarioLogado.Nome} realizou logout",
                            Usuario = UsuarioLogado.Nome
                        };
                        await LogHistorico.SalvarLog(log);

                        UsuarioLogado = null;
                        _connectDatabaseTimer.Stop();
            DatabaseConnect.Disconnect();
                    }

                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                Alerta.AdicionarAlerta("Erro",
                    ex.Message.ToString(),
                    "Não foi possível realizar o logout. Possíveis motivos:\n" +
                    "- Problemas de conexão com o sistema;\n" +
                    "- Configurações incorretas do sistema;\n" +
                    "- Serviço do sistema indisponível.",
                    "- Tente novamente;\n" +
                    "- Feche a aplicação e abra novamente.");
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                switch (button.Name)
                {
                    case "BtnAdicionar":
                        ContentArea.Content = new AddEntradaSaídaUserControl();
                        break;
                    case "BtnVendas":
                        ContentArea.Content = new VendasUserControl();
                        break;
                    case "BtnRegistro":
                        ContentArea.Content = new RegistroUserControl();
                        break;
                    case "BtnEstoque":
                        ContentArea.Content = new ControleEstoqueUserControl();
                        break;
                    case "BtnDashboard":
                        ContentArea.Content = new DashboardUserControl();
                        break;
                    case "BtnNotificacoes":
                        ContentArea.Content = new NotificacoesUserControl();
                        break;
                    case "BtnConfiguracoes":
                        ContentArea.Content = new ConfiguracaoUserControl();
                        break;
                }
                UpdateTitle();
            }
        }

        private void UsuariosButton_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = usuariosUserControl;
            usuariosUserControl.AtualizarTabelaUsuarios();
        }

        private void EstoqueButton_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = controleEstoqueUserControl;
            controleEstoqueUserControl.AtualizarTabelaEstoque();
        }

        private void RegistroButton_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = registroUserControl;
            registroUserControl.CarregarEntradas();
            registroUserControl.CarregarSaidas();
            registroUserControl.CarregarHistorico();
        }

        private void IniciarTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(1);
            timer.Tick += async (s, e) =>
            {
                await CarregarTodasTabelasNoCache();
            };
            timer.Start();
        }
    }
}
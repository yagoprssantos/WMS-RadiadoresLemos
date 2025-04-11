using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        private bool isThemeChange = false;
        private int _notificationCount = 0;

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

        public MainWindow()
        {
            InitializeComponent();
            _instance = this;

            // Carregar os dados do usuário logado
            usuariosUserControl = new UsuariosUserControl();
            controleEstoqueUserControl = new ControleEstoqueUserControl();
            registroUserControl = new RegistroUserControl();

            InicializarElementos();
            ConfigurarEntrada();
            ConfigurarVisibilidadeBotoes();
        }

        // Inicia os elementos da interface
        private void InicializarElementos()
        {
            AdicionarEventos();
            InicializarUserControls();
            //InicializarNotificações();
        }

        // Adiciona eventos necessários
        private void AdicionarEventos()
        {
            Alerta.AlertaAdicionado += OnAlertaAdicionado;
            this.Closing += Window_Closing;
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


        // Inicializa os UserControls
        private void InicializarUserControls()
        {
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

            _currentIndex = 0;
            UpdateTitle();
        }

        // Inicializa os elementos para notificações
        private void InicializarNotificações()
        {
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

        }


        // Configura a entrada do usuário no sistema
        private void ConfigurarEntrada()
        {
            if (UsuarioLogado != null)
            {
                SetupUsuarioLogado();
                RegistrarEntradaLog();
            }
            else
            {
                MessageBox.Show("Usuário não logado. Por favor, faça login para continuar.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        // Configura o usuário logado na interface e seus dados
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


        // Configura a visibilidade dos botões de acordo com o cargo do usuário
        private void ConfigurarVisibilidadeBotoes()
        {
            // Verifica se o usuário está logado
            if (UsuarioLogado == null)
            {
                return;
            }

            // Verifica o cargo do usuário e ajusta a visibilidade dos botões
            // TODO: Implementar lógica para verificar o cargo do usuário e ajustar a visibilidade dos botões
            switch (UsuarioLogado.Cargo)
            {
            }
        }


        // Método para lidar com o clique nos botões do menu
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
                        else if (innerChild is Image image)
                        {
                            // Define a imagem não selecionada
                            string imageName = GetImageName(image.Name, "NotSelected");
                            image.Source = new BitmapImage(new Uri($"/src/Resources/Icons/NotSelected/{imageName}.png", UriKind.Relative));
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
                        else if (innerChild is Image image)
                        {
                            // Define a imagem não selecionada
                            string imageName = GetImageName(image.Name, "NotSelected");
                            image.Source = new BitmapImage(new Uri($"/src/Resources/Icons/NotSelected/{imageName}.png", UriKind.Relative));
                        }
                    }
                }
            }

            // Se o botão clicado for um botão
            if (sender is Button clickedButton)
            {
                // Altera o estilo do botão clicado
                clickedButton.Style = (Style)FindResource("MenuItemSelectedStyle");

                // Define a aba correspondente
                switch (clickedButton.Name)
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

                // Atualiza o título
                UpdateTitle();
                UpdateIcon();

                // Altera a cor do texto e do ícone do botão clicado
                foreach (var innerChild in ((StackPanel)clickedButton.Content).Children)
                {
                    if (innerChild is TextBlock textBlock)
                    {
                        textBlock.Foreground = (Brush)FindResource("AccentBrush");
                    }
                    else if (innerChild is Image image)
                    {
                        // Define a imagem selecionada
                        string imageName = GetImageName(image.Name, "Selected");
                        image.Source = new BitmapImage(new Uri($"/src/Resources/Icons/Selected/{imageName}.png", UriKind.Relative));
                    }
                }
            }
        }


        // Método para lidar com botões de navegação
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


        // Método para lidar com o botão de logout
        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBoxResult result = MessageBox.Show("Você tem certeza que deseja sair?", "Confirmar Logout", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    isLogoutInitiated = true;
                    this.Hide();

                    // Verifica se o usuário está logado e registra o log de logout
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

        // Método para obter o nome da imagem com base no nome do ícone e no estado
        private string GetImageName(string iconName, string state)
        {
            return iconName switch
            {
                "IconAdicionar" => state == "Selected" ? "PlusS" : "PlusNS",
                "IconVendas" => state == "Selected" ? "PranchetaS" : "PranchetaNS",
                "IconRegistro" => state == "Selected" ? "historicos" : "HistoricoNS",
                "IconEstoque" => state == "Selected" ? "CaixaS" : "CaixaNS",
                "IconDashboard" => state == "Selected" ? "GraficoS" : "GraficoNS",
                "IconNotificacoes" => state == "Selected" ? "SinoS" : "SinoNS",
                "IconConfiguracoes" => state == "Selected" ? "EngrenagemS" : "EngrenagemNS",
                "IconSair" => state == "Selected" ? "SairS" : "SairNS",
                _ => throw new ArgumentException("Nome de ícone desconhecido", nameof(iconName))
            };
        }

        // Métodos para atualizar o título da janela e o ícone do botão
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
        private void UpdateIcon()
        {
            Uri? iconUri = null; 

            if (ContentArea.Content is AddEntradaSaídaUserControl)
            {
                iconUri = new Uri("/src/Resources/Icons/Selected/PlusS.png", UriKind.Relative);
            }
            else if (ContentArea.Content is VendasUserControl)
            {
                iconUri = new Uri("/src/Resources/Icons/Selected/PranchetaS.png", UriKind.Relative);
            }
            else if (ContentArea.Content is RegistroUserControl)
            {
                iconUri = new Uri("/src/Resources/Icons/Selected/historicos.png", UriKind.Relative);
            }
            else if (ContentArea.Content is ControleEstoqueUserControl)
            {
                iconUri = new Uri("/src/Resources/Icons/Selected/CaixaS.png", UriKind.Relative);
            }
            else if (ContentArea.Content is DashboardUserControl)
            {
                iconUri = new Uri("/src/Resources/Icons/Selected/GraficoS.png", UriKind.Relative);
            }
            else if (ContentArea.Content is NotificacoesUserControl)
            {
                iconUri = new Uri("/src/Resources/Icons/Selected/SinoS.png", UriKind.Relative);
            }
            else if (ContentArea.Content is ConfiguracaoUserControl)
            {
                iconUri = new Uri("/src/Resources/Icons/Selected/EngrenagemS.png", UriKind.Relative);
            }

            if (iconUri != null)
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = iconUri;
                bitmap.DecodePixelWidth = 64; 
                bitmap.DecodePixelHeight = 64;
                bitmap.EndInit();
                RenderOptions.SetBitmapScalingMode(bitmap, BitmapScalingMode.HighQuality);
                IconImage.Source = bitmap;
            }
            else
            {
                // Limpa o ícone se nenhum controle corresponder ou se ContentArea estiver vazio
                IconImage.Source = null;
            }
        }

        // Método para lidar com o fechamento da janela
        private async void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isLogoutInitiated || isThemeChange) return; // Verifica se é logout ou troca de tema

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

        // Recarrega a tela
        public void Reload()
        {
            isThemeChange = true;
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}
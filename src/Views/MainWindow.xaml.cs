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
        private readonly UsuariosUserControl usuariosUserControl;
        private readonly ControleEstoqueUserControl controleEstoqueUserControl;
        private readonly RegistroUserControl registroUserControl;

        private NavigationService _navigationService;

        public MainWindow()
        {
            InitializeComponent();
            _instance = this;

            // Escuta o evento de alteração na contagem de notificações
            Alerta.ContagemAlterada += AtualizarBotaoNotificacoes;

            // Inicializa o serviço de navegação
            _navigationService = new NavigationService(this, ContentArea, TitleTextBlock, IconImage);

            usuariosUserControl = new UsuariosUserControl();
            controleEstoqueUserControl = new ControleEstoqueUserControl();
            registroUserControl = new RegistroUserControl();

            InicializarUserControls();
            ConfigurarEntrada();
            ConfigurarVisibilidadeBotoes();
        }

        // Inicializa os UserControls
        private void InicializarUserControls()
        {
            _userControls = new List<UserControl>
                    {
                        new ComprasUserControl(),
                        new VendasUserControl(),
                        new RegistroUserControl(),
                        new ControleEstoqueUserControl(),
                        new CadastroUserControl(),
                        new DashboardUserControl(),
                        new NotificacoesUserControl(),
                        new ConfiguracaoUserControl()
                    };

            _currentIndex = 0;
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

        // Método para atualizar o botão de notificações
        private void AtualizarBotaoNotificacoes(int totalNovasNotificacoes)
        {
            _notificationCount = totalNovasNotificacoes; // Atualiza a contagem interna
            var notificacoesButton = (Button)FindName("BtnNotificacoes");
            var notificacoesIcon = (Image)FindName("IconNotificacoes");
            var notificacoesText = (TextBlock)FindName("TextNotificacoes");

            if (notificacoesButton != null && notificacoesIcon != null && notificacoesText != null)
            {
                notificacoesText.Text = totalNovasNotificacoes > 0
                    ? $"Notificações ({totalNovasNotificacoes})"
                    : "Notificações";

                notificacoesIcon.Source = new BitmapImage(new Uri("/assets/Icons/NotSelected/SinoNotNS.png", UriKind.Relative));
            }
        }

        // Método para lidar com o clique nos botões do menu
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                UserControl control = null;
                string title = "";
                string iconPath = "";

                // Define o controle correspondente
                switch (clickedButton.Name)
                {
                    case "BtnCompras":
                        control = new ComprasUserControl();
                        title = "Compras";
                        iconPath = "/assets/Icons/Selected/ComprarS.png";
                        break;
                    case "BtnVendas":
                        control = new VendasUserControl();
                        title = "Vendas";
                        iconPath = "/assets/Icons/Selected/PranchetaS.png";
                        break;
                    case "BtnRegistro":
                        control = new RegistroUserControl();
                        title = "Registro";
                        iconPath = "/assets/Icons/Selected/historicos.png";
                        break;
                    case "BtnEstoque":
                        control = new ControleEstoqueUserControl();
                        title = "Estoque";
                        iconPath = "/assets/Icons/Selected/CaixaS.png";
                        break;
                    case "BtnCadastro":
                        control = new CadastroUserControl();
                        title = "Cadastro";
                        iconPath = "/assets/Icons/Selected/CadastroS.png";
                        break;
                    case "BtnDashboard":
                        control = new DashboardUserControl();
                        title = "Relatório";
                        iconPath = "/assets/Icons/Selected/GraficoS.png";
                        break;
                    case "BtnNotificacoes":
                        control = new NotificacoesUserControl();
                        title = "Notificações";
                        iconPath = "/assets/Icons/Selected/SinoS.png";
                        ResetarBotaoNotificacoes();
                        break;
                    case "BtnConfiguracoes":
                        control = new ConfiguracaoUserControl();
                        title = "Configurações";
                        iconPath = "/assets/Icons/Selected/EngrenagemS.png";
                        break;
                }

                if (control != null)
                {
                    _navigationService.Navigate(control, title, iconPath, clickedButton.Name);
                }
            }
        }

        // Método para resetar o botão de notificações
        private void ResetarBotaoNotificacoes()
        {
            Alerta.ResetarNovasNotificacoes(); // Reseta a contagem de novas notificações
            AtualizarBotaoNotificacoes(0); // Atualiza o botão de notificações
        }

        // Método para lidar com botões de navegação
        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (_navigationService.CanGoBack)
            {
                _navigationService.GoBack();
            }
            else
            {
                MessageBox.Show("Não há telas anteriores no histórico.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_navigationService.CanGoForward)
            {
                _navigationService.GoForward();
            }
            else
            {
                MessageBox.Show("Não há telas futuras no histórico.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _navigationService.Refresh();
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
                "IconCompras" => state == "Selected" ? "ComprarS" : "ComprarNS",
                "IconVendas" => state == "Selected" ? "PranchetaS" : "PranchetaNS",
                "IconRegistro" => state == "Selected" ? "historicos" : "HistoricoNS",
                "IconEstoque" => state == "Selected" ? "CaixaS" : "CaixaNS",
                "IconCadastro" => state == "Selected" ? "CadastroS" : "CadastroNS",
                "IconDashboard" => state == "Selected" ? "GraficoS" : "GraficoNS",
                "IconNotificacoes" => state == "Selected" ? "SinoS" : "SinoNS",
                "IconConfiguracoes" => state == "Selected" ? "EngrenagemS" : "EngrenagemNS",
                "IconSair" => state == "Selected" ? "SairS" : "SairNS",
                _ => throw new ArgumentException("Nome de ícone desconhecido", nameof(iconName))
            };
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

        // Adicione esse método para permitir atualização da seleção visual do menu
        public void UpdateMenuSelection(string buttonName)
        {
            // Resetar todos os botões para o estilo padrão
            ResetAllMenuButtons();

            // Encontrar e atualizar o botão específico
            Button button = FindName(buttonName) as Button;
            if (button != null)
            {
                button.Style = (Style)FindResource("MenuItemSelectedStyle");

                // Atualizar cores e ícones
                foreach (var innerChild in ((StackPanel)button.Content).Children)
                {
                    if (innerChild is TextBlock textBlock)
                    {
                        textBlock.Foreground = (Brush)FindResource("AccentBrush");
                    }
                    else if (innerChild is Image image)
                    {
                        string imageName = GetImageName(image.Name, "Selected");
                        image.Source = new BitmapImage(new Uri($"/assets/Icons/Selected/{imageName}.png", UriKind.Relative));
                    }
                }
            }
        }

        // Método para resetar todos os botões do menu
        private void ResetAllMenuButtons()
        {
            // Reset dos botões no painel principal
            foreach (var child in MenuItemsPanel.Children)
            {
                ResetMenuButton(child as Button);
            }

            // Reset dos botões no painel de rodapé
            foreach (var child in MenuItemsFooterPanel.Children)
            {
                ResetMenuButton(child as Button);
            }
        }

        private void ResetMenuButton(Button button)
        {
            if (button == null) return;

            button.Style = (Style)FindResource("MenuButtonStyle");

            foreach (var innerChild in ((StackPanel)button.Content).Children)
            {
                if (innerChild is TextBlock textBlock)
                {
                    textBlock.Foreground = (Brush)FindResource("TextBrush");
                }
                else if (innerChild is Image image)
                {
                    // Caso especial para o botão de notificações
                    if (button.Name == "BtnNotificacoes")
                    {
                        image.Source = new BitmapImage(new Uri(
                            _notificationCount > 0
                                ? "/assets/Icons/NotSelected/SinoNotNS.png"
                                : "/assets/Icons/NotSelected/SinoNS.png",
                            UriKind.Relative));
                    }
                    else
                    {
                        string imageName = GetImageName(image.Name, "NotSelected");
                        image.Source = new BitmapImage(new Uri($"/assets/Icons/NotSelected/{imageName}.png", UriKind.Relative));
                    }
                }
            }
        }
    }
}
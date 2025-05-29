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

        private Stack<UserControl> _navigationHistory = new Stack<UserControl>();
        private Stack<UserControl> _forwardHistory = new Stack<UserControl>();

        public MainWindow()
        {
            InitializeComponent();
            _instance = this;

            // Escuta o evento de alteração na contagem de notificações
            Alerta.ContagemAlterada += AtualizarBotaoNotificacoes;

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
            UpdateTitle();
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
            // Salva a tela atual no histórico de navegação
            if (ContentArea.Content is UserControl currentControl)
            {
                _navigationHistory.Push(currentControl); // Salva a tela atual no histórico
            }

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
                            // Verifica se é o botão de notificações
                            if (button.Name == "BtnNotificacoes")
                            {
                                // Mantém o ícone de notificações se houver notificações não lidas
                                image.Source = new BitmapImage(new Uri(
                                    _notificationCount > 0
                                        ? "/assets/Icons/NotSelected/SinoNotNS.png"
                                        : "/assets/Icons/NotSelected/SinoNS.png",
                                    UriKind.Relative));
                            }
                            else
                            {
                                // Define a imagem não selecionada para os outros botões
                                string imageName = GetImageName(image.Name, "NotSelected");
                                image.Source = new BitmapImage(new Uri($"/assets/Icons/NotSelected/{imageName}.png", UriKind.Relative));
                            }
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
                            image.Source = new BitmapImage(new Uri($"/assets/Icons/NotSelected/{imageName}.png", UriKind.Relative));
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
                    case "BtnCompras":
                        ContentArea.Content = new ComprasUserControl();
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
                    case "BtnCadastro":
                        ContentArea.Content = new CadastroUserControl();
                        break;
                    case "BtnDashboard":
                        ContentArea.Content = new DashboardUserControl();
                        break;
                    case "BtnNotificacoes":
                        ContentArea.Content = new NotificacoesUserControl();
                        ResetarBotaoNotificacoes(); // Reseta o botão de notificações
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
                        image.Source = new BitmapImage(new Uri($"/assets/Icons/Selected/{imageName}.png", UriKind.Relative));
                    }
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
            if (_navigationHistory.Count > 0)
            {
                if (ContentArea.Content is UserControl currentControl)
                {
                    _forwardHistory.Push(currentControl); // Salva a tela atual no histórico de avanço
                }

                ContentArea.Content = _navigationHistory.Pop(); // Carrega a última tela do histórico
                UpdateTitle();
                UpdateIcon();
            }
            else
            {
                MessageBox.Show("Não há telas anteriores no histórico.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_forwardHistory.Count > 0)
            {
                if (ContentArea.Content is UserControl currentControl)
                {
                    _navigationHistory.Push(currentControl); // Salva a tela atual no histórico de navegação
                }

                ContentArea.Content = _forwardHistory.Pop(); // Carrega a próxima tela do histórico de avanço
                UpdateTitle();
                UpdateIcon();
            }
            else
            {
                MessageBox.Show("Não há telas futuras no histórico.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
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

        // Métodos para atualizar o título da janela e o ícone do botão
        private void UpdateTitle()
        {
            if (ContentArea.Content is ComprasUserControl)
            {
                TitleTextBlock.Text = "Compras";
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
            else if (ContentArea.Content is CadastroUserControl)
            {
                TitleTextBlock.Text = "Cadastro";
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

            if (ContentArea.Content is ComprasUserControl)
            {
                iconUri = new Uri("/assets/Icons/Selected/ComprarS.png", UriKind.Relative);
            }
            else if (ContentArea.Content is VendasUserControl)
            {
                iconUri = new Uri("/assets/Icons/Selected/PranchetaS.png", UriKind.Relative);
            }
            else if (ContentArea.Content is RegistroUserControl)
            {
                iconUri = new Uri("/assets/Icons/Selected/historicos.png", UriKind.Relative);
            }
            else if (ContentArea.Content is ControleEstoqueUserControl)
            {
                iconUri = new Uri("/assets/Icons/Selected/CaixaS.png", UriKind.Relative);
            }
            else if (ContentArea.Content is CadastroUserControl)
            {
                iconUri = new Uri("/assets/Icons/Selected/CadastroS.png", UriKind.Relative);
            }
            else if (ContentArea.Content is DashboardUserControl)
            {
                iconUri = new Uri("/assets/Icons/Selected/GraficoS.png", UriKind.Relative);
            }
            else if (ContentArea.Content is NotificacoesUserControl)
            {
                iconUri = new Uri("/assets/Icons/Selected/SinoS.png", UriKind.Relative);
            }
            else if (ContentArea.Content is ConfiguracaoUserControl)
            {
                iconUri = new Uri("/assets/Icons/Selected/EngrenagemS.png", UriKind.Relative);
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
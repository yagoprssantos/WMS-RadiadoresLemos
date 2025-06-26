using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WMS_RadiadoresLemos_WPF.src.Views;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public class NavigationState
    {
        public UserControl Control { get; set; }
        public string Title { get; set; }
        public string IconPath { get; set; }
        public string MenuButton { get; set; }
        public Dictionary<string, object> Parameters { get; set; }

        public NavigationState(UserControl control, string title, string iconPath, string menuButton = null, Dictionary<string, object> parameters = null)
        {
            Control = control;
            Title = title;
            IconPath = iconPath;
            MenuButton = menuButton;
            Parameters = parameters ?? new Dictionary<string, object>();
        }
    }

    public class NavigationService
    {
        // Histórico de navegação
        private readonly Stack<NavigationState> _backStack = new Stack<NavigationState>();
        private readonly Stack<NavigationState> _forwardStack = new Stack<NavigationState>();
        private NavigationState _currentState;

        // Referências para controles da UI
        private readonly MainWindow _mainWindow;
        private readonly ContentControl _contentArea;
        private readonly TextBlock _titleTextBlock;
        private readonly Image _iconImage;

        // Eventos para notificar mudanças no estado de navegação
        public event EventHandler NavigationChanged;

        public bool CanGoBack => _backStack.Count > 0;
        public bool CanGoForward => _forwardStack.Count > 0;

        public NavigationService(MainWindow mainWindow, ContentControl contentArea, TextBlock titleTextBlock, Image iconImage)
        {
            _mainWindow = mainWindow;
            _contentArea = contentArea;
            _titleTextBlock = titleTextBlock;
            _iconImage = iconImage;
        }

        // Navegar para um novo controle
        public void Navigate(UserControl control, string title, string iconPath, string menuButton = null, Dictionary<string, object> parameters = null)
        {
            // Se o controle atual não for nulo, salve-o na pilha de retorno
            if (_contentArea.Content != null && _currentState != null)
            {
                _backStack.Push(_currentState);
                _forwardStack.Clear(); // Limpa a pilha de avanço quando uma nova navegação ocorre
            }

            // Cria o novo estado de navegação
            _currentState = new NavigationState(control, title, iconPath, menuButton, parameters);

            // Atualiza a UI
            UpdateContent();

            // Notifica sobre a mudança na navegação
            NavigationChanged?.Invoke(this, EventArgs.Empty);
        }

        // Voltar na navegação
        public void GoBack()
        {
            if (!CanGoBack)
                return;

            // Move o estado atual para a pilha de avanço
            if (_currentState != null)
            {
                _forwardStack.Push(_currentState);
            }

            // Recupera o estado anterior
            _currentState = _backStack.Pop();

            // Atualiza a UI
            UpdateContent();

            // Notifica sobre a mudança na navegação
            NavigationChanged?.Invoke(this, EventArgs.Empty);
        }

        // Avançar na navegação
        public void GoForward()
        {
            if (!CanGoForward)
                return;

            // Move o estado atual para a pilha de retorno
            if (_currentState != null)
            {
                _backStack.Push(_currentState);
            }

            // Recupera o próximo estado
            _currentState = _forwardStack.Pop();

            // Atualiza a UI
            UpdateContent();

            // Notifica sobre a mudança na navegação
            NavigationChanged?.Invoke(this, EventArgs.Empty);
        }

        // Recarregar o conteúdo atual
        public void Refresh()
        {
            if (_currentState == null)
                return;

            // Recria o controle com base no tipo atual
            Type controlType = _currentState.Control.GetType();
            UserControl newControl = (UserControl)Activator.CreateInstance(controlType);

            // Aplica os parâmetros salvos no estado atual
            if (_currentState.Parameters != null && _currentState.Parameters.Count > 0)
            {
                ApplyParameters(newControl, _currentState.Parameters);
            }

            // Atualiza o estado atual com o novo controle
            _currentState.Control = newControl;

            // Atualiza a UI
            UpdateContent();
        }

        // Aplicar parâmetros específicos ao controle
        private void ApplyParameters(UserControl control, Dictionary<string, object> parameters)
        {
            // Lógica para aplicar parâmetros específicos de cada tipo de controle
            if (control is CadastroUserControl cadastroControl && parameters.ContainsKey("tipoTabela"))
            {
                var tipoTabela = parameters["tipoTabela"] as string;
                // Executa método específico para selecionar a tabela
                // Esta implementação depende da API disponível no CadastroUserControl
            }

            // Adicione outros tipos de controle conforme necessário
        }

        // Atualizar o conteúdo da tela
        private void UpdateContent()
        {
            if (_currentState == null)
                return;

            // Atualiza o conteúdo principal
            _contentArea.Content = _currentState.Control;

            // Atualiza o título
            if (_titleTextBlock != null)
            {
                _titleTextBlock.Text = _currentState.Title;
            }

            // Atualiza o ícone
            if (_iconImage != null && !string.IsNullOrEmpty(_currentState.IconPath))
            {
                UpdateIcon(_currentState.IconPath);
            }

            // Atualiza a seleção no menu
            if (_mainWindow != null && !string.IsNullOrEmpty(_currentState.MenuButton))
            {
                _mainWindow.UpdateMenuSelection(_currentState.MenuButton);
            }
        }

        // Atualizar o ícone
        private void UpdateIcon(string iconPath)
        {
            try
            {
                // Obter a cor do tema atual
                Color accentColor = ((SolidColorBrush)Application.Current.Resources["AccentBrush"]).Color;

                // Colorizar o ícone com a cor do tema
                _iconImage.Source = ImageUtils.ColorizeImage(iconPath, accentColor);

                // Aplicar configurações de qualidade
                RenderOptions.SetBitmapScalingMode(_iconImage, BitmapScalingMode.HighQuality);
            }
            catch (Exception ex)
            {
                // Log do erro, mas continua sem o ícone
                Console.WriteLine($"Erro ao atualizar ícone: {ex.Message}");
                _iconImage.Source = null;
            }
        }

        // Obter o botão de menu associado ao tipo de controle
        public string GetMenuButtonForControlType(Type controlType)
        {
            if (controlType == typeof(ComprasUserControl)) return "BtnCompras";
            if (controlType == typeof(VendasUserControl)) return "BtnVendas";
            if (controlType == typeof(RegistroUserControl)) return "BtnRegistro";
            if (controlType == typeof(ControleEstoqueUserControl)) return "BtnEstoque";
            if (controlType == typeof(CadastroUserControl)) return "BtnCadastro";
            if (controlType == typeof(DashboardUserControl)) return "BtnDashboard";
            if (controlType == typeof(NotificacoesUserControl)) return "BtnNotificacoes";
            if (controlType == typeof(ConfiguracaoUserControl)) return "BtnConfiguracoes";
            if (controlType == typeof(EscolherCadastroUserControl)) return "BtnCadastro";
            if (controlType == typeof(BoletoTestUserControl)) return "BtnBoletos";

            return null;
        }
    }

    public static class NavigationExtensions
    {
        // Método para navegar para outro controle a partir de um UserControl
        public static void NavigateTo(this UserControl source, UserControl targetControl, string title, string iconPath, Dictionary<string, object> parameters = null)
        {
            if (MainWindow._instance == null) return;

            // Acessa o serviço de navegação da MainWindow
            var navigationField = typeof(MainWindow).GetField("_navigationService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (navigationField == null) return;

            var navigationService = navigationField.GetValue(MainWindow._instance) as NavigationService;
            if (navigationService == null) return;

            // Determina o botão do menu associado (se existir)
            string menuButton = navigationService.GetMenuButtonForControlType(targetControl.GetType());

            // Navega para o novo controle
            navigationService.Navigate(targetControl, title, iconPath, menuButton, parameters);
        }
    }
}
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
        // Tipo do UserControl
        public Type ControlType { get; set; }
        
        // Instância do controle (para preservar estado)
        public UserControl Control { get; set; }
        
        // Parâmetros para inicialização
        public Dictionary<string, object> Parameters { get; set; }
        
        // Botão do menu associado (para seleção visual)
        public string AssociatedMenuButton { get; set; }
        
        // Título da página
        public string Title { get; set; }
        
        // Caminho do ícone
        public string IconPath { get; set; }

        public NavigationState(UserControl control, string title, string iconPath, string menuButton = null, Dictionary<string, object> parameters = null)
        {
            Control = control;
            ControlType = control.GetType();
            Title = title;
            IconPath = iconPath;
            AssociatedMenuButton = menuButton;
            Parameters = parameters ?? new Dictionary<string, object>();
        }
    }

    public class NavigationService
    {
        // Histórico de navegação
        private Stack<NavigationState> _backStack = new Stack<NavigationState>();
        private Stack<NavigationState> _forwardStack = new Stack<NavigationState>();
        private NavigationState _currentState;

        // Referências para controles da UI
        private MainWindow _mainWindow;
        private ContentControl _contentArea;
        private TextBlock _titleTextBlock;
        private Image _iconImage;

        public NavigationService(MainWindow mainWindow, ContentControl contentArea, TextBlock titleTextBlock, Image iconImage)
        {
            _mainWindow = mainWindow;
            _contentArea = contentArea;
            _titleTextBlock = titleTextBlock;
            _iconImage = iconImage;
        }

        public bool CanGoBack => _backStack.Count > 0;
        public bool CanGoForward => _forwardStack.Count > 0;

        // Navegar para um novo controle
        public void Navigate(UserControl control, string title, string iconPath, string menuButton = null, Dictionary<string, object> parameters = null)
        {
            if (_currentState != null)
            {
                _backStack.Push(_currentState);
            }

            _forwardStack.Clear();
            
            _currentState = new NavigationState(control, title, iconPath, menuButton, parameters);
            UpdateContent();
        }

        // Voltar na navegação
        public void GoBack()
        {
            if (!CanGoBack) return;

            if (_currentState != null)
            {
                _forwardStack.Push(_currentState);
            }

            _currentState = _backStack.Pop();
            UpdateContent();
        }

        // Avançar na navegação
        public void GoForward()
        {
            if (!CanGoForward) return;

            if (_currentState != null)
            {
                _backStack.Push(_currentState);
            }

            _currentState = _forwardStack.Pop();
            UpdateContent();
        }

        // Recarregar o conteúdo atual
        public void Refresh()
        {
            if (_currentState == null) return;

            // Cria uma nova instância do tipo atual
            UserControl newInstance = (UserControl)Activator.CreateInstance(_currentState.ControlType);
            
            // Aplica os parâmetros específicos conforme o tipo do controle
            ApplyParameters(newInstance, _currentState.Parameters);
            
            // Atualiza o controle atual
            _currentState.Control = newInstance;
            UpdateContent();
        }

        // Aplicar parâmetros específicos ao controle
        private void ApplyParameters(UserControl control, Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0) return;

            // Aplica parâmetros específicos por tipo de controle
            if (control is ControleEstoqueUserControl estoqueControl)
            {
                estoqueControl.AtualizarTabelaEstoque();
            }
            else if (control is RegistroUserControl registroControl)
            {
                if (parameters.TryGetValue("tab", out object tab))
                {
                    switch (tab.ToString())
                    {
                        case "entradas":
                            registroControl.CarregarEntradas();
                            break;
                        case "saidas":
                            registroControl.CarregarSaidas();
                            break;
                        case "historico":
                            registroControl.CarregarHistorico();
                            break;
                    }
                }
                else
                {
                    registroControl.CarregarHistorico();
                }
            }
            // Adicionar outros casos específicos conforme necessário
        }

        // Atualizar o conteúdo da tela
        private void UpdateContent()
        {
            if (_currentState == null) return;

            // Atualiza o conteúdo
            _contentArea.Content = _currentState.Control;
            
            // Atualiza o título
            _titleTextBlock.Text = _currentState.Title;
            
            // Atualiza o ícone
            UpdateIcon(_currentState.IconPath);
            
            // Atualiza a seleção do menu
            if (!string.IsNullOrEmpty(_currentState.AssociatedMenuButton))
            {
                _mainWindow.UpdateMenuSelection(_currentState.AssociatedMenuButton);
            }
        }

        // Atualizar o ícone
        private void UpdateIcon(string iconPath)
        {
            if (!string.IsNullOrEmpty(iconPath))
            {
                try
                {
                    // Obter a cor do tema atual
                    Color accentColor = ((SolidColorBrush)_mainWindow.FindResource("AccentBrush")).Color;
                    
                    // Colorizar o ícone com a cor do tema
                    _iconImage.Source = ImageUtils.ColorizeImage(iconPath, accentColor);
                    
                    // Aplicar configurações de qualidade
                    RenderOptions.SetBitmapScalingMode(_iconImage, BitmapScalingMode.HighQuality);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao atualizar ícone no NavigationService: {ex.Message}");
                }
            }
            else
            {
                _iconImage.Source = null;
            }
        }

        // Obter o menu associado ao tipo de controle
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
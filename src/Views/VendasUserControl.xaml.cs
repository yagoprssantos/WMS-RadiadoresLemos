using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    /// <summary>
    /// Interação lógica para VendasUserControl.xaml
    /// </summary>
    public partial class VendasUserControl : UserControl
    {
        private List<Venda> _listaVendas;
        private string _ordenacaoAtual = "recente"; // Padrão: mais recente primeiro
        private string _filtroTexto = "Ordenar por";

        public VendasUserControl()
        {
            InitializeComponent();

            // Registrar no evento de adição de venda
            CadastroVendasWindow.VendaAdicionada += CadastroVendasWindow_VendaAdicionada;

            // Carregar vendas ao inicializar
            Loaded += VendasUserControl_Loaded;
        }

        private void VendasUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CarregarVendas();
        }

        private void CadastroVendasWindow_VendaAdicionada(object sender, Venda e)
        {
            // Recarregar vendas quando uma nova for adicionada
            CarregarVendas();
        }

        private void CarregarVendas()
        {
            try
            {
                // Obter vendas do serviço
                _listaVendas = VendaService.ObterVendas();

                // Aplicar ordenação atual
                AplicarOrdenacao();

                // Limpar container e adicionar vendas
                AtualizarInterfaceVendas();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar vendas: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOrdenar_Click(object sender, RoutedEventArgs e)
        {
            // Abrir/fechar o popup de ordenação
            popupOrdenar.IsOpen = !popupOrdenar.IsOpen;
        }

        private void OrdenacaoItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tipoOrdenacao)
            {
                _ordenacaoAtual = tipoOrdenacao;

                // Atualiza o texto do botão para refletir a seleção atual
                _filtroTexto = $"Ordenar por {button.Content}";

                // Acessar o TextBlock dentro do template do botão
                if (btnOrdenar.Template.FindName("OrderButtonText", btnOrdenar) is TextBlock textBlock)
                {
                    textBlock.Text = _filtroTexto;
                }

                // Fecha o popup
                popupOrdenar.IsOpen = false;

                // Aplica a ordenação e atualiza a interface
                if (_listaVendas != null && _listaVendas.Count > 0)
                {
                    AplicarOrdenacao();
                    AtualizarInterfaceVendas();
                }
            }
        }

        private void AplicarOrdenacao()
        {
            if (_listaVendas == null || _listaVendas.Count == 0)
                return;

            switch (_ordenacaoAtual)
            {
                case "preco":
                    _listaVendas = _listaVendas.OrderByDescending(v => v.ValorTotal).ToList();
                    break;
                case "produto":
                    _listaVendas = _listaVendas.OrderBy(v => v.Produto).ToList();
                    break;
                case "cliente":
                    _listaVendas = _listaVendas.OrderBy(v => v.Cliente).ToList();
                    break;
                case "recente":
                    _listaVendas = _listaVendas.OrderByDescending(v => v.DataCompra).ToList();
                    break;
                case "antigo":
                    _listaVendas = _listaVendas.OrderBy(v => v.DataCompra).ToList();
                    break;
                default:
                    // Ordem padrão (mais recentes primeiro)
                    _listaVendas = _listaVendas.OrderByDescending(v => v.DataCadastro).ToList();
                    break;
            }
        }

        private void AtualizarInterfaceVendas()
        {
            vendasContainer.Children.Clear();

            if (_listaVendas == null || _listaVendas.Count == 0)
            {
                TextBlock mensagem = new TextBlock
                {
                    Text = "Nenhuma venda cadastrada.",
                    FontSize = 18,
                    Foreground = (SolidColorBrush)FindResource("TextBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10)
                };
                vendasContainer.Children.Add(mensagem);
                return;
            }

            foreach (var venda in _listaVendas)
            {
                Border border = new Border
                {
                    Background = (SolidColorBrush)FindResource("PanelBackgroundBrush"),
                    CornerRadius = new CornerRadius(15),
                    Padding = new Thickness(15),
                    Margin = new Thickness(5),
                    Width = 240,
                };

                StackPanel stackPanel = new StackPanel();

                TextBlock titleTextBlock = new TextBlock
                {
                    Text = "Nota Fiscal",
                    Style = (Style)FindResource("VendasTitleTextBox"),
                    FontSize = 16
                };
                stackPanel.Children.Add(titleTextBlock);

                Separator separator = new Separator
                {
                    Margin = new Thickness(10, 2, 10, 5)
                };
                stackPanel.Children.Add(separator);

                TextBlock clienteTextBlock = new TextBlock
                {
                    Text = $"Cliente: {venda.Cliente}",
                    Style = (Style)FindResource("VendasTextBox"),
                    FontSize = 14
                };
                stackPanel.Children.Add(clienteTextBlock);

                TextBlock pedidoTextBlock = new TextBlock
                {
                    Text = $"Pedido: {venda.Pedido}",
                    Style = (Style)FindResource("VendasTextBox"),
                    FontSize = 14
                };
                stackPanel.Children.Add(pedidoTextBlock);

                TextBlock dataCompraTextBlock = new TextBlock
                {
                    Text = $"Data da Compra: {venda.DataCompra:dd/MM/yyyy}",
                    Style = (Style)FindResource("VendasTextBox"),
                    FontSize = 14
                };
                stackPanel.Children.Add(dataCompraTextBlock);

                TextBlock valorTextBlock = new TextBlock
                {
                    Text = $"Valor Total: R$ {venda.ValorTotal:N2}",
                    Style = (Style)FindResource("VendasTextBox"),
                    FontSize = 14
                };
                stackPanel.Children.Add(valorTextBlock);

                Button detalhesButton = new Button
                {
                    Content = "Detalhes",
                    Style = (Style)FindResource("EmphasisButtonStyle"),
                    FontSize = 16,
                    Width = 120,
                    Margin = new Thickness(4),
                    DataContext = venda
                };

                detalhesButton.Click += DetalhesButton_Click;
                stackPanel.Children.Add(detalhesButton);

                border.Child = stackPanel;
                vendasContainer.Children.Add(border);
            }
        }

        private void DetalhesButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtenha os dados da venda correspondente
            var venda = (sender as Button)?.DataContext as Venda;

            if (venda != null)
            {
                // Crie uma nova instância do UserControl de detalhes
                var detalhesVendaUserControl = new DetalhesVendaUserControl
                {
                    DataContext = venda
                };

                // Exiba a tela de detalhes usando o ContentControl
                var contentControl = (Parent as ContentControl);
                if (contentControl != null)
                {
                    contentControl.Content = detalhesVendaUserControl;
                }
                else
                {
                    // Fallback caso não consiga encontrar o ContentControl
                    string detalhes = $"Detalhes da venda:\n\n" +
                                     $"Cliente: {venda.Cliente}\n" +
                                     $"Pedido: {venda.Pedido}\n" +
                                     $"Produto: {venda.Produto}\n" +
                                     $"Valor Total: R$ {venda.ValorTotal:N2}\n" +
                                     $"Data da Compra: {venda.DataCompra:dd/MM/yyyy}\n" +
                                     $"Data do Pagamento: {venda.DataPagamento:dd/MM/yyyy}\n" +
                                     $"Data de Cadastro: {venda.DataCadastro:dd/MM/yyyy HH:mm}";

                    MessageBox.Show(detalhes, "Detalhes da Venda", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void NovaTransacaoButton_Click(object sender, RoutedEventArgs e)
        {
            // Abrir a janela de cadastro de vendas
            CadastroVendasWindow cadastroVendasWindow = new CadastroVendasWindow();
            cadastroVendasWindow.ShowDialog(); // Usar ShowDialog para modal
        }
    }
}
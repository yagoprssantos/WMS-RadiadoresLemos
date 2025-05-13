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
    public partial class VendasUserControl : UserControl
    {
        private List<Venda> _todasVendas;      // Lista completa de vendas
        private List<Venda> _vendasFiltradas;  // Lista filtrada e ordenada
        private string _ordenacaoAtual = "recente";
        private string _filtroTexto = "Ordenar por";

        public VendasUserControl()
        {
            InitializeComponent();

            CadastroVendasWindow.VendaAdicionada += CadastroVendasWindow_VendaAdicionada;
            Loaded += VendasUserControl_Loaded;
        }

        // 1. Carregamento inicial
        private void VendasUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CarregarVendas();
        }

        private void CadastroVendasWindow_VendaAdicionada(object sender, Venda e)
        {
            CarregarVendas();
        }

        private void CarregarVendas()
        {
            try
            {
                _todasVendas = VendaService.ObterVendas();
                _vendasFiltradas = new List<Venda>(_todasVendas);
                AplicarOrdenacao();
                AtualizarInterfaceVendas();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar vendas: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 2. Pesquisa
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_todasVendas == null) return;

            string textoBusca = SearchBox.Text?.Trim().ToLower() ?? "";
            _vendasFiltradas = _todasVendas
                .Where(v =>
                    (v.Cliente?.ToLower().Contains(textoBusca) ?? false) ||
                    (v.Pedido?.ToLower().Contains(textoBusca) ?? false) ||
                    (v.Produto?.ToLower().Contains(textoBusca) ?? false) ||
                    (v.NotaFiscal?.ToLower().Contains(textoBusca) ?? false)
                )
                .ToList();

            AplicarOrdenacao();
            AtualizarInterfaceVendas();
        }

        // 3. Filtro
        private void FiltrarButton_Click(object sender, RoutedEventArgs e)
        {
            FiltroPopup.IsOpen = true;
        }

        private void AplicarFiltroButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Adicionar lógica de filtro
            FiltroPopup.IsOpen = false;
            // string clienteSelecionado = ClienteComboBox.SelectedItem?.ToString();
            // _vendasFiltradas = _todasVendas.Where(v => v.Cliente == clienteSelecionado).ToList();
            AplicarOrdenacao();
            AtualizarInterfaceVendas();
        }

        private void LimparFiltroButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Limpar filtros

            FiltroPopup.IsOpen = false;
            _vendasFiltradas = new List<Venda>(_todasVendas);
            AplicarOrdenacao();
            AtualizarInterfaceVendas();
        }

        // 4. Ordenação
        private void OrdenarButton_Click(object sender, RoutedEventArgs e)
        {
            OrdenarPopup.IsOpen = !OrdenarPopup.IsOpen;
        }

        private void OrdenacaoItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tipoOrdenacao)
            {
                _ordenacaoAtual = tipoOrdenacao;
                _filtroTexto = $"Ordenar por {button.Content}";

                // Atualiza o texto do botão de ordenação, se houver TextBlock no template
                if (OrderButton.Template.FindName("OrderButtonText", OrderButton) is TextBlock textBlock)
                {
                    textBlock.Text = _filtroTexto;
                }
                else
                {
                    OrderButton.Content = _filtroTexto;
                }

                OrdenarPopup.IsOpen = false;
                AplicarOrdenacao();
                AtualizarInterfaceVendas();
            }
        }

        private void AplicarOrdenacao()
        {
            if (_vendasFiltradas == null || _vendasFiltradas.Count == 0)
                return;

            switch (_ordenacaoAtual)
            {
                case "preco":
                    _vendasFiltradas = _vendasFiltradas.OrderByDescending(v => v.ValorTotal).ToList();
                    break;
                case "produto":
                    _vendasFiltradas = _vendasFiltradas.OrderBy(v => v.Produto).ToList();
                    break;
                case "cliente":
                    _vendasFiltradas = _vendasFiltradas.OrderBy(v => v.Cliente).ToList();
                    break;
                case "recente":
                    _vendasFiltradas = _vendasFiltradas.OrderByDescending(v => v.DataCompra).ToList();
                    break;
                case "antigo":
                    _vendasFiltradas = _vendasFiltradas.OrderBy(v => v.DataCompra).ToList();
                    break;
                default:
                    _vendasFiltradas = _vendasFiltradas.OrderByDescending(v => v.DataCadastro).ToList();
                    break;
            }
        }

        // 5. Atualização da interface
        private void AtualizarInterfaceVendas()
        {
            vendasContainer.Children.Clear();

            if (_vendasFiltradas == null || _vendasFiltradas.Count == 0)
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

            foreach (var venda in _vendasFiltradas)
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

        // 6. Botões de ação
        private void DetalhesButton_Click(object sender, RoutedEventArgs e)
        {
            var venda = (sender as Button)?.DataContext as Venda;

            if (venda != null)
            {
                var detalhesVendaUserControl = new DetalhesVendaUserControl
                {
                    DataContext = venda
                };

                var contentControl = (Parent as ContentControl);
                if (contentControl != null)
                {
                    contentControl.Content = detalhesVendaUserControl;
                }
                else
                {
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
            CadastroVendasWindow cadastroVendasWindow = new CadastroVendasWindow();
            cadastroVendasWindow.ShowDialog();
        }
    }
}
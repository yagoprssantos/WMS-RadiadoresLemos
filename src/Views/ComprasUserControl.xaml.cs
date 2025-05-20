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
        public partial class ComprasUserControl : UserControl
        {
            private List<CompraData> _todasCompras;      // Lista completa de compras
            private List<CompraData> _comprasFiltradas;  // Lista filtrada e ordenada
            private string _ordenacaoAtual = "recente";
            private string _filtroTexto = "Ordenar por";

            public ComprasUserControl()
            {
                InitializeComponent();

                Loaded += ComprasUserControl_Loaded;
            }

            // 1. Carregamento inicial
            private void ComprasUserControl_Loaded(object sender, RoutedEventArgs e)
            {
                CarregarCompras();
            }

            private void CarregarCompras()
            {
                try
                {
                _todasCompras = CompraService.ObterCompras();
                _comprasFiltradas = new List<CompraData>(_todasCompras);
                AplicarOrdenacao();
                    AtualizarInterfaceCompras();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao carregar compras: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // 2. Pesquisa
            private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
            {
                if (_todasCompras == null) return;

                string textoBusca = SearchBox.Text?.Trim().ToLower() ?? "";
                _comprasFiltradas = _todasCompras
                    .Where(v =>
                        (v.Fornecedor?.ToLower().Contains(textoBusca) ?? false) ||
                        (v.Produto?.ToLower().Contains(textoBusca) ?? false) ||
                        (v.NotaFiscal?.ToLower().Contains(textoBusca) ?? false)
                    )
                    .ToList();

                AplicarOrdenacao();
                AtualizarInterfaceCompras();
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
                // string fornecedorSelecionado = FornecedorComboBox.SelectedItem?.ToString();
                // _comprasFiltradas = _todasCompras.Where(v => v.Fornecedor == fornecedorSelecionado).ToList();
                AplicarOrdenacao();
                AtualizarInterfaceCompras();
            }

            private void LimparFiltroButton_Click(object sender, RoutedEventArgs e)
            {
                // TODO: Limpar filtros

                // TODO: Limpar filtro Ordenar

                FiltroPopup.IsOpen = false;
                _comprasFiltradas = new List<CompraData>(_todasCompras);
                AplicarOrdenacao();
                AtualizarInterfaceCompras();
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
                    AtualizarInterfaceCompras();
                }
            }

            private void AplicarOrdenacao()
            {
                if (_comprasFiltradas == null || _comprasFiltradas.Count == 0)
                    return;

                switch (_ordenacaoAtual)
                {
                    case "preco":
                        _comprasFiltradas = _comprasFiltradas.OrderByDescending(v => v.ValorTotal).ToList();
                        break;
                    case "produto":
                        _comprasFiltradas = _comprasFiltradas.OrderBy(v => v.Produto).ToList();
                        break;
                    case "fornecedor":
                        _comprasFiltradas = _comprasFiltradas.OrderBy(v => v.Fornecedor).ToList();
                        break;
                    case "recente":
                        _comprasFiltradas = _comprasFiltradas.OrderByDescending(v => v.DataCompra).ToList();
                        break;
                    case "antigo":
                        _comprasFiltradas = _comprasFiltradas.OrderBy(v => v.DataCompra).ToList();
                        break;
                    default:
                        _comprasFiltradas = _comprasFiltradas.OrderByDescending(v => v.DataCadastro).ToList();
                        break;
                }
            }

            // 5. Atualização da interface
            private void AtualizarInterfaceCompras()
            {
                ComprasContainer.Children.Clear();

                if (_comprasFiltradas == null || _comprasFiltradas.Count == 0)
                {
                    TextBlock mensagem = new TextBlock
                    {
                        Text = "Nenhuma compra cadastrada.",
                        FontSize = 18,
                        Foreground = (SolidColorBrush)FindResource("TextBrush"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(10)
                    };
                    ComprasContainer.Children.Add(mensagem);
                    return;
                }

                foreach (var compra in _comprasFiltradas)
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
                        Style = (Style)FindResource("ComprasTitleTextBox"),
                        FontSize = 16
                    };
                    stackPanel.Children.Add(titleTextBlock);

                    Separator separator = new Separator
                    {
                        Margin = new Thickness(10, 2, 10, 5)
                    };
                    stackPanel.Children.Add(separator);

                    TextBlock fornecedorTextBlock = new TextBlock
                    {
                        Text = $"Fornecedor: {compra.Fornecedor}",
                        Style = (Style)FindResource("ComprasTextBox"),
                        FontSize = 14
                    };
                    stackPanel.Children.Add(fornecedorTextBlock);

                    TextBlock dataCompraTextBlock = new TextBlock
                    {
                        Text = $"Data da Compra: {compra.DataCompra:dd/MM/yyyy}",
                        Style = (Style)FindResource("ComprasTextBox"),
                        FontSize = 14
                    };
                    stackPanel.Children.Add(dataCompraTextBlock);

                    TextBlock valorTextBlock = new TextBlock
                    {
                        Text = $"Valor Total: R$ {compra.ValorTotal:N2}",
                        Style = (Style)FindResource("ComprasTextBox"),
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
                        DataContext = compra
                    };

                    stackPanel.Children.Add(detalhesButton);

                    border.Child = stackPanel;
                    ComprasContainer.Children.Add(border);
                }
            }

            // 6. Registrar Compra
            private void RegistrarCompraButton_Click(object sender, RoutedEventArgs e)
            {
                var compras = new AddEntradaSaídaWindow(isEntrada: true);
                compras.ShowDialog();
            }

            // 7. Botões de ação
            //private void DetalhesButton_Click(object sender, RoutedEventArgs e)
            //{
            //    var compra = (sender as Button)?.DataContext as CompraData;

            //    if (compra != null)
            //    {
            //        var detalhesCompraUserControl = new DetalhesCompraUserControl
            //        {
            //            DataContext = compra
            //        };

            //        var contentControl = (Parent as ContentControl);
            //        if (contentControl != null)
            //        {
            //            contentControl.Content = detalhesCompraUserControl;
            //        }
            //        else
            //        {
            //            string detalhes = $"Detalhes da compra:\n\n" +
            //                             $"Fornecedor: {compra.Fornecedor}\n" +
            //                             $"Produto: {compra.Produto}\n" +
            //                             $"Valor Total: R$ {compra.ValorTotal:N2}\n" +
            //                             $"Data da Compra: {compra.DataCompra:dd/MM/yyyy}\n" +
            //                             $"Data do Pagamento: {compra.DataPagamento:dd/MM/yyyy}\n" +
            //                             $"Data de Cadastro: {compra.DataCadastro:dd/MM/yyyy HH:mm}";

            //            MessageBox.Show(detalhes, "Detalhes da Compra", MessageBoxButton.OK, MessageBoxImage.Information);
            //        }
            //    }
            //}
        }
    }
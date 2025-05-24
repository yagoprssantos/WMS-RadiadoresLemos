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
                // Obter compras diretamente do banco de dados em vez de usar CompraService
                var db = DatabaseConnect.Database;
                if (db != null)
                {
                    var collection = db.GetCollection<CompraData>("compras");
                    _todasCompras = collection.FindAll().ToList();
                    _comprasFiltradas = new List<CompraData>(_todasCompras);
                    AplicarOrdenacao();
                    AtualizarInterfaceCompras();
                }
                else
                {
                    MessageBox.Show("Não foi possível conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                    (v.FornecedorNome?.ToLower().Contains(textoBusca) ?? false) ||
                    (v.Itens.Any(i => i.ProdutoNome.ToLower().Contains(textoBusca))) ||
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
                    _comprasFiltradas = _comprasFiltradas.OrderBy(v => v.Itens.FirstOrDefault()?.ProdutoNome ?? "").ToList();
                    break;
                case "fornecedor":
                    _comprasFiltradas = _comprasFiltradas.OrderBy(v => v.FornecedorNome).ToList();
                    break;
                case "recente":
                    _comprasFiltradas = _comprasFiltradas.OrderByDescending(v => v.DataCompra).ToList();
                    break;
                case "antigo":
                    _comprasFiltradas = _comprasFiltradas.OrderBy(v => v.DataCompra).ToList();
                    break;
                default:
                    _comprasFiltradas = _comprasFiltradas.OrderByDescending(v => v.DataCompra).ToList();
                    break;
            }
        }

        // 5. Atualização da interface
        private void AtualizarInterfaceCompras()
        {
            if (_comprasFiltradas == null || _comprasFiltradas.Count == 0)
            {
                ComprasContainer.ItemsSource = null;
                MensagemVazia.Visibility = Visibility.Visible;
                return;
            }

            MensagemVazia.Visibility = Visibility.Collapsed;
            ComprasContainer.ItemsSource = _comprasFiltradas;
        }

        // 6. Registrar Compra
        private void RegistrarCompraButton_Click(object sender, RoutedEventArgs e)
        {
            var compras = new AddEntradaSaídaWindow(isEntrada: true);
            compras.ShowDialog();

            // Atualiza a lista de compras após o registro
            CarregarCompras();
        }

        // 7. Botões de ação
        private void DetalhesButton_Click(object sender, RoutedEventArgs e)
        {
            var compra = (sender as Button)?.DataContext as CompraData;
            if (compra != null)
            {
                var detalhesCompraUserControl = new DetalhesUserControl(compra);
                var contentControl = (Parent as ContentControl);
                if (contentControl != null)
                {
                    contentControl.Content = detalhesCompraUserControl;
                }
            }
        }
    }
}
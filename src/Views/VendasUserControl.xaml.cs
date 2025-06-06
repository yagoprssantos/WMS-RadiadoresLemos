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
        private List<VendaData> _todasVendas;      // Lista completa de vendas
        private List<VendaData> _vendasFiltradas;  // Lista filtrada e ordenada
        private string _ordenacaoAtual = "recente";
        private string _filtroTexto = "Ordenar por";

        public VendasUserControl()
        {
            InitializeComponent();
            Loaded += VendasUserControl_Loaded;
        }

        // 1. Carregamento inicial
        private void VendasUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CarregarVendas();
        }

        private void CarregarVendas()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db != null)
                {
                    var collection = db.GetCollection<VendaData>("vendas");
                    _todasVendas = collection.FindAll().ToList();
                    _vendasFiltradas = new List<VendaData>(_todasVendas);
                    AplicarOrdenacao();
                    AtualizarInterfaceVendas();
                }
                else
                {
                    MessageBox.Show("Não foi possível conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                    (v.ClienteCNPJ?.ToLower().Contains(textoBusca) ?? false) ||
                    (v.Pedido?.ToLower().Contains(textoBusca) ?? false) ||
                    (v.Itens.Any(i => i.ProdutoNome.ToLower().Contains(textoBusca))) ||
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

            // TODO: Limpar filtro Ordenar

            FiltroPopup.IsOpen = false;
            _vendasFiltradas = new List<VendaData>(_todasVendas);
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
                    _vendasFiltradas = _vendasFiltradas.OrderBy(v => v.Itens.FirstOrDefault()?.ProdutoNome ?? "").ToList();
                    break;
                case "cliente":
                    _vendasFiltradas = _vendasFiltradas.OrderBy(v => v.ClienteCNPJ).ToList();
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
            if (_vendasFiltradas == null || _vendasFiltradas.Count == 0)
            {
                VendasContainer.ItemsSource = null;
                MensagemVazia.Visibility = Visibility.Visible;
                return;
            }

            MensagemVazia.Visibility = Visibility.Collapsed;
            VendasContainer.ItemsSource = _vendasFiltradas;
        }

        // 6. Registrar VendaData
        private void RegistrarVendaButton_Click(object sender, RoutedEventArgs e)
        {
            var compras = new AddEntradaSaídaWindow(isEntrada: false);
            compras.ShowDialog();

            // Atualiza a lista de vendas após o registro
            CarregarVendas();
        }

        // 7. Botões de ação
        private void DetalhesButton_Click(object sender, RoutedEventArgs e)
        {
            var venda = (sender as Button)?.DataContext as VendaData;
            if (venda != null)
            {
                var detalhesVendaUserControl = new DetalhesUserControl(venda);
                var contentControl = (Parent as ContentControl);
                if (contentControl != null)
                {
                    contentControl.Content = detalhesVendaUserControl;
                }
            }
        }
    }
}
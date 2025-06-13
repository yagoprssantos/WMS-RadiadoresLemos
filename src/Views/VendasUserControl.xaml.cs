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
            CarregarClientes();
            CarregarProdutos();
            CarregarNotasFiscais();
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

        // Método para carregar os dados dos clientes no ComboBox
        private void CarregarClientes()
        {
            try
            {
                if (_todasVendas == null || !_todasVendas.Any()) return;

                // Extrai os clientes das vendas
                var clientesDasVendas = _todasVendas
                    .Select(v => new { Id = v.ClienteId, Nome = v.ClienteCNPJ })
                    .Where(c => !string.IsNullOrEmpty(c.Id) && !string.IsNullOrEmpty(c.Nome))
                    .Distinct()
                    .OrderBy(c => c.Nome)
                    .ToList();

                // Adicionar item vazio no início
                var listaClientes = new List<dynamic>();
                listaClientes.Add(new { Id = "", Nome = "Todos os clientes" });
                listaClientes.AddRange(clientesDasVendas);

                ClienteComboBox.ItemsSource = listaClientes;
                ClienteComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar clientes: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para carregar os produtos que foram vendidos
        private void CarregarProdutos()
        {
            try
            {
                if (_todasVendas == null) return;

                // Extrair IDs de produtos únicos de todas as vendas
                var produtosIds = _todasVendas
                    .SelectMany(v => v.Itens)
                    .Select(i => new { Id = i.ProdutoId, Nome = i.ProdutoNome })
                    .GroupBy(p => p.Id)  // Agrupar para eliminar duplicados
                    .Select(g => g.First())  // Pegar o primeiro item de cada grupo
                    .OrderBy(p => p.Nome)
                    .ToList();

                // Adicionar item vazio no início
                var listaProdutos = new List<dynamic>();
                listaProdutos.Add(new { Id = "", Nome = "Todos os produtos" });
                listaProdutos.AddRange(produtosIds);

                ProdutosVendidosComboBox.ItemsSource = listaProdutos;
                ProdutosVendidosComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar produtos: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para carregar as notas fiscais das vendas
        private void CarregarNotasFiscais()
        {
            try
            {
                if (_todasVendas == null || !_todasVendas.Any()) return;

                // Extrair notas fiscais das vendas
                var notasFiscais = _todasVendas
                    .Where(v => !string.IsNullOrEmpty(v.NotaFiscal))
                    .Select(v => new { Id = v.Id, NotaFiscal = v.NotaFiscal })
                    .Distinct()
                    .OrderBy(nf => nf.NotaFiscal)
                    .ToList();

                // Adicionar item vazio no início
                var listaNotasFiscais = new List<dynamic>();
                listaNotasFiscais.Add(new { Id = "", NotaFiscal = "Todas as notas fiscais" });
                listaNotasFiscais.AddRange(notasFiscais);

                NotaFiscalComboBox.ItemsSource = listaNotasFiscais;
                NotaFiscalComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar notas fiscais: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (_todasVendas == null) return;

            // Criar uma nova lista baseada em todas as vendas
            _vendasFiltradas = new List<VendaData>(_todasVendas);

            // 1. Filtrar por cliente
            string clienteSelecionadoId = ClienteComboBox.SelectedValue as string;
            if (!string.IsNullOrEmpty(clienteSelecionadoId))
            {
                _vendasFiltradas = _vendasFiltradas.Where(v => v.ClienteId == clienteSelecionadoId).ToList();
            }

            // 2. Filtrar por produto
            string produtoSelecionadoId = ProdutosVendidosComboBox.SelectedValue as string;
            if (!string.IsNullOrEmpty(produtoSelecionadoId))
            {
                _vendasFiltradas = _vendasFiltradas.Where(v => v.Itens.Any(i => i.ProdutoId == produtoSelecionadoId)).ToList();
            }

            // 3. Filtrar por nota fiscal
            string notaFiscalSelecionadaId = NotaFiscalComboBox.SelectedValue as string;
            if (!string.IsNullOrEmpty(notaFiscalSelecionadaId))
            {
                _vendasFiltradas = _vendasFiltradas.Where(v => v.Id == notaFiscalSelecionadaId).ToList();
            }

            // 4. Filtrar por período
            DateTime? dataInicio = DataInicioPicker.SelectedDate;
            DateTime? dataFim = DataFimPicker.SelectedDate;

            if (dataInicio.HasValue)
            {
                _vendasFiltradas = _vendasFiltradas.Where(v => v.DataCompra.Date >= dataInicio.Value.Date).ToList();
            }

            if (dataFim.HasValue)
            {
                _vendasFiltradas = _vendasFiltradas.Where(v => v.DataCompra.Date <= dataFim.Value.Date).ToList();
            }

            // 5. Filtrar por tipo de pagamento
            if (TipoPagamentoComboBox.SelectedIndex > 0)  // Se não for "Todos"
            {
                string tipoPagamento = (TipoPagamentoComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (!string.IsNullOrEmpty(tipoPagamento))
                {
                    _vendasFiltradas = _vendasFiltradas.Where(v => v.TipoPagamento == tipoPagamento).ToList();
                }
            }

            // Aplicar direção de ordenação
            var direcaoOrdenacao = (OrdemComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (direcaoOrdenacao == "desc" && _ordenacaoAtual != "recente" && _ordenacaoAtual != "preco")
            {
                // Inverter a ordenação atual para ordem decrescente
                _vendasFiltradas.Reverse();
            }

            FiltroPopup.IsOpen = false;
            AplicarOrdenacao();
            AtualizarInterfaceVendas();
        }

        private void LimparFiltroButton_Click(object sender, RoutedEventArgs e)
        {
            // Limpar ComboBoxes
            if (ClienteComboBox.Items.Count > 0) ClienteComboBox.SelectedIndex = 0;
            if (ProdutosVendidosComboBox.Items.Count > 0) ProdutosVendidosComboBox.SelectedIndex = 0;
            if (NotaFiscalComboBox.Items.Count > 0) NotaFiscalComboBox.SelectedIndex = 0;

            // Limpar datas
            DataInicioPicker.SelectedDate = null;
            DataFimPicker.SelectedDate = null;
            
            // Limpar tipo de pagamento
            TipoPagamentoComboBox.SelectedIndex = 0;
            
            // Ordenação
            _ordenacaoAtual = "recente";
            _filtroTexto = "Ordenar por";
            
            // Corrigir a referência para OrdemComboBox
            if (OrdemComboBox.Items.Count > 0) OrdemComboBox.SelectedIndex = 0;

            // Fechar popup e restaurar lista completa
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
            // Recarregar os ComboBoxes com os novos dados
            CarregarClientes();
            CarregarProdutos();
        }

        // 7. Botões de ação
        private void DetalhesButton_Click(object sender, RoutedEventArgs e)
        {
            var venda = (sender as Button)?.DataContext as VendaData;
            if (venda != null)
            {
                var detalhesVendaUserControl = new DetalhesUserControl(venda);
                this.NavigateTo(
                    detalhesVendaUserControl,
                    "Detalhes da Venda",
                    "/assets/Icons/Selected/PranchetaS.png"
                );
            }
        }
    }
}
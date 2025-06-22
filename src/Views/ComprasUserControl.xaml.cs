using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // Adicionado para VisualTreeHelper
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class ComprasUserControl : UserControl
    {

        private List<CompraData> _todasCompras;      // Lista completa de compras
        private List<CompraData> _comprasFiltradas;  // Lista filtrada e ordenada
        private List<BoletoData> _todosBoletos;      // Lista completa de boletos

        private string _ordenacaoAtual = "recente";
        private string _filtroTexto = "Ordenar por"; // Mantido para o botão de ordenação
        private DateTime _currentCalendarMonth = DateTime.Today;
        private CalendarDayViewModel _selectedDay;

        public ComprasUserControl()
        {
            InitializeComponent();
            Loaded += ComprasUserControl_Loaded;
        }

        private void ComprasUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CarregarCompras();
            CarregarBoletos();
            CarregarCalendario();
            CarregarFornecedores();
            CarregarProdutos();
        }

        private void CarregarCompras()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db != null)
                {
                    var collection = db.GetCollection<CompraData>("compras");
                    _todasCompras = collection.FindAll().ToList();
                    _comprasFiltradas = new List<CompraData>(_todasCompras);


                    // Calcular os próximos vencimentos depois de carregar boletos
                    if (_todosBoletos != null)
                    {
                        CalcularProximosVencimentos();
                    }

                    AplicarOrdenacao();
                    AtualizarInterfaceCompras();
                }
                else
                {
                    MessageBox.Show("Não foi possível conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    _todasCompras = new List<CompraData>();
                    _comprasFiltradas = new List<CompraData>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar compras: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                _todasCompras = new List<CompraData>();
                _comprasFiltradas = new List<CompraData>();
            }
        }


        private void CarregarBoletos()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db != null)
                {
                    var collection = db.GetCollection<BoletoData>("boletos");
                    _todosBoletos = collection.FindAll().ToList();

                    // Se as compras já foram carregadas, calcular os próximos vencimentos
                    if (_todasCompras != null)
                    {
                        CalcularProximosVencimentos();
                        AtualizarInterfaceCompras(); // Atualizar a interface para refletir os novos vencimentos
                    }
                }
                else
                {
                    MessageBox.Show("Não foi possível conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar boletos: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para carregar os dados dos fornecedores no ComboBox
        private void CarregarFornecedores()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db != null)
                {
                    // Usar a classe FornecedorData em vez de dynamic para garantir o tipo correto
                    var collection = db.GetCollection<FornecedorData>("fornecedores");
                    var fornecedores = collection.FindAll()
                        .Select(f => new { Id = f.Id, Nome = f.Nome })
                        .OrderBy(f => f.Nome)
                        .ToList();

                    // Adicionar item vazio no início
                    var listaFornecedores = new List<dynamic>();
                    listaFornecedores.AddRange(fornecedores);

                    // Verificar se há fornecedores nas compras que não constam na lista de fornecedores
                    if (_todasCompras != null && _todasCompras.Any())
                    {
                        var fornecedoresCompras = _todasCompras
                            .Where(c => !string.IsNullOrEmpty(c.FornecedorId))
                            .Select(c => new { Id = c.FornecedorId, Nome = c.FornecedorNome })
                            .GroupBy(f => f.Id)
                            .Select(g => g.First())
                            .ToList();

                        // Adicionar apenas fornecedores que não estão na lista original
                        foreach (var fornecedor in fornecedoresCompras)
                        {
                            if (!fornecedores.Any(f => f.Id == fornecedor.Id))
                            {
                                listaFornecedores.Add(fornecedor);
                            }
                        }
                    }

                    FornecedorComboBox.ItemsSource = listaFornecedores;
                    FornecedorComboBox.DisplayMemberPath = "Nome";
                    FornecedorComboBox.SelectedValuePath = "Id";
                    FornecedorComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar fornecedores: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para carregar os produtos que foram comprados
        private void CarregarProdutos()
        {
            try
            {
                if (_todasCompras == null) return;

                // Extrair IDs de produtos únicos de todas as compras
                var produtosIds = _todasCompras
                    .SelectMany(c => c.Itens)
                    .Select(i => new { Id = i.ProdutoId, Nome = i.ProdutoNome })
                    .GroupBy(p => p.Id)  // Agrupar para eliminar duplicados
                    .Select(g => g.First())  // Pegar o primeiro item de cada grupo
                    .OrderBy(p => p.Nome)
                    .ToList();

                // Adicionar item vazio no início
                var listaProdutos = new List<dynamic>();
                listaProdutos.Add(new { Id = "", Nome = "Todos os produtos" });
                listaProdutos.AddRange(produtosIds);

                ProdutosCompradosComboBox.ItemsSource = listaProdutos;
                ProdutosCompradosComboBox.DisplayMemberPath = "Nome";
                ProdutosCompradosComboBox.SelectedValuePath = "Id";
                ProdutosCompradosComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar produtos: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalcularProximosVencimentos()
        {
            if (_todasCompras == null || _todosBoletos == null) return;

            foreach (var compra in _todasCompras)
            {
                // Verificar se a compra tem boletos associados
                if (compra.Boletos == null || !compra.Boletos.Any())
                {
                    compra.ProximoVencimento = null;
                    continue;
                }

                // Filtrar boletos associados a esta compra que ainda não foram pagos
                var boletosCompra = _todosBoletos
                    .Where(b => compra.Boletos.Contains(b.Id) && b.DataPagamento == null)
                    .ToList();

                if (!boletosCompra.Any())
                {
                    compra.ProximoVencimento = null;
                    continue;
                }

                // Encontrar o boleto com a data de vencimento mais próxima da data atual
                DateTime hoje = DateTime.Today;
                var boletosNaoVencidos = boletosCompra.Where(b => b.DataVencimento >= hoje).ToList();

                if (boletosNaoVencidos.Any())
                {
                    // Se há boletos não vencidos, pega o de vencimento mais próximo
                    compra.ProximoVencimento = boletosNaoVencidos.OrderBy(b => b.DataVencimento).First().DataVencimento;
                }
                else
                {
                    // Se todos os boletos já venceram, pega o de vencimento mais recente
                    compra.ProximoVencimento = boletosCompra.OrderByDescending(b => b.DataVencimento).First().DataVencimento;
                }
            }
        }

        private void CarregarCalendario()
        {
            CalendarMonthText.Text = _currentCalendarMonth.ToString("MMMM yyyy");

            var days = new List<CalendarDayViewModel>();
            DateTime firstDayOfMonth = new DateTime(_currentCalendarMonth.Year, _currentCalendarMonth.Month, 1);
            int offset = ((int)firstDayOfMonth.DayOfWeek);

            // Dias do mês anterior
            DateTime previousMonth = firstDayOfMonth.AddDays(-offset);
            for (int i = 0; i < offset; i++)
            {
                days.Add(new CalendarDayViewModel
                {
                    Day = previousMonth.AddDays(i).Day.ToString(),
                    IsCurrentMonth = false,
                    Date = previousMonth.AddDays(i)
                });
            }

            // Dias do mês atual
            int daysInMonth = DateTime.DaysInMonth(firstDayOfMonth.Year, firstDayOfMonth.Month);
            for (int i = 1; i <= daysInMonth; i++)
            {
                var currentDate = new DateTime(firstDayOfMonth.Year, firstDayOfMonth.Month, i);
                var day = new CalendarDayViewModel
                {
                    Day = i.ToString(),
                    IsCurrentMonth = true,
                    IsToday = currentDate.Date == DateTime.Today,
                    Date = currentDate
                };

                // Verificar se há compras neste dia
                day.HasPayment = VerificarComprasNaData(currentDate);

                // Verificar se há boletos com vencimento neste dia
                day.HasBoletoVencimento = VerificarBoletosNaData(currentDate);

                days.Add(day);
            }

            // Completar o grid com dias do próximo mês
            int remainingDays = 42 - days.Count; // 6 linhas x 7 colunas = 42 células
            DateTime nextMonth = firstDayOfMonth.AddMonths(1);
            for (int i = 1; i <= remainingDays; i++)
            {
                days.Add(new CalendarDayViewModel
                {
                    Day = i.ToString(),
                    IsCurrentMonth = false,
                    Date = new DateTime(nextMonth.Year, nextMonth.Month, i)
                });
            }

            CalendarDaysControl.ItemsSource = days;
        }
        private bool VerificarComprasNaData(DateTime data)
        {
            if (_todasCompras == null) return false;

            return _todasCompras.Any(c => c.DataCompra.Date == data.Date);
        }
        private bool VerificarBoletosNaData(DateTime data)
        {
            if (_todosBoletos == null) return false;

            return _todosBoletos.Any(b =>
                b.DataVencimento.Date == data.Date &&
                b.DataPagamento == null &&
                b.NotaFiscal != null);
        }

        // 2. Pesquisa

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_todasCompras == null) return;

            string textoBusca = SearchBox.Text?.Trim().ToLower() ?? "";
            // 1. Filtrar compras com base no texto de busca
            _comprasFiltradas = _todasCompras
                .Where(v =>
                    (v.FornecedorNome?.ToLower().Contains(textoBusca) ?? false) ||
                    (v.Itens != null && v.Itens.Any(i => i.ProdutoNome != null && i.ProdutoNome.ToLower().Contains(textoBusca))) ||
                    (v.NotaFiscal?.ToLower().Contains(textoBusca) ?? false)
                )
                .ToList();

            // 2. Reordena os itens para aparecer primeiro os que contêm o texto de busca
            _comprasFiltradas = _comprasFiltradas.OrderByDescending(v =>
                v.FornecedorNome?.ToLower().Contains(textoBusca) == true ? 1 : 0 +
                (v.Itens != null && v.Itens.Any(i => i.ProdutoNome != null && i.ProdutoNome.ToLower().Contains(textoBusca)) ? 1 : 0) +
                (v.NotaFiscal?.ToLower().Contains(textoBusca) == true ? 1 : 0)
            ).ToList();

            AplicarOrdenacao();
            AtualizarInterfaceCompras();
        }

        // 3. Filtros
        private void FiltrarButton_Click(object sender, RoutedEventArgs e)
        {
            FiltroPopup.IsOpen = true;
        }

        private void AplicarFiltroButton_Click(object sender, RoutedEventArgs e)
        {

            if (_todasCompras == null) return;

            // Criar uma nova lista baseada em todas as compras
            _comprasFiltradas = new List<CompraData>(_todasCompras);

            // 1. Filtrar por fornecedor
            string fornecedorSelecionadoId = FornecedorComboBox.SelectedValue as string;
            if (!string.IsNullOrEmpty(fornecedorSelecionadoId))
            {
                _comprasFiltradas = _comprasFiltradas.Where(c => c.FornecedorId == fornecedorSelecionadoId).ToList();
            }

            // 2. Filtrar por produto
            string produtoSelecionadoId = ProdutosCompradosComboBox.SelectedValue as string;
            if (!string.IsNullOrEmpty(produtoSelecionadoId))
            {
                _comprasFiltradas = _comprasFiltradas.Where(c => c.Itens.Any(i => i.ProdutoId == produtoSelecionadoId)).ToList();
            }

            // 3. Filtrar por período
            DateTime? dataInicio = DataInicioPicker.SelectedDate;
            DateTime? dataFim = DataFimPicker.SelectedDate;

            if (dataInicio.HasValue)
            {
                _comprasFiltradas = _comprasFiltradas.Where(c => c.DataCompra.Date >= dataInicio.Value.Date).ToList();
            }

            if (dataFim.HasValue)
            {
                _comprasFiltradas = _comprasFiltradas.Where(c => c.DataCompra.Date <= dataFim.Value.Date).ToList();
            }

            // 4. Filtrar por boletos pendentes
            if (BoletosPagarCheckBox.IsChecked == true)
            {
                _comprasFiltradas = _comprasFiltradas.Where(c =>
                    c.ProximoVencimento.HasValue &&
                    c.ProximoVencimento.Value >= DateTime.Today).ToList();
            }

            // Aplicar direção de ordenação
            var direcaoOrdenacao = (OrdemComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (direcaoOrdenacao == "desc" && _ordenacaoAtual != "recente" && _ordenacaoAtual != "preco")
            {
                // Inverter a ordenação atual para ordem decrescente
                _comprasFiltradas.Reverse();
            }

            FiltroPopup.IsOpen = false;
            AplicarOrdenacao();
            AtualizarInterfaceCompras();
        }

        private void LimparFiltroButton_Click(object sender, RoutedEventArgs e)
        {

            // Limpar seleção de fornecedor e produto
            if (FornecedorComboBox.Items.Count > 0) FornecedorComboBox.SelectedIndex = 0;
            if (ProdutosCompradosComboBox.Items.Count > 0) ProdutosCompradosComboBox.SelectedIndex = 0;

            // Limpar datas
            DataInicioPicker.SelectedDate = null;
            DataFimPicker.SelectedDate = null;

            // Desmarcar checkbox de boletos pendentes
            BoletosPagarCheckBox.IsChecked = false;

            // Ordenação
            _ordenacaoAtual = "recente";
            _filtroTexto = "Ordenar por";

            // Corrigir a referência para OrdemComboBox (estava usando como string)
            if (OrdemComboBox.Items.Count > 0) OrdemComboBox.SelectedIndex = 0;

            // Fechar popup e restaurar lista completa

            FiltroPopup.IsOpen = false;
            if (_todasCompras != null) // Garante que _todasCompras não é nulo
            {
                _comprasFiltradas = new List<CompraData>(_todasCompras);
            }
            else
            {
                _comprasFiltradas = new List<CompraData>();
            }
            _ordenacaoAtual = "recente"; // Resetar ordenação
            _filtroTexto = "Ordenar por";
            if (OrdenarButton != null) OrdenarButton.Content = _filtroTexto;

            AplicarOrdenacao();
            AtualizarInterfaceCompras();
        }

        private void OrdenarButton_Click(object sender, RoutedEventArgs e)
        {
            OrdenarPopup.IsOpen = !OrdenarPopup.IsOpen;
        }
        private void OrdenacaoItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tipoOrdenacao)
            {
                _ordenacaoAtual = tipoOrdenacao;
                // O conteúdo do botão clicado (ex: "Preço", "Recente") será usado para o texto.
                _filtroTexto = $"Ordenar por: {button.Content}";

                // Atualiza o texto do botão de ordenação, se houver TextBlock no template
                if (OrdenarButton.Template.FindName("OrdenarButtonText", OrdenarButton) is TextBlock textBlock)
                {
                    textBlock.Text = _filtroTexto;
                }
                else
                {
                    OrdenarButton.Content = _filtroTexto;

                }

                OrdenarPopup.IsOpen = false;
                AplicarOrdenacao();
                AtualizarInterfaceCompras();
            }
        }
        private void AplicarOrdenacao()
        {
            if (_comprasFiltradas == null || !_comprasFiltradas.Any()) return;

            switch (_ordenacaoAtual)
            {
                case "preco":
                    _comprasFiltradas = _comprasFiltradas.OrderByDescending(v => v.ValorTotal).ToList();
                    break;
                case "produto":
                    _comprasFiltradas = _comprasFiltradas.OrderBy(v => v.Itens?.FirstOrDefault()?.ProdutoNome ?? "").ToList();
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
                case "vencimento":
                    // Ordenar por próximo vencimento, colocando compras sem vencimento no final
                    _comprasFiltradas = _comprasFiltradas
                        .OrderBy(v => v.ProximoVencimento == null) // Primeiro os que têm vencimento (false vem antes de true)
                        .ThenBy(v => v.ProximoVencimento) // Depois ordenar pelos vencimentos mais próximos
                        .ToList();
                    break;
                default:
                    _comprasFiltradas = _comprasFiltradas.OrderByDescending(v => v.DataCompra).ToList();
                    break;
            }
        }

        private void AtualizarInterfaceCompras()
        {
            if (_comprasFiltradas == null || !_comprasFiltradas.Any())
            {
                ComprasContainer.ItemsSource = null;
                MensagemVazia.Visibility = Visibility.Visible;
            }
            else
            {
                MensagemVazia.Visibility = Visibility.Collapsed;
                ComprasContainer.ItemsSource = _comprasFiltradas;
            }
        }

        private void RegistrarCompraButton_Click(object sender, RoutedEventArgs e)
        {
            var comprasWindow = new AddEntradaSaídaWindow(isEntrada: true);
            comprasWindow.ShowDialog();

            // Atualiza a lista de compras após o registro
            CarregarCompras();
            // Atualiza também os boletos e o calendário
            CarregarBoletos();
            CarregarCalendario();
        }

        private void DetalhesButton_Click(object sender, RoutedEventArgs e)
        {
            var compra = (sender as Button)?.DataContext as CompraData;
            if (compra != null)
            {
                var detalhesCompraUserControl = new DetalhesUserControl(compra);

                this.NavigateTo(
                    detalhesCompraUserControl,
                    "Detalhes da Compra",
                    "/assets/Icons/Selected/ComprarS.png"
                );
            }
        }

        public static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindVisualParent<T>(parentObject);
        }

        private void PrevMonthButton_Click(object sender, RoutedEventArgs e)
        {
            MudarMesCalendario(-1);
        }
        private void NextMonthButton_Click(object sender, RoutedEventArgs e)
        {
            MudarMesCalendario(1);
        }
        private void MudarMesCalendario(int incrementoMes)
        {
            _currentCalendarMonth = _currentCalendarMonth.AddMonths(incrementoMes);
            CarregarCalendario();
        }


        private void CalendarDayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is CalendarDayViewModel dayVM)
            {
                if (_selectedDay != null) _selectedDay.IsSelected = false;
                dayVM.IsSelected = true;
                _selectedDay = dayVM;

                DateTime selectedDate = dayVM.Date;

                // Atualizar o texto da data selecionada
                DataSelecionadaText.Text = selectedDate.ToString("dd 'de' MMMM 'de' yyyy");

                // Buscar compras para a data selecionada
                var comprasNoDia = BuscarComprasNoDia(selectedDate);

                // Buscar boletos para a data selecionada
                var boletosNoDia = BuscarBoletosNoDia(selectedDate);

                // Configurar visualização de compras
                if (comprasNoDia.Any())
                {
                    ComprasDoDiaList.ItemsSource = comprasNoDia;
                    ComprasDoDiaList.Visibility = Visibility.Visible;
                    SemComprasNoDiaText.Visibility = Visibility.Collapsed;
                }
                else if (boletosNoDia.Any())
                {
                    // Se não há compras, mas há boletos, mostrar mensagem personalizada
                    ComprasDoDiaList.ItemsSource = null;
                    ComprasDoDiaList.Visibility = Visibility.Collapsed;
                    SemComprasNoDiaText.Text = "Não há compras nesta data.";
                    SemComprasNoDiaText.Visibility = Visibility.Visible;
                }
                else
                {
                    // Se não há compras nem boletos
                    ComprasDoDiaList.ItemsSource = null;
                    ComprasDoDiaList.Visibility = Visibility.Collapsed;
                    SemComprasNoDiaText.Text = "Não há compras ou boletos nesta data.";
                    SemComprasNoDiaText.Visibility = Visibility.Visible;
                }

                // Configurar visualização de boletos
                if (boletosNoDia.Any())
                {
                    BoletosDoDiaList.ItemsSource = boletosNoDia;
                    BoletosDoDiaList.Visibility = Visibility.Visible;
                }
                else
                {
                    BoletosDoDiaList.ItemsSource = null;
                    BoletosDoDiaList.Visibility = Visibility.Collapsed;
                }

                // Mostrar o painel de detalhes
                DiaDetalhesPanel.Visibility = Visibility.Visible;
            }
        }
        private List<CompraData> BuscarComprasNoDia(DateTime data)
        {
            if (_todasCompras == null) return new List<CompraData>();

            return _todasCompras
                .Where(c =>
                    c.DataCompra.Date == data.Date ||
                    (c.DataPagamento != default && c.DataPagamento.Date == data.Date)) // Fixed: Removed HasValue and Value
                .ToList();
        }
        private List<BoletoData> BuscarBoletosNoDia(DateTime data)
        {
            if (_todosBoletos == null)
                return new List<BoletoData>();

            return _todosBoletos
                .Where(b => b.DataVencimento.Date == data.Date && b.DataPagamento == null)
                .ToList();
        }

        // Apresenta os detalhes da compra ao clicar no botão de detalhes
        private void DetalhesCompraCalendarioButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is CompraData compra)
            {
                var detalhesCompraUserControl = new DetalhesUserControl(compra);

                this.NavigateTo(
                    detalhesCompraUserControl,
                    "Detalhes da Compra",
                    "/assets/Icons/Selected/ComprarS.png"
                );
            }
        }

        // Abre o boleto ao clicar no botão de detalhes do boleto
        private void VerBoletoCalendarioButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is BoletoData boleto)
            {
                // Captura o caminho do arquivo do boleto
                string caminhoBoleto = boleto.CaminhoArquivo;
                if (string.IsNullOrEmpty(caminhoBoleto))
                {
                    MessageBox.Show("Caminho do boleto não informado.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Verifica se o arquivo existe
                if (!System.IO.File.Exists(caminhoBoleto))
                {
                    MessageBox.Show($"Arquivo do boleto não encontrado no caminho: {caminhoBoleto}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Tenta abrir o arquivo usando o comando CMD do Windows
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c start \"\" \"{caminhoBoleto}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    System.Diagnostics.Process.Start(psi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Não foi possível abrir o arquivo PDF.\n\nDetalhes: {ex.Message}",
                        "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                }
            }
        }

        // Abre os detalhes da compra ao clicar no botão de detalhes do boleto
        private void VerCompraCalendarioButton_Click(object sender, RoutedEventArgs e)
        {
            // Captura compra com base no boleto selecionado
            if (sender is Button button && button.DataContext is BoletoData boleto)
            {
                // Busca a compra associada ao boleto
                var compra = _todasCompras.FirstOrDefault(c => c.Boletos != null && c.Boletos.Contains(boleto.Id));
                if (compra != null)
                {
                    var detalhesCompraUserControl = new DetalhesUserControl(compra);
                    this.NavigateTo(
                        detalhesCompraUserControl,
                        "Detalhes da Compra",
                        "/assets/Icons/Selected/ComprarS.png"
                    );
                }
            }
        }

        // Recarrega itens necessários
        public void RecarregarItens()
        {
            CarregarCompras();
            CarregarBoletos();
            CarregarCalendario();
            CarregarFornecedores();
            CarregarProdutos();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls.Primitives;
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
        private string _filtroTexto = "Ordenar por";
        private DateTime _currentCalendarMonth = DateTime.Today;
        private CalendarDayViewModel _selectedDay; // Variável para armazenar o dia selecionado atualmente

        public ComprasUserControl()
        {
            InitializeComponent();
            Loaded += ComprasUserControl_Loaded;
        }

        // 1. Carregamento inicial
        private void ComprasUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CarregarCompras();
            CarregarBoletos(); 
            CarregarCalendario();
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

        private void CarregarBoletos()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db != null)
                {
                    var collection = db.GetCollection<BoletoData>("boletos");
                    _todosBoletos = collection.FindAll().ToList();
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

            return _todosBoletos.Any(b => b.Vencimento.Date == data.Date && b.Pagamento == null);
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
            // Atualiza também os boletos e o calendário
            CarregarBoletos();
            CarregarCalendario();
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


        // Navegação do calendário
        private void PrevMonthButton_Click(object sender, RoutedEventArgs e)
        {
            // Navegar para o mês anterior
            MudarMesCalendario(-1);
        }
        private void NextMonthButton_Click(object sender, RoutedEventArgs e)
        {
            // Navegar para o próximo mês
            MudarMesCalendario(1);
        }
        private void MudarMesCalendario(int incrementoMes)
        {
            _currentCalendarMonth = _currentCalendarMonth.AddMonths(incrementoMes);
            CarregarCalendario();
        }

        // Dia selecionada no calendário
        private void CalendarDayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DateTime selectedDate)
            {
                // Encontra o objeto CalendarDayViewModel do dia clicado
                var days = CalendarDaysControl.ItemsSource as List<CalendarDayViewModel>;
                if (days != null)
                {
                    // Limpar a seleção anterior
                    if (_selectedDay != null)
                    {
                        _selectedDay.IsSelected = false;
                    }

                    // Definir o novo dia selecionado
                    var clickedDay = days.FirstOrDefault(d => d.Date.Date == selectedDate.Date);
                    if (clickedDay != null)
                    {
                        clickedDay.IsSelected = true;
                        _selectedDay = clickedDay;
                    }
                }

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
            if (_comprasFiltradas == null)
                return new List<CompraData>();

            return _comprasFiltradas
                .Where(c => c.DataCompra.Date == data.Date)
                .ToList();
        }
        private List<BoletoData> BuscarBoletosNoDia(DateTime data)
        {
            if (_todosBoletos == null)
                return new List<BoletoData>();

            return _todosBoletos
                .Where(b => b.Vencimento.Date == data.Date && b.Pagamento == null)
                .ToList();
        }

        // Apresenta os detalhes da compra ao clicar no botão de detalhes
        private void DetalhesCompraCalendarioButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is CompraData compra)
            {
                var detalhesCompraUserControl = new DetalhesUserControl(compra);
                var contentControl = (Parent as ContentControl);
                if (contentControl != null)
                {
                    contentControl.Content = detalhesCompraUserControl;
                }
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
    }
}
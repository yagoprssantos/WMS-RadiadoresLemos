using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // Adicionado para VisualTreeHelper
// using System.Windows.Media.Imaging; // Não usado diretamente aqui
// using System.Windows.Controls.Primitives; // Não usado diretamente aqui
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;
// Removida a referência a WMS_RadiadoresLemos_WPF.src.Views.Windows pois CadastroBoletoCompraWindow não é mais usada aqui

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class ComprasUserControl : UserControl
    {
        private List<CompraData> _todasCompras;
        private List<CompraData> _comprasFiltradas;
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
            CarregarCalendario(); // Se o calendário ainda for parte desta tela
            if (OrderButton != null) // Garante que o botão de ordenação tenha o texto inicial correto
            {
                OrderButton.Content = _filtroTexto;
            }
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
                    AplicarOrdenacao(); // Certifique-se que a ordenação é aplicada
                    AtualizarInterfaceCompras();
                }
                else
                {
                    MessageBox.Show("Não foi possível conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    _todasCompras = new List<CompraData>(); // Inicializa para evitar NullReferenceException
                    _comprasFiltradas = new List<CompraData>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar compras: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                _todasCompras = new List<CompraData>(); // Inicializa em caso de erro
                _comprasFiltradas = new List<CompraData>();
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_todasCompras == null) return;

            string textoBusca = SearchBox.Text?.Trim().ToLower() ?? "";
            _comprasFiltradas = _todasCompras
                .Where(v =>
                    (v.FornecedorNome?.ToLower().Contains(textoBusca) ?? false) ||
                    (v.Itens != null && v.Itens.Any(i => i.ProdutoNome != null && i.ProdutoNome.ToLower().Contains(textoBusca))) ||
                    (v.NotaFiscal?.ToLower().Contains(textoBusca) ?? false)
                )
                .ToList();

            AplicarOrdenacao();
            AtualizarInterfaceCompras();
        }

        private void FiltrarButton_Click(object sender, RoutedEventArgs e)
        {
            FiltroPopup.IsOpen = true;
        }

        private void AplicarFiltroButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implementar lógica de filtro específica
            FiltroPopup.IsOpen = false;
            AplicarOrdenacao();
            AtualizarInterfaceCompras();
        }

        private void LimparFiltroButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Limpar controles de filtro
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
            if (OrderButton != null) OrderButton.Content = _filtroTexto;

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

                if (OrderButton != null)
                {
                    // O estilo FilterButtonStyle usa ContentPresenter diretamente, então mudar Content é o correto.
                    OrderButton.Content = _filtroTexto;
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
            CarregarCompras();
        }

        private void DetalhesButton_Click(object sender, RoutedEventArgs e)
        {
            var compra = (sender as Button)?.DataContext as CompraData;
            if (compra != null)
            {
                var detalhesCompraUserControl = new DetalhesUserControl(compra);

                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null && mainWindow.FindName("ContentArea") is ContentControl contentArea)
                {
                    contentArea.Content = detalhesCompraUserControl;
                }
                else
                {
                    var parentContentControl = FindVisualParent<ContentControl>(this);
                    if (parentContentControl != null)
                    {
                        parentContentControl.Content = detalhesCompraUserControl;
                    }
                    else
                    {
                        MessageBox.Show("Não foi possível encontrar a área de conteúdo para exibir os detalhes.", "Erro de Navegação", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
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

        private void CarregarCalendario()
        {
            CalendarMonthText.Text = _currentCalendarMonth.ToString("MMMM yyyy", new System.Globalization.CultureInfo("pt-BR")); // Formatado para Português

            var days = new List<CalendarDayViewModel>();
            DateTime firstDayOfMonth = new DateTime(_currentCalendarMonth.Year, _currentCalendarMonth.Month, 1);
            int offset = ((int)firstDayOfMonth.DayOfWeek);

            DateTime dayIterator = firstDayOfMonth.AddDays(-offset);

            for (int i = 0; i < 42; i++)
            {
                var dayVM = new CalendarDayViewModel
                {
                    Day = dayIterator.Day.ToString(),
                    IsCurrentMonth = dayIterator.Month == _currentCalendarMonth.Month,
                    IsToday = dayIterator.Date == DateTime.Today,
                    Date = dayIterator.Date,
                    HasPayment = VerificarPagamentosNaData(dayIterator.Date)
                };
                days.Add(dayVM);
                dayIterator = dayIterator.AddDays(1);
            }
            CalendarDaysControl.ItemsSource = days;
        }

        private bool VerificarPagamentosNaData(DateTime data)
        {
            if (_todasCompras == null) return false;
            return _todasCompras.Any(c =>
                (c.DataCompra.Date == data.Date) ||
                (c.DataPagamento.HasValue && c.DataPagamento.Value.Date == data.Date)
            );
        }

        private void CalendarDayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is CalendarDayViewModel dayVM)
            {
                if (_selectedDay != null) _selectedDay.IsSelected = false;
                dayVM.IsSelected = true;
                _selectedDay = dayVM;

                DataSelecionadaText.Text = dayVM.Date.ToString("dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("pt-BR"));
                var comprasNoDia = BuscarComprasNoDia(dayVM.Date);

                if (comprasNoDia.Any())
                {
                    ComprasDoDiaList.ItemsSource = comprasNoDia;
                    ComprasDoDiaList.Visibility = Visibility.Visible;
                    SemComprasNoDiaText.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ComprasDoDiaList.ItemsSource = null;
                    ComprasDoDiaList.Visibility = Visibility.Collapsed;
                    SemComprasNoDiaText.Visibility = Visibility.Visible;
                }
                DiaDetalhesPanel.Visibility = Visibility.Visible;
            }
        }

        private List<CompraData> BuscarComprasNoDia(DateTime data)
        {
            if (_todasCompras == null) return new List<CompraData>();
            return _todasCompras
                .Where(c => (c.DataCompra.Date == data.Date) || (c.DataPagamento.HasValue && c.DataPagamento.Value.Date == data.Date))
                .ToList();
        }

        private void DetalhesCompraCalendarioButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is CompraData compra)
            {
                var detalhesCompraUserControl = new DetalhesUserControl(compra);
                var parentContentControl = FindVisualParent<ContentControl>(this);
                if (parentContentControl != null)
                {
                    parentContentControl.Content = detalhesCompraUserControl;
                }
            }
        }
    }

    public class CalendarDayViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public string Day { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsToday { get; set; }
        public DateTime Date { get; set; }
        public bool HasPayment { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); OnPropertyChanged(nameof(SelectedBorderThickness)); }
        }

        public Thickness SelectedBorderThickness => IsSelected ? new Thickness(2) : new Thickness(0);
        public Visibility TodayIndicatorVisibility => IsToday ? Visibility.Visible : Visibility.Collapsed;
        public FontWeight FontWeight => IsCurrentMonth ? FontWeights.SemiBold : FontWeights.Normal; // Ajustado para SemiBold
        public Visibility PaymentVisibility => HasPayment ? Visibility.Visible : Visibility.Collapsed;

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}

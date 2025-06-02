using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace WMS_RadiadoresLemos_WPF.src.Models
{

    public class CalendarDayViewModel : INotifyPropertyChanged
    {
        public string Day { get; set; }
        public DateTime Date { get; set; }

        private bool _isCurrentMonth;
        public bool IsCurrentMonth
        {
            get => _isCurrentMonth;
            set
            {
                _isCurrentMonth = value;
                OnPropertyChanged(nameof(Foreground));
            }
        }

        private bool _isToday;
        public bool IsToday
        {
            get => _isToday;
            set
            {
                _isToday = value;
                OnPropertyChanged(nameof(TodayIndicatorVisibility));
                OnPropertyChanged(nameof(FontWeight));
            }
        }

        private bool _hasPayment;
        public bool HasPayment
        {
            get => _hasPayment;
            set
            {
                _hasPayment = value;
                OnPropertyChanged(nameof(PaymentVisibility));
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(SelectedBorderThickness));
                OnPropertyChanged(nameof(FontWeight));
            }
        }

        // Cor do texto do dia
        public Brush Foreground => IsCurrentMonth ?
            new SolidColorBrush(Colors.Black) :
            new SolidColorBrush(Color.FromArgb(127, 0, 0, 0));

        // Visibilidade do indicador de dia atual
        public Visibility TodayIndicatorVisibility => IsToday ? Visibility.Visible : Visibility.Collapsed;

        // Espessura da borda para dia selecionado
        public Thickness SelectedBorderThickness => IsSelected ? new Thickness(2) : new Thickness(0);

        // Estilo do texto
        public FontWeight FontWeight => IsToday || IsSelected ? FontWeights.Bold : FontWeights.Normal;

        // Visibilidade do indicador de pagamento
        public Visibility PaymentVisibility => HasPayment ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
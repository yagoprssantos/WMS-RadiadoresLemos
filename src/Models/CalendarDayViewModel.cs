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
                UpdateVisualProperties();
                OnPropertyChanged(nameof(IsCurrentMonth));
            }
        }

        private bool _isToday;
        public bool IsToday 
        {
            get => _isToday;
            set
            {
                _isToday = value;
                UpdateVisualProperties();
                OnPropertyChanged(nameof(IsToday));
            }
        }

        private bool _hasPayment;
        public bool HasPayment 
        {
            get => _hasPayment;
            set
            {
                _hasPayment = value;
                UpdateVisualProperties();
                OnPropertyChanged(nameof(HasPayment));
            }
        }

        private bool _hasBoletoVencimento;
        public bool HasBoletoVencimento
        {
            get => _hasBoletoVencimento;
            set
            {
                _hasBoletoVencimento = value;
                UpdateVisualProperties();
                OnPropertyChanged(nameof(HasBoletoVencimento));
            }
        }

        private bool _isSelected;
        public bool IsSelected 
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                UpdateVisualProperties();
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        // Propriedades visuais derivadas
        public Brush Foreground => IsCurrentMonth ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(128, 255, 255, 255));
        public Visibility TodayIndicatorVisibility => IsToday ? Visibility.Visible : Visibility.Collapsed;
        public Thickness SelectedBorderThickness => IsSelected ? new Thickness(2) : new Thickness(0);
        public FontWeight FontWeight => IsToday || IsSelected ? FontWeights.Bold : FontWeights.Normal;
        public Visibility PaymentVisibility => HasPayment ? Visibility.Visible : Visibility.Collapsed;
        public Visibility BoletoVisibility => HasBoletoVencimento ? Visibility.Visible : Visibility.Collapsed;

        private void UpdateVisualProperties()
        {
            OnPropertyChanged(nameof(Foreground));
            OnPropertyChanged(nameof(TodayIndicatorVisibility));
            OnPropertyChanged(nameof(SelectedBorderThickness));
            OnPropertyChanged(nameof(FontWeight));
            OnPropertyChanged(nameof(PaymentVisibility));
            OnPropertyChanged(nameof(BoletoVisibility));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
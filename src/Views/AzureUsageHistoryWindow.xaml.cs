using System;
using System.Windows;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.Views
{
    public partial class AzureUsageHistoryWindow : Window
    {
        public AzureUsageHistoryWindow()
        {
            InitializeComponent();
            LoadUsageHistory();
        }

        private void LoadUsageHistory()
        {
            try
            {
                var stats = AzureUsageStats.LoadAllStats();
                UsageHistoryGrid.ItemsSource = stats;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar histórico: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadUsageHistory();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 
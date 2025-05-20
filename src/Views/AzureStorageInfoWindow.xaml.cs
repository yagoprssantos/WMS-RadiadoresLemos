using System;
using System.Collections.Generic;
using System.Windows;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF.Views
{
    public partial class AzureStorageInfoWindow : Window
    {
        private readonly AzureStorageService _azureService;

        public AzureStorageInfoWindow()
        {
            InitializeComponent();
            _azureService = new AzureStorageService();
            LoadAzureStorageInfo();
        }

        private async void LoadAzureStorageInfo()
        {
            try
            {
                var files = await _azureService.ListBackupFilesAsync();
                FilesListView.ItemsSource = files;

                // Obtém estatísticas detalhadas do container
                var stats = await _azureService.GetContainerStatsAsync();
                
                // Atualiza as informações na interface
                TotalSpaceText.Text = stats.FormattedSize;
                TotalFilesText.Text = stats.TotalFiles.ToString();
                LastModifiedText.Text = stats.LastModifiedFormatted;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar informações do Azure: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private long ParseFileSize(string size)
        {
            var parts = size.Split(' ');
            if (parts.Length != 2) return 0;

            double value = double.Parse(parts[0]);
            string unit = parts[1].ToUpper();

            switch (unit)
            {
                case "B": return (long)value;
                case "KB": return (long)(value * 1024);
                case "MB": return (long)(value * 1024 * 1024);
                case "GB": return (long)(value * 1024 * 1024 * 1024);
                case "TB": return (long)(value * 1024 * 1024 * 1024 * 1024);
                default: return 0;
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ShowHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var historyWindow = new AzureUsageHistoryWindow();
            historyWindow.ShowDialog();
        }
    }
} 
using System;
using System.Windows;
using System.IO;
using System.Diagnostics;

namespace WMS_RadiadoresLemos_WPF.Views
{
    public partial class ImportSuccessWindow : Window
    {
        private readonly string _caminhoTemp;
        private readonly string _bancoAtual;

        public bool Confirmado { get; private set; }

        public ImportSuccessWindow(string fileName, DateTime date, string source, string destination)
        {
            InitializeComponent();

            FileNameText.Text = $"Arquivo: {fileName}";
            DateText.Text = $"Data: {date:dd/MM/yyyy HH:mm}";
            SourceText.Text = $"Origem: {source}";
            DestinationText.Text = $"Destino: {destination}";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Confirmado = true;
            DialogResult = true;
        }
    }
} 
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

        public ImportSuccessWindow(string backupFileName, DateTime backupDate, string sourcePath, string destinationPath)
        {
            InitializeComponent();

            BackupInfoText.Text = $"Arquivo importado: {backupFileName}\n" +
                                $"Data do backup: {backupDate:dd/MM/yyyy HH:mm:ss}\n" +
                                $"Origem: {sourcePath}\n" +
                                $"Destino: {destinationPath}";
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            Confirmado = true;
            DialogResult = true;
        }
    }
} 
using System;
using System.Windows;
using System.IO;
using System.Diagnostics;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class ImportSuccessWindow : Window
    {
        private readonly string _caminhoTemp;
        private readonly string _bancoAtual;

        public bool Confirmado { get; private set; }

        public ImportSuccessWindow(string nomeBackup, DateTime dataBackup, string caminhoTemp, string bancoAtual)
        {
            InitializeComponent();
            _caminhoTemp = caminhoTemp;
            _bancoAtual = bancoAtual;

            NomeBackupText.Text = $"{nomeBackup}";
            DataBackupText.Text = $"{dataBackup:dd/MM/yyyy HH:mm:ss}";
        }

        private void ConfirmarButton_Click(object sender, RoutedEventArgs e)
        {
            Confirmado = true;

            // Limpa o arquivo temporário
            if (File.Exists(_caminhoTemp))
            {
                try
                {
                    File.Delete(_caminhoTemp);
                }
                catch { /* Ignora erro ao deletar arquivo temporário */ }
            }

            // Reinicia o aplicativo
            var startInfo = new ProcessStartInfo
            {
                FileName = Process.GetCurrentProcess().MainModule.FileName,
                UseShellExecute = true
            };
            Process.Start(startInfo);
            Application.Current.Shutdown();
        }
    }
} 
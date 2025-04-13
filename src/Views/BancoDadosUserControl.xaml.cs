using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using WMS_RadiadoresLemos_WPF.src.Models;
using Microsoft.Win32;
using ClosedXML.Excel;
using System.IO;
using System.Windows;
using WMS_RadiadoresLemos_WPF.src.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DocumentFormat.OpenXml.Packaging;
using WMS_RadiadoresLemos_WPF.src.Views;
using System.Windows.Media;
using System.Diagnostics;
using LiteDB;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class BancoDadosUserControl : UserControl
    {
        private List<object> dadosFiltrados = new List<object>();
        private bool dadosCarregados = false;
        private List<string> tabelasSelecionadas = new List<string>();
        private static readonly string[] TabelasDisponiveis = { "usuarios", "produtos", "historico", "movimentacoes" };

        public BancoDadosUserControl()
        {
            InitializeComponent();
            DataContext = this;
            SetupLinks();
        }

        // Método para configurar botões de links (arquivos locais e banco de dados)
        private void SetupLinks()
        {
            // Configura o evento do botão para abrir arquivos locais
            var abrirArquivosLocaisButton = FindName("AbrirArquivosLocaisButton") as Button;
            if (abrirArquivosLocaisButton != null)
            {
                abrirArquivosLocaisButton.Click += AbrirArquivosLocais_Click;
            }

            // Configura o evento do botão para abrir o OneDrive
            var abrirOneDriveButton = FindName("AbrirOneDriveButton") as Button;
            if (abrirOneDriveButton != null)
            {
                abrirOneDriveButton.Click += AbrirOneDrive_Click;
            }
        }

        private void AbrirArquivosLocais_Click(object sender, RoutedEventArgs e)
        {
            // Abre o diretório de arquivos locais
            Process.Start(new ProcessStartInfo
            {
                // Diretório "DadosBancoDeDadosOffline" dentro do diretório atual do projeto
                FileName = Path.Combine(Directory.GetCurrentDirectory(), "DadosBancoDeDadosOffline"),

                // Abre o diretório no explorador de arquivos
                UseShellExecute = true
            });
        }

        private void AbrirOneDrive_Click(object sender, RoutedEventArgs e)
        {
            // Abre o OneDrive
            Process.Start("explorer.exe", "shell:OneDrive");
        }

        // Evento disparado para visualizar dados em Excel
        private void VisualizarExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var excelWindow = new ExcelWindow();
            excelWindow.ShowDialog();
        }

        // Adiciona o evento de clique ao botão "Abrir Menu Tabelas"
        private void AbrirMenuTabelasButton_Click(object sender, RoutedEventArgs e)
        {
            var menuTabelasWindow = new MenuTabelasWindow();
            menuTabelasWindow.ShowDialog();
        }
    }
}
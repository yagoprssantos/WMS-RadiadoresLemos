using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WMS_RadiadoresLemos_WPF.src.Services;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Views
{

    public partial class RegistroUserControl : UserControl
    {
        public RegistroUserControl()
        {
            InitializeComponent();
            CarregarEntradas();
            CarregarSaidas();
            CarregarHistorico();

            // Seleciona primeira opção de categoria
            CategoriasComboBox.SelectedIndex = 0;
        }

        // Método para carregar dados no DataGrid de Entradas
        private void CarregarEntradas()
        {
            try
            {
                var movimentacoes = MovimentacoesCache.ObterMovimentacoes();
                var entradas = movimentacoes.Where(m => m.Tipo == "Entrada").ToList();
                EntradaDataGrid.ItemsSource = entradas;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar entradas: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para carregar dados no DataGrid de Saídas
        private void CarregarSaidas()
        {
            try
            {
                var movimentacoes = MovimentacoesCache.ObterMovimentacoes();
                var saidas = movimentacoes.Where(m => m.Tipo == "Saída").ToList();
                SaidaDataGrid.ItemsSource = saidas;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar saídas: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para carregar dados no DataGrid de Histórico
        private async void CarregarHistorico()
        {
            try
            {
                var historico = await Task.Run(() => LogHistorico.ObterLogs());
                HistoricoDataGrid.ItemsSource = historico;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar histórico: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Evento para alterar a visibilidade das grids com base na categoria selecionada
        private void CategoriasComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Altera a visibilidade das grids
            var comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                var selectedCategory = comboBox.SelectedItem as ComboBoxItem;
                if (selectedCategory != null)
                {
                    string category = selectedCategory.Content.ToString();
                    if (category == "Entrada/Saída")
                    {
                        EntradasSaidasGrid.Visibility = Visibility.Visible;
                        HistoricoGrid.Visibility = Visibility.Collapsed;
                    }
                    else if (category == "Histórico")
                    {
                        EntradasSaidasGrid.Visibility = Visibility.Collapsed;
                        HistoricoGrid.Visibility = Visibility.Visible;
                    }
                }
            }
        }


        // Evento para aplicar o filtro selecionado
        private void FiltroComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                var selectedFilter = comboBox.SelectedItem as ComboBoxItem;
                if (selectedFilter != null)
                {
                    string filter = selectedFilter.Content.ToString();
                    AplicarFiltro(filter);
                }
            }
        }

        // Método para aplicar o filtro selecionado
        private void AplicarFiltro(string filtro)
        {
            
        }
    }
}

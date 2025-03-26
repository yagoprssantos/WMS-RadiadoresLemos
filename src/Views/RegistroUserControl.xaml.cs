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
            CarregarProdutos();

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
                //MessageBox.Show($"Erro ao carregar entradas: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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
                //MessageBox.Show($"Erro ao carregar saídas: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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
                //MessageBox.Show($"Erro ao carregar histórico: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para carregar produtos no ComboBox
        private void CarregarProdutos()
        {
            try
            {
                var movimentacoes = MovimentacoesCache.ObterMovimentacoes();
                var produtos = movimentacoes.Select(m => m.ProdutoId).Distinct().ToList();
                ProdutoComboBox.ItemsSource = produtos;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar produtos: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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

        // Evento para abrir o popup de filtro
        private void FiltrarButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedCategory = CategoriasComboBox.SelectedItem as ComboBoxItem;
            if (selectedCategory != null)
            {
                string category = selectedCategory.Content.ToString();
                if (category == "Entrada/Saída")
                {
                    FiltroEntradaSaidaPopup.IsOpen = true;
                }
                else if (category == "Histórico")
                {
                    FiltroHistoricoPopup.IsOpen = true;
                }
            }
        }

        // Evento para aplicar o filtro de Entrada/Saída
        private void AplicarFiltroEntradaSaidaButton_Click(object sender, RoutedEventArgs e)
        {
            string produto = ProdutoComboBox.SelectedItem?.ToString();
            DateTime? dataInicio = DataInicioPicker.SelectedDate;
            DateTime? dataFim = DataFimPicker.SelectedDate;

            AplicarFiltroEntradaSaida(produto, dataInicio, dataFim);
            FiltroEntradaSaidaPopup.IsOpen = false;
        }

        // Evento para limpar os filtros de Entrada/Saída
        private void LimparFiltroEntradaSaidaButton_Click(object sender, RoutedEventArgs e)
        {
            ProdutoComboBox.SelectedItem = null;
            DataInicioPicker.SelectedDate = null;
            DataFimPicker.SelectedDate = null;

            // Recarregar todas as movimentações
            CarregarEntradas();
            CarregarSaidas();
            FiltroEntradaSaidaPopup.IsOpen = false;
        }

        // Método para aplicar o filtro de Entrada/Saída
        private void AplicarFiltroEntradaSaida(string produto, DateTime? dataInicio, DateTime? dataFim)
        {
            try
            {
                var movimentacoes = MovimentacoesCache.ObterMovimentacoes();

                // Filtrar entradas
                var entradasFiltradas = movimentacoes.Where(m =>
                    m.Tipo == "Entrada" &&
                    (string.IsNullOrEmpty(produto) || m.ProdutoId == produto) &&
                    (!dataInicio.HasValue || m.Data >= dataInicio.Value) &&
                    (!dataFim.HasValue || m.Data <= dataFim.Value)).ToList();

                // Filtrar saídas
                var saidasFiltradas = movimentacoes.Where(m =>
                    m.Tipo == "Saída" &&
                    (string.IsNullOrEmpty(produto) || m.ProdutoId == produto) &&
                    (!dataInicio.HasValue || m.Data >= dataInicio.Value) &&
                    (!dataFim.HasValue || m.Data <= dataFim.Value)).ToList();

                // Atualizar DataGrids
                EntradaDataGrid.ItemsSource = entradasFiltradas;
                SaidaDataGrid.ItemsSource = saidasFiltradas;
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Erro ao aplicar filtro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Evento para aplicar o filtro de Histórico
        private void AplicarFiltroHistoricoButton_Click(object sender, RoutedEventArgs e)
        {
            string tipo = (TipoComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            string nivel = (NivelComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            DateTime? dataInicio = DataInicioHistoricoPicker.SelectedDate;
            DateTime? dataFim = DataFimHistoricoPicker.SelectedDate;

            AplicarFiltroHistorico(tipo, nivel, dataInicio, dataFim);
            FiltroHistoricoPopup.IsOpen = false;
        }

        // Método para aplicar o filtro de Histórico
        private void AplicarFiltroHistorico(string tipo, string nivel, DateTime? dataInicio, DateTime? dataFim)
        {
            try
            {
                var historico = LogHistorico.ObterLogs();

                var historicoFiltrado = historico.Where(h =>
                    (string.IsNullOrEmpty(tipo) || h.Tipo.Equals(tipo, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(nivel) || h.Nivel == nivel) &&
                    (!dataInicio.HasValue || h.Data >= dataInicio.Value) &&
                    (!dataFim.HasValue || h.Data <= dataFim.Value)).ToList();

                // Atualizar HistoricoDataGrid
                HistoricoDataGrid.ItemsSource = historicoFiltrado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao aplicar filtro: {ex.Message}");
            }
        }

        // Evento para limpar os filtros de Histórico
        private void LimparFiltroHistoricoButton_Click(object sender, RoutedEventArgs e)
        {
            TipoComboBox.SelectedItem = null;
            NivelComboBox.SelectedItem = null;
            DataInicioHistoricoPicker.SelectedDate = null;
            DataFimHistoricoPicker.SelectedDate = null;

            // Recarregar histórico
            CarregarHistorico();
            FiltroHistoricoPopup.IsOpen = false;
        }
    }
}

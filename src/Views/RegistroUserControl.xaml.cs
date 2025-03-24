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
                        // Ocultar filtros adicionais
                        FiltroTipoTextBlock.Visibility = Visibility.Collapsed;
                        TipoComboBox.Visibility = Visibility.Collapsed;
                        FiltroNivelTextBlock.Visibility = Visibility.Collapsed;
                        NivelComboBox.Visibility = Visibility.Collapsed;
                    }
                    else if (category == "Histórico")
                    {
                        EntradasSaidasGrid.Visibility = Visibility.Collapsed;
                        HistoricoGrid.Visibility = Visibility.Visible;
                        // Mostrar filtros adicionais
                        FiltroTipoTextBlock.Visibility = Visibility.Visible;
                        TipoComboBox.Visibility = Visibility.Visible;
                        FiltroNivelTextBlock.Visibility = Visibility.Visible;
                        NivelComboBox.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        // Evento para abrir o popup de filtro
        private void FiltrarButton_Click(object sender, RoutedEventArgs e)
        {
            FiltroPopup.IsOpen = true;
        }

        // Evento para aplicar o filtro selecionado
        private void AplicarFiltroButton_Click(object sender, RoutedEventArgs e)
        {
            string produto = ProdutoComboBox.SelectedItem?.ToString();
            DateTime? dataInicio = DataInicioPicker.SelectedDate;
            DateTime? dataFim = DataFimPicker.SelectedDate;

            AplicarFiltro(produto, dataInicio, dataFim);
            FiltroPopup.IsOpen = false;
        }

        // Evento para limpar os filtros
        private void LimparFiltroButton_Click(object sender, RoutedEventArgs e)
        {
            ProdutoComboBox.SelectedItem = null;
            DataInicioPicker.SelectedDate = null;
            DataFimPicker.SelectedDate = null;

            // Historico
            TipoComboBox.SelectedItem = null;
            NivelComboBox.SelectedItem = null;

            // Recarregar todas as movimentações
            CarregarEntradas();
            CarregarSaidas();
            FiltroPopup.IsOpen = false;
        }

        // Método para aplicar o filtro selecionado
        private void AplicarFiltro(string produto, DateTime? dataInicio, DateTime? dataFim)
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

                // Filtrar histórico
                var historico = LogHistorico.ObterLogs();
                var tipo = TipoComboBox.SelectedItem?.ToString();
                var nivel = NivelComboBox.SelectedItem?.ToString();

                var historicoFiltrado = historico.Where(h =>
                    (string.IsNullOrEmpty(tipo) || h.Tipo == tipo) &&
                    (string.IsNullOrEmpty(nivel) || h.Nivel == nivel) &&
                    (!dataInicio.HasValue || h.Data >= dataInicio.Value) &&
                    (!dataFim.HasValue || h.Data <= dataFim.Value)).ToList();

                // Atualizar HistoricoDataGrid
                HistoricoDataGrid.ItemsSource = historicoFiltrado;
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Erro ao aplicar filtro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

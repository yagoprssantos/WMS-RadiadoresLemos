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

        // Método para carregar dados no DataGrid de Entradas/Saídas
        public void CarregarEntradas()
        {
            try
            {
                if (DatabaseConnect.Database == null)
                {
                    MessageBox.Show("Erro ao conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var collection = DatabaseConnect.Database.GetCollection<MovimentacaoData>("movimentacoes");
                var movimentacoes = collection.FindAll().ToList();
                var entradas = movimentacoes.Where(m => m.Tipo == "Entrada").ToList();
                EntradaDataGrid.ItemsSource = entradas;
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Erro ao carregar entradas: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public void CarregarSaidas()
        {
            try
            {
                if (DatabaseConnect.Database == null)
                {
                    MessageBox.Show("Erro ao conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var collection = DatabaseConnect.Database.GetCollection<MovimentacaoData>("movimentacoes");
                var movimentacoes = collection.FindAll().ToList();
                var saidas = movimentacoes.Where(m => m.Tipo == "Saída").ToList();
                SaidaDataGrid.ItemsSource = saidas;
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Erro ao carregar saídas: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para carregar dados no DataGrid de Histórico
        public void CarregarHistorico()
        {
            try
            {
                if (DatabaseConnect.Database == null)
                {
                    MessageBox.Show("Erro ao conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var collection = DatabaseConnect.Database.GetCollection<LogData>("historico");
                var historico = collection.FindAll().ToList();
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
                if (DatabaseConnect.Database == null)
                {
                    MessageBox.Show("Erro ao conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var collection = DatabaseConnect.Database.GetCollection<MovimentacaoData>("movimentacoes");
                var movimentacoes = collection.FindAll().ToList();
                var produtos = movimentacoes.Select(m => m.ProdutoId).Distinct().OrderBy(p => p).ToList();
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

                    // Aba Entrada/Saída
                    if (category == "Entrada/Saída")
                    {
                        EntradasSaidasGrid.Visibility = Visibility.Visible;
                        HistoricoGrid.Visibility = Visibility.Collapsed;
                    }
                    // Aba Histórico
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

                // Popup para Entrada/Saída
                if (category == "Entrada/Saída")
                {
                    FiltroEntradaSaidaPopup.IsOpen = true;
                }
                // Popup para Histórico
                else if (category == "Histórico")
                {
                    FiltroHistoricoPopup.IsOpen = true;
                }
            }
        }


        // Evento Filtros Entrada/Saída
        private void AplicarFiltroEntradaSaidaButton_Click(object sender, RoutedEventArgs e)
        {
            string produto = ProdutoComboBox.SelectedItem?.ToString();
            DateTime? dataInicio = DataInicioPicker.SelectedDate;
            DateTime? dataFim = DataFimPicker.SelectedDate;

            AplicarFiltroEntradaSaida(produto, dataInicio, dataFim);
            FiltroEntradaSaidaPopup.IsOpen = false;
        }
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
        private void AplicarFiltroEntradaSaida(string produto, DateTime? dataInicio, DateTime? dataFim)
        {
            try
            {
                if (DatabaseConnect.Database == null)
                {
                    MessageBox.Show("Erro ao conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var collection = DatabaseConnect.Database.GetCollection<MovimentacaoData>("movimentacoes");
                var movimentacoes = collection.FindAll().ToList();

                // Filtrar entradas
                var entradasFiltradas = movimentacoes.Where(m =>
                    m.Tipo == "Entrada" &&
                    (string.IsNullOrEmpty(produto) || m.ProdutoId.Equals(produto, StringComparison.OrdinalIgnoreCase)) &&
                    (!dataInicio.HasValue || m.Data.Date >= dataInicio.Value.Date) &&
                    (!dataFim.HasValue || m.Data.Date <= dataFim.Value.Date)).ToList();

                // Filtrar saídas
                var saidasFiltradas = movimentacoes.Where(m =>
                    m.Tipo == "Saída" &&
                    (string.IsNullOrEmpty(produto) || m.ProdutoId.Equals(produto, StringComparison.OrdinalIgnoreCase)) &&
                    (!dataInicio.HasValue || m.Data.Date >= dataInicio.Value.Date) &&
                    (!dataFim.HasValue || m.Data.Date <= dataFim.Value.Date)).ToList();

                // Atualizar DataGrids
                EntradaDataGrid.ItemsSource = entradasFiltradas;
                SaidaDataGrid.ItemsSource = saidasFiltradas;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao aplicar filtro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // Evento Filtros Histórico
        private void AplicarFiltroHistoricoButton_Click(object sender, RoutedEventArgs e)
        {
            string tipo = (TipoComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            string nivel = (NivelComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            DateTime? dataInicio = DataInicioHistoricoPicker.SelectedDate;
            DateTime? dataFim = DataFimHistoricoPicker.SelectedDate;

            AplicarFiltroHistorico(tipo, nivel, dataInicio, dataFim);
            FiltroHistoricoPopup.IsOpen = false;
        }
        private void AplicarFiltroHistorico(string tipo, string nivel, DateTime? dataInicio, DateTime? dataFim)
        {
            try
            {
                if (DatabaseConnect.Database == null)
                {
                    MessageBox.Show("Erro ao conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var collection = DatabaseConnect.Database.GetCollection<LogData>("historico");
                var historico = collection.FindAll().ToList();

                var historicoFiltrado = historico.Where(h =>
                    (string.IsNullOrEmpty(tipo) || h.Tipo.Equals(tipo, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(nivel) || h.Nivel.Equals(nivel, StringComparison.OrdinalIgnoreCase)) &&
                    (!dataInicio.HasValue || h.Data.Date >= dataInicio.Value.Date) &&
                    (!dataFim.HasValue || h.Data.Date <= dataFim.Value.Date)).ToList();

                HistoricoDataGrid.ItemsSource = historicoFiltrado;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao aplicar filtro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
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

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var selectedCategory = CategoriasComboBox.SelectedItem as ComboBoxItem;
            if (selectedCategory == null)
                return;

            string searchText = SearchBox.Text.ToLower();
            string category = selectedCategory.Content.ToString();

            // Se a caixa de pesquisa estiver vazia, recarregar todos os dados
            if (string.IsNullOrEmpty(searchText))
            {
                if (category == "Entrada/Saída")
                {
                    CarregarEntradas();
                    CarregarSaidas();
                }
                else if (category == "Histórico")
                {
                    CarregarHistorico();
                }
                return;
            }

            // Aplicar filtro baseado na categoria selecionada
            if (category == "Entrada/Saída")
            {
                FiltrarMovimentacoesPorTexto(searchText);
            }
            else if (category == "Histórico")
            {
                FiltrarHistoricoPorTexto(searchText);
            }
        }

        private void FiltrarMovimentacoesPorTexto(string searchText)
        {
            try
            {
                if (DatabaseConnect.Database == null)
                {
                    MessageBox.Show("Erro ao conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var collection = DatabaseConnect.Database.GetCollection<MovimentacaoData>("movimentacoes");
                var movimentacoes = collection.FindAll().ToList();

                // Entradas
                // 1. Filtrar entradas
                var entradasFiltradas = movimentacoes.Where(m =>
                    m.Tipo == "Entrada" && (
                        (m.ProdutoId?.ToLower().Contains(searchText) ?? false) ||
                        (m.Quantidade.ToString().Contains(searchText))
                    )).ToList();

                // 2. Reordena os produtos filtrados com prioridade mais clara
                entradasFiltradas = entradasFiltradas
                    .OrderBy(m => m.ProdutoId?.ToLower().StartsWith(searchText) == true ? 0 : 1) // Prioriza correspondências no início do ProdutoId
                    .ThenBy(m => m.ProdutoId?.ToLower().Contains(searchText) == true ? 0 : 1)    // Depois prioriza qualquer correspondência no ProdutoId
                    .ThenBy(m => m.ProdutoId?.ToLower().IndexOf(searchText) ?? int.MaxValue)     // Depois por posição no ProdutoId
                    .ThenBy(m => m.Quantidade.ToString().Contains(searchText) ? 0 : 1)           // Por último, correspondência na quantidade
                    .ThenBy(m => m.ProdutoId)                                                    // Ordenação alfabética como critério final
                    .ToList();

                // Saídas
                // 1. Filtrar saídas
                var saidasFiltradas = movimentacoes.Where(m =>
                    m.Tipo == "Saída" && (
                        (m.ProdutoId?.ToLower().Contains(searchText) ?? false) ||
                        (m.Quantidade.ToString().Contains(searchText))
                    )).ToList();

                // 2. Reordena os produtos filtrados com prioridade mais clara
                saidasFiltradas = saidasFiltradas
                    .OrderBy(m => m.ProdutoId?.ToLower().StartsWith(searchText) == true ? 0 : 1) // Prioriza correspondências no início do ProdutoId
                    .ThenBy(m => m.ProdutoId?.ToLower().Contains(searchText) == true ? 0 : 1)    // Depois prioriza qualquer correspondência no ProdutoId
                    .ThenBy(m => m.ProdutoId?.ToLower().IndexOf(searchText) ?? int.MaxValue)     // Depois por posição no ProdutoId
                    .ThenBy(m => m.Quantidade.ToString().Contains(searchText) ? 0 : 1)           // Por último, correspondência na quantidade
                    .ThenBy(m => m.ProdutoId)                                                    // Ordenação alfabética como critério final
                    .ToList();

                // Atualizar DataGrids
                EntradaDataGrid.ItemsSource = entradasFiltradas;
                SaidaDataGrid.ItemsSource = saidasFiltradas;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao filtrar movimentações: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FiltrarHistoricoPorTexto(string searchText)
        {
            try
            {
                if (DatabaseConnect.Database == null)
                {
                    MessageBox.Show("Erro ao conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var collection = DatabaseConnect.Database.GetCollection<LogData>("historico");
                var historico = collection.FindAll().ToList();

                // 1. Filtrar histórico
                var historicoFiltrado = historico.Where(h =>
                    (h.Tipo?.ToLower().Contains(searchText) ?? false) ||
                    (h.Nivel?.ToLower().Contains(searchText) ?? false) ||
                    (h.Usuario?.ToLower().Contains(searchText) ?? false)
                ).ToList();

                // 2. Reordena os produtos filtrados com prioridade mais clara
                historicoFiltrado = historicoFiltrado
                    .OrderBy(h => h.Tipo?.ToLower().StartsWith(searchText) == true ? 0 : 1) // Prioriza correspondências no início do Tipo
                    .ThenBy(h => h.Tipo?.ToLower().Contains(searchText) == true ? 0 : 1)    // Depois prioriza qualquer correspondência no Tipo
                    .ThenBy(h => h.Tipo?.ToLower().IndexOf(searchText) ?? int.MaxValue)     // Depois por posição no Tipo
                    .ThenBy(h => h.Nivel?.ToLower().Contains(searchText) ?? false ? 0 : 1)   // Por último, correspondência no Nível
                    .ThenBy(h => h.Usuario?.ToLower().Contains(searchText) ?? false ? 0 : 1) // E finalmente, correspondência no Usuário
                    .ThenBy(h => h.Data)                                                    // Ordenação por data como critério final
                    .ToList();

                // Atualizar DataGrid
                HistoricoDataGrid.ItemsSource = historicoFiltrado;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao filtrar histórico: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

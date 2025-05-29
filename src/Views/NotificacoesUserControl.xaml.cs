using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using WMS_RadiadoresLemos_WPF.src.Services;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class NotificacoesUserControl : UserControl
    {
        private List<AlertaData> alertas;

        // Evento para notificar o MainWindow sobre novas notificações
        public static event Action<AlertaData>? NovaNotificacaoAdicionada;

        public NotificacoesUserControl()
        {
            InitializeComponent();
            CarregarNotificacoes();
            CarregarFiltros();
        }

        // Método para carregar todas as notificações
        private void CarregarNotificacoes()
        {
            alertas = Alerta.Alertas.Values.SelectMany(lista => lista).ToList();

            if (AlertaDataGrid != null)
            {
                AlertaDataGrid.ItemsSource = alertas;
            }
        }

        // Método para carregar os filtros
        private void CarregarFiltros()
        {
            CarregarDadosComboBoxes();
        }

        // Método para carregar dados nos ComboBoxes
        private void CarregarDadosComboBoxes()
        {
            TipoComboBox.ItemsSource = alertas.Select(a => a.Tipo).Distinct().ToList();
        }

        // Método chamado ao clicar no botão de aplicar filtro
        private void AplicarFiltroButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string tipo = TipoComboBox.SelectedItem?.ToString();
            DateTime? dataInicio = DataInicioHistoricoPicker.SelectedDate;
            DateTime? dataFim = DataFimHistoricoPicker.SelectedDate;

            AplicarFiltro(tipo, dataInicio, dataFim);
            FiltroPopup.IsOpen = false;
        }

        // Método para aplicar os filtros na tabela de notificações
        private void AplicarFiltro(string tipo, DateTime? dataInicio, DateTime? dataFim)
        {
            try
            {
                var alertasFiltrados = alertas.Where(a =>
                    (string.IsNullOrEmpty(tipo) || a.Tipo.Equals(tipo, StringComparison.OrdinalIgnoreCase)) &&
                    (!dataInicio.HasValue || DateTime.Parse(a.Data).Date >= dataInicio.Value.Date) &&
                    (!dataFim.HasValue || DateTime.Parse(a.Data).Date <= dataFim.Value.Date)).ToList();

                AlertaDataGrid.ItemsSource = alertasFiltrados;
            }
            catch (Exception ex)
            {
                // Log ou mensagem de erro
                //MessageBox.Show($"Erro ao aplicar filtro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Evento para limpar os filtros
        private void LimparFiltroButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            TipoComboBox.SelectedItem = null;
            DataInicioHistoricoPicker.SelectedDate = null;
            DataFimHistoricoPicker.SelectedDate = null;

            // Recarregar todas as notificações
            AlertaDataGrid.ItemsSource = alertas;
            FiltroPopup.IsOpen = false;
        }

        // Método chamado ao clicar no botão de filtrar
        private void FiltrarButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            FiltroPopup.IsOpen = true;
        }

        private void AlertaDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}

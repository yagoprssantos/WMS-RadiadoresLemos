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
            alertas = new List<AlertaData>();

            // Para cada tipo de alerta, carregar os dados
            foreach (var tipo in Alerta.Alertas.Keys)
            {
                alertas.AddRange(Alerta.ObterAlertas(tipo));
            }

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
            DataComboBox.ItemsSource = alertas.Select(a => DateTime.Parse(a.Data).ToString("dd/MM/yyyy")).Distinct().ToList();
        }

        // Método para adicionar uma nova notificação
        public void AdicionarNovaNotificacao(AlertaData alerta)
        {
            alertas.Add(alerta);

            // Atualiza a tabela de notificações
            if (AlertaDataGrid != null)
            {
                AlertaDataGrid.ItemsSource = null;
                AlertaDataGrid.ItemsSource = alertas;
            }

            // Dispara o evento para notificar o MainWindow
            NovaNotificacaoAdicionada?.Invoke(alerta);
        }

        // Método chamado ao clicar no botão de aplicar filtro
        private void AplicarFiltroButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string tipo = TipoComboBox.SelectedItem?.ToString();
            string data = DataComboBox.SelectedItem?.ToString();

            AplicarFiltro(tipo, data);
            FiltroPopup.IsOpen = false;
        }

        // Método para aplicar os filtros na tabela de notificações
        private void AplicarFiltro(string tipo, string data)
        {
            try
            {
                var alertasFiltrados = alertas.Where(a =>
                    (string.IsNullOrEmpty(tipo) || a.Tipo == tipo) &&
                    (string.IsNullOrEmpty(data) || DateTime.Parse(a.Data).ToString("dd/MM/yyyy") == data)).ToList();

                AlertaDataGrid.ItemsSource = alertasFiltrados;
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Erro ao aplicar filtro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Evento para limpar os filtros
        private void LimparFiltroButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            TipoComboBox.SelectedItem = null;
            DataComboBox.SelectedItem = null;

            // Recarregar todas as notificações
            AlertaDataGrid.ItemsSource = alertas;
            FiltroPopup.IsOpen = false;
        }

        // Método chamado ao clicar no botão de filtrar
        private void FiltrarButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            FiltroPopup.IsOpen = true;
        }
    }
}

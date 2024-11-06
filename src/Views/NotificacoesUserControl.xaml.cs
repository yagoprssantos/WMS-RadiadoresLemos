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
        public NotificacoesUserControl()
        {
            InitializeComponent();
            CarregarNotificacoes();
        }

        // Método para carregar todas as notificações
        private void CarregarNotificacoes()
        {
            CarregarHistorico();
            CarregarAlertas();
        }

        // Método para carregar dados no DataGrid de Histórico
        private async void CarregarHistorico()
        {
            var historico = await LogEventos.ObterLogsAsync();

            HistoricoDataGrid.ItemsSource = historico;
        }

        // Método para carregar dados no DataGrid de Alertas
        private void CarregarAlertas()
        {
            var alertas = new List<AlertaData>();

            // Para cada tipo de alerta, carregar os dados
            foreach (var tipo in AlertaCache.Alertas.Keys)
            {
                alertas.AddRange(AlertaCache.ObterAlertas(tipo));
            }

            AlertaDataGrid.ItemsSource = alertas;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class NotificacoesUserControl : UserControl
    {
        public NotificacoesUserControl()
        {
            InitializeComponent();
            CarregarHistorico();
            CarregarAlertas();
        }

        // Método para carregar dados fictícios no DataGrid de Histórico
        private void CarregarHistorico()
        {
            var historico = new List<HistoricoItem>
            {
                new HistoricoItem { Data = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy"), Mensagem = "Entrada de novos itens no estoque" },
                new HistoricoItem { Data = DateTime.Now.AddDays(-2).ToString("dd/MM/yyyy"), Mensagem = "Saída de itens para cliente" },
                new HistoricoItem { Data = DateTime.Now.AddDays(-3).ToString("dd/MM/yyyy"), Mensagem = "Ajuste de inventário" }
            };

            HistoricoDataGrid.ItemsSource = historico;
        }

        // Método para carregar dados fictícios no DataGrid de Alertas
        private void CarregarAlertas()
        {
            var alertas = new List<AlertaItem>
            {
                new AlertaItem { Data = DateTime.Now.ToString("dd/MM/yyyy"), Alerta = "Baixa no estoque", Detalhes = "Estoque abaixo do mínimo para item XYZ" },
                new AlertaItem { Data = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy"), Alerta = "Item vencido", Detalhes = "Produto ABC expirado" }
            };

            AlertaDataGrid.ItemsSource = alertas;
        }
    }

    // Classe para representar um item de histórico
    public class HistoricoItem
    {
        public string Data { get; set; }
        public string Mensagem { get; set; }
    }

    // Classe para representar um item de alerta
    public class AlertaItem
    {
        public string Data { get; set; }
        public string Alerta { get; set; }
        public string Detalhes { get; set; }
    }
}

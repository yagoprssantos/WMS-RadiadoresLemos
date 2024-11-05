using System;
using System.Collections.Generic;
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
        private void CarregarHistorico()
        {
            var historico = new List<NotificacaoData>();
            // Adicione aqui o código para carregar os dados reais de histórico

            HistoricoDataGrid.ItemsSource = historico;
        }

        // Método para carregar dados no DataGrid de Alertas
        private void CarregarAlertas()
        {
            var alertas = new List<NotificacaoData>();

            foreach (var mensagem in AlertaCache.ObterNotificacoes("Aviso"))
            {
                alertas.Add(new NotificacaoData { Data = DateTime.Now.ToString("dd/MM/yyyy"), Tipo = "Aviso", Detalhes = mensagem });
            }

            foreach (var mensagem in AlertaCache.ObterNotificacoes("Erro"))
            {
                alertas.Add(new NotificacaoData { Data = DateTime.Now.ToString("dd/MM/yyyy"), Tipo = "Erro", Detalhes = mensagem });
            }

            foreach (var mensagem in AlertaCache.ObterNotificacoes("Importante"))
            {
                alertas.Add(new NotificacaoData { Data = DateTime.Now.ToString("dd/MM/yyyy"), Tipo = "Crítica", Detalhes = mensagem });
            }

            AlertaDataGrid.ItemsSource = alertas;
        }
    }
}

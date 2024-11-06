namespace WMS_RadiadoresLemos_WPF.src.Models
{
    class NotificacaoData
    {
        public string Data { get; set; } // Data da notificação [Alerta e Histórico]
        public string Tipo { get; set; } // Tipo de notificação [Alerta e Histórico]
        public string Nivel { get; set; } // Nível de alteração [Histórico]
        public string Detalhes { get; set; } // Detalhes da notificação [Alerta e Histórico]
        public string Acoes { get; set; } // Ações a serem tomadas [Alerta]
        public string Usuario { get; set; } // Usuário responsável pela notificação [Histórico]
    }
}

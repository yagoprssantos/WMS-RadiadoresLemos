namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class AlertaData
    {
        public string Data { get; set; } // Data e hora da notificação
        public string Tipo { get; set; } // Tipo de notificação
        public string MensagemdoSistema { get; set; } // Mensagem do sistema
        public string Detalhes { get; set; } // Detalhes da notificação
        public string Acoes { get; set; } // Ações recomendadas
    }
}

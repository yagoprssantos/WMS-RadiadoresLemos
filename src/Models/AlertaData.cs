namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class AlertaData
    {
        public required string Data { get; set; }

        public required string Tipo { get; set; }
        // Tipo de notificação (ERRO, AVISO, INFORMATIVO)

        public required string Sistema { get; set; }
        // Mensagem automática de erro do sistema

        public required string Detalhes { get; set; }
        // Detalhes da notificação - possíveis causas, o que aconteceu

        public required string Acoes { get; set; }
        // Ações recomendadas - o que consideramos que deve ser feito
    }
}

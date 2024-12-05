namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class AlertaData
    {
        public required string Data { get; set; }
        // Data e hora da notificação

        public required string Tipo { get; set; }
        // Tipo de notificação

        public required string Sistema { get; set; }
        // Sistema que gerou a notificação

        public required string Detalhes { get; set; }
        // Detalhes da notificação - possíveis causas, o que aconteceu

        public required string Acoes { get; set; }
        // Ações recomendadas - o que consideramos que deve ser feito
    }
}

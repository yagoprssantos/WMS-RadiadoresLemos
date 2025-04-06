using LiteDB;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class LogData
    {
        [BsonField("data")]
        public required DateTime Data { get; set; }
        // Data e hora da alteração

        [BsonField("tipo")]
        public required string Tipo { get; set; }
        // OPERACIONAL, RESTRITIVA, CRÍTICA

        [BsonField("nivel")]
        public required string Nivel { get; set; }
        // Cargo do usuário

        [BsonField("detalhes")]
        public required string Detalhes { get; set; }
        // Qual foi a alteração propriamente dita

        [BsonField("usuario")]
        public required string Usuario { get; set; }
        // Nome do usuário que fez a alteração

        [BsonId]
        public int Id { get; set; }

        // DataFormatada1 é uma propriedade que retorna a data e hora formatada, removendo a formatação gringa
        public string DataFormatada1
        {
            get
            {
                TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
                DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(Data, timeZone);
                return localTime.ToString("dd/MM/yyyy HH:mm:ss");
            }
        }

        public string DataFormatada2
        {
            get
            {
                TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
                DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(Data, timeZone);
                return localTime.ToString("dd/MM HH:mm:ss");
            }
        }
    }
}

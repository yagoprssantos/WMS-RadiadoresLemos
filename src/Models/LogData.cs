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

        public string DataFormatadaSemAno => Data.ToString("dd/MM HH:mm:ss");
        public string DataFormatadaComAno => Data.ToString("dd/MM/yyyy HH:mm:ss");
    }
}

using LiteDB;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class MovimentacaoData
    {
        [BsonField("produtoId")]
        public required string ProdutoId { get; set; }
        // Id do produto que foi movimentado

        [BsonField("tipo")]
        public required string Tipo { get; set; }
        // Tipo da movimentação (Entrada ou Saída)

        [BsonField("preco")]
        public required double Preco { get; set; }
        // Valor unitário do produto movimentado

        [BsonField("quantidade")]
        public required int Quantidade { get; set; }

        [BsonField("data")]
        public required DateTime Data { get; set; }

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
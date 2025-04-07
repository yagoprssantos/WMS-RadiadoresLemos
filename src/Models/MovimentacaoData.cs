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

        public string DataFormatadaSemAno => Data.ToString("dd/MM HH:mm:ss");
        public string DataFormatadaComAno => Data.ToString("dd/MM/yyyy HH:mm:ss");
    }
}
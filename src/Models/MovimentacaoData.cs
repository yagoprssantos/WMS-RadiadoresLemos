using LiteDB;
using System;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class MovimentacaoData
    {
        [BsonId]
        public int Id { get; set; }

        [BsonField("produtoId")]
        public required string ProdutoId { get; set; }

        [BsonField("produtoNome")]
        public string ProdutoNome { get; set; } = string.Empty;

        [BsonField("tipo")]
        public required string Tipo { get; set; } // "Entrada" ou "Saída"

        [BsonField("preco")]
        public required double Preco { get; set; }

        [BsonField("quantidade")]
        public required int Quantidade { get; set; }

        [BsonField("data")]
        public required DateTime Data { get; set; }

        [BsonField("detalhes")]
        public string? Detalhes { get; set; }

        // Relacionamento reverso (opcional, para facilitar consultas)
        [BsonField("compraId")]
        public Guid? CompraId { get; set; }

        [BsonField("vendaId")]
        public Guid? VendaId { get; set; }

        public string DataFormatadaSemAno => Data.ToString("dd/MM HH:mm:ss");
        public string DataFormatadaComAno => Data.ToString("dd/MM/yyyy HH:mm:ss");
    }
}
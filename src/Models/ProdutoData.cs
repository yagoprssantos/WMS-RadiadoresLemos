using LiteDB;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class ProdutoData
    {
        [BsonField("nome")]
        public required string Nome { get; set; }

        [BsonField("tipo")]
        public required string Tipo { get; set; }

        [BsonField("marca")]
        public required string Marca { get; set; }

        [BsonField("codigo")]
        public required string Codigo { get; set; }

        [BsonField("preco")]
        public double Preco { get; set; }

        [BsonField("quantidade")]
        public int Quantidade { get; set; }

        [BsonId]
        public string Id { get; set; } // Identificador único do documento

        public ProdutoData()
        {
            // Inicializa com string vazia para evitar null
            Id = string.Empty;
            Nome = string.Empty;
            Tipo = string.Empty;
            Marca = string.Empty;
            Codigo = string.Empty;
        }
    }
}

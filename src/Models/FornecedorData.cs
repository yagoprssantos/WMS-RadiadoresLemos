using System.Collections.Generic;
using LiteDB;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class FornecedorData
    {
        [BsonId]
        public required string Id { get; set; }

        [BsonField("nome")]
        public required string Nome { get; set; }

        [BsonField("cnpj")]
        public required string CNPJ { get; set; }

        [BsonField("estado")]
        public required string Estado { get; set; }

        [BsonField("compras")]
        public List<string> ComprasRelacionadas { get; set; } = new List<string>();

        public FornecedorData()
        {
            Id = string.Empty; // Inicialização temporária
        }

        public void SetIdFromCNPJ()
        {
            Id = CNPJ;
        }
    }
}
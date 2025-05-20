using System.Collections.Generic;
using LiteDB;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class ClienteData
    {
        [BsonId]
        public required string Id { get; set; }

        [BsonField("email")]
        public required string Email { get; set; }

        [BsonField("telefone")]
        public required string Telefone { get; set; }

        [BsonField("cnpj")]
        public required string CNPJ { get; set; }

        [BsonField("estado")]
        public required string Estado { get; set; }

        [BsonField("vendas")]
        public List<string> VendasRelacionadas { get; set; } = new List<string>();

        public ClienteData()
        {
            Id = string.Empty; // Inicialização temporária
        }

        public void SetIdFromCNPJ()
        {
            Id = CNPJ;
        }
    }
}
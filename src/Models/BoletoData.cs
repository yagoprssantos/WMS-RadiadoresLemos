using LiteDB;
using System;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class BoletoData
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;

        // Caminho do arquivo do boleto na pasta
        [BsonField("caminhoArquivo")]
        public string CaminhoArquivo { get; set; }

        // Nota Fiscal associada à compra/venda
        [BsonField("notaFiscal")]
        public string? NotaFiscal { get; set; }

        // Referência ao fornecedor
        [BsonField("fornecedorId")]
        public string FornecedorId { get; set; } = string.Empty;

        // Data de vencimento do boleto
        [BsonField("vencimento")]
        public DateTime Vencimento { get; set; }

        [BsonField("pagamento")]
        public DateTime? Pagamento { get; set; }

        // Número da parcela
        [BsonField("parcela")]
        public int Parcela { get; set; }


        public void SetIdFromNotaFiscal()
        {
            Id = NotaFiscal;
        }
    }
}
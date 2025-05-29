using LiteDB;
using System;
using System.Collections.Generic;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class CompraData
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;

        [BsonField("fornecedorId")]
        public string FornecedorId { get; set; } = string.Empty; // Referência ao Fornecedor

        [BsonField("fornecedorNome")]
        public string FornecedorNome { get; set; } = string.Empty; // Nome para exibição rápida

        [BsonField("dataCompra")]
        public DateTime DataCompra { get; set; } = DateTime.Now;

        [BsonField("dataPagamento")]
        public DateTime? DataPagamento { get; set; }

        [BsonField("tipoPagamento")]
        public string TipoPagamento { get; set; } = string.Empty; // "À vista" ou "Parcelado"

        [BsonField("parcelas")]
        public int Parcelas { get; set; }

        [BsonField("boletos")]
        public List<string>? Boletos { get; set; }

        [BsonField("notaFiscal")]
        public string? NotaFiscal { get; set; }

        [BsonField("itens")]
        public List<MovimentacaoData> Itens { get; set; } = new();

        [BsonField("valorTotal")]
        public decimal ValorTotal { get; set; }

        [BsonField("detalhes")]
        public string? Detalhes { get; set; }

        public void SetIdFromNotaFiscal()
        {
            Id = NotaFiscal;
        }
    }
}



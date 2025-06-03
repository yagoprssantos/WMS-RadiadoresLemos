using LiteDB;
using System;
using System.Collections.Generic;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class VendaData
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;

        [BsonField("clienteId")]
        public string ClienteId { get; set; } = string.Empty; // Referência ao Cliente

        [BsonField("clienteCNPJ")]
        public string ClienteCNPJ { get; set; } = string.Empty; // CNPJ para exibição rápida

        [BsonField("pedido")]
        public string Pedido { get; set; } = string.Empty;

        [BsonField("dataCompra")]
        public DateTime DataCompra { get; set; } = DateTime.Now;

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

        [BsonField("dataCadastro")]
        public DateTime DataCadastro { get; set; } = DateTime.Now;

        [BsonField("detalhes")]
        public string? Detalhes { get; set; }

        public void SetIdFromNotaFiscal()
        {
            Id = NotaFiscal;
        }
    }
}
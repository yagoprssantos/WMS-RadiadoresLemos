using System;
using System.Collections.Generic;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class VendaData
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ClienteId { get; set; } = string.Empty; // Referência ao Cliente
        public string ClienteCNPJ { get; set; } = string.Empty; // CNPJ para exibição rápida
        public string Pedido { get; set; } = string.Empty;
        public DateTime DataCompra { get; set; } = DateTime.Now;
        public DateTime? DataPagamento { get; set; }
        public string TipoPagamento { get; set; } = string.Empty; // "À vista" ou "Parcelado"
        public int Parcelas { get; set; }
        public List<string>? Boletos { get; set; }
        public string? NotaFiscal { get; set; }
        public List<MovimentacaoData> Itens { get; set; } = new();
        public decimal ValorTotal { get; set; }
        public DateTime DataCadastro { get; set; } = DateTime.Now;
        public string? Detalhes { get; set; }
    }
}
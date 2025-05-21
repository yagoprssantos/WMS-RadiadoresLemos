using System;
using System.Collections.Generic;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class CompraData
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FornecedorId { get; set; } = string.Empty; // Referência ao Fornecedor
        public string FornecedorNome { get; set; } = string.Empty; // Nome para exibição rápida
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



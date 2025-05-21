using System;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class MovimentacaoListItem
    {
        public string ProdutoId { get; set; } = string.Empty;
        public string ProdutoNome { get; set; } = string.Empty;
        public string? FornecedorId { get; set; }
        public string? ClienteId { get; set; }
        public int Quantidade { get; set; }
        public double Preco { get; set; }
        public string? FormaPagamento { get; set; }
        public int Parcelas { get; set; }
        public string Detalhes { get; set; } = string.Empty;
        public DateTime Data { get; set; }
        public MovimentacaoData MovimentacaoData { get; set; } = null!;
    }
}
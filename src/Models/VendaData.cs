using System;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class VendaData
    {
        public Guid Id { get; set; }
        public required string Cliente { get; set; }
        public required string Pedido { get; set; }
        public required string Produto { get; set; } // Talvez transformar em lista
        public DateTime DataCompra { get; set; }
        public DateTime DataPagamento { get; set; }
        public decimal ValorTotal { get; set; } // Soma dos valores dos produtos
        public DateTime DataCadastro { get; set; } // Data em que a venda foi registrada
        public required string TipoPagamento { get; set; } // "À vista" ou "Parcelado"
        public int Parcelas { get; set; } // Número de parcelas, se aplicável
        public required string Boletos { get; set; } // Exemplo: link ou identificador (?)
        public required string Movimentacao { get; set; } // Exemplo: link ou identificador (?)
        public required string NotaFiscal { get; set; } // Exemplo: link ou identificador (?)

        public VendaData()
        {
            Id = Guid.NewGuid();
            DataCadastro = DateTime.Now;
            Cliente = string.Empty;
            Pedido = string.Empty;
            Produto = string.Empty;
            TipoPagamento = string.Empty;
            Boletos = string.Empty;
            Movimentacao = string.Empty;
            NotaFiscal = string.Empty;
        }
    }
}
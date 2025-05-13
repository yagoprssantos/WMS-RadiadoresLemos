using System;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class Venda
    {
        public Guid Id { get; set; }
        public string Cliente { get; set; }
        public string Pedido { get; set; }
        public string Produto { get; set; } // Talvez transformar em lista
        public DateTime DataCompra { get; set; }
        public DateTime DataPagamento { get; set; }
        public decimal ValorTotal { get; set; } // Soma dos valores dos produtos
        public DateTime DataCadastro { get; set; } // Data em que a venda foi registrada
        public string TipoPagamento { get; set; } // "À vista" ou "Parcelado"
        public int Parcelas { get; set; } // Número de parcelas, se aplicável
        public string Boletos { get; set; } // Exemplo: link ou identificador (?)
        public string Movimentacao { get; set; } // Exemplo: link ou identificador (?)
        public string NotaFiscal { get; set; } // Exemplo: link ou identificador (?)

        public Venda()
        {
            Id = Guid.NewGuid();
            DataCadastro = DateTime.Now;
        }
    }
}
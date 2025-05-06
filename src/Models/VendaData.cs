using System;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class Venda
    {
        public Guid Id { get; set; }
        public string Cliente { get; set; }
        public string Pedido { get; set; }
        public string Produto { get; set; } // Adicionado para permitir registrar o produto
        public DateTime DataCompra { get; set; }
        public DateTime DataPagamento { get; set; } // Adicionado para concordar com o formulário
        public decimal ValorTotal { get; set; }
        public DateTime DataCadastro { get; set; }

        public Venda()
        {
            Id = Guid.NewGuid();
            DataCadastro = DateTime.Now;
        }
    }
}
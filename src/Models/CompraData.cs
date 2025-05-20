using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class CompraData
    {
        public Guid Id { get; set; }
        public string Fornecedor { get; set; }
        public string Produto { get; set; }
        public DateTime DataCompra { get; set; }
        public DateTime DataPagamento { get; set; }
        public decimal ValorTotal { get; set; }
        public DateTime DataCadastro { get; set; }
        public string TipoPagamento { get; set; }
        public int Parcelas { get; set; }
        public string Boletos { get; set; }
        public string Movimentacao { get; set; }
        public string NotaFiscal { get; set; }

        public CompraData()
        {
            Id = Guid.NewGuid();
            DataCadastro = DateTime.Now;
        }
    }
}



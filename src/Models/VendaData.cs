using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class Venda
    {
        public string Cliente { get; set; }
        public string Pedido { get; set; }
        public DateTime DataCompra { get; set; }
        public decimal ValorTotal { get; set; }
    }
}

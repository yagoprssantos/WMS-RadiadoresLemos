using System;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class BoletoViewModel
    {
        // Propriedades originais
        public string Id { get; set; }
        public string Parcela { get; set; }
        public DateTime Vencimento { get; set; }
        public DateTime? Pagamento { get; set; }
        public string CaminhoArquivo { get; set; }
        public string NotaFiscal { get; set; }
        public string FornecedorId { get; set; }

        // Propriedades estendidas
        public string VencimentoFormatado { get; set; }
        public string PagamentoFormatado { get; set; }
        public string NomeArquivo { get; set; }
        public string NomeFornecedor { get; set; }
        public bool ArquivoExiste { get; set; }
        public string Status { get; set; }
        public string SituacaoVencimento { get; set; }
        public string CorStatus { get; set; }

        // Referência ao objeto original
        public BoletoData Original { get; set; }
    }

    public class ProdutoViewModel
    {
        public string Nome { get; set; }
        public int Quantidade { get; set; }
        public double PrecoUnitario { get; set; }
        public double Subtotal => Quantidade * PrecoUnitario;
    }

    public class ItemEdicaoViewModel
    {
        private MovimentacaoData _item;

        public ItemEdicaoViewModel(MovimentacaoData item)
        {
            _item = item;
        }

        public string ProdutoNome { get => _item.ProdutoNome; set => _item.ProdutoNome = value; }
        public int Quantidade { get => _item.Quantidade; set => _item.Quantidade = value; }
        public double Preco { get => _item.Preco; set => _item.Preco = value; }
        public double Subtotal => Quantidade * Preco;

        public MovimentacaoData ObterItem() => _item;
    }
}
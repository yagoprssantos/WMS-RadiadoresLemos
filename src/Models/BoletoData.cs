using LiteDB;
using System;
using System.ComponentModel.DataAnnotations;

namespace WMS_RadiadoresLemos_WPF.src.Models
{

    public class BoletoData
    {
        // Dados do boleto salvos no Banco de Dados
        [BsonId]
        public string Id { get; set; }

        // Nome do arquivo do boleto na pasta
        [BsonField("nomeArquivo")]
        public string? NomeArquivo { get; set; }

        // Caminho do arquivo do boleto na pasta
        [BsonField("caminhoArquivo")]
        public string? CaminhoArquivo { get; set; }

        // Nota Fiscal associada à compra/venda
        [BsonField("notaFiscal")]
        public string? NotaFiscal { get; set; }

        // Referência ao fornecedor
        [BsonField("fornecedorId")]
        public string FornecedorId { get; set; } = string.Empty;

        // Data de vencimento do boleto
        [BsonField("dataVencimento")]
        public DateTime DataVencimento { get; set; }

        [BsonField("dataPagamento")]
        public DateTime? DataPagamento { get; set; } = null;

        // Número da parcela
        [BsonField("parcela")]
        public int Parcela { get; set; }

        // Valor do boleto
        [BsonField("valor")]
        public decimal Valor { get; set; }

        public void SetIdFromNome()
        {
            Id = NomeArquivo;
        }

        // Dados do boleto não salvos no Banco de Dados

        // Beneficiario (Fornecedor) -> Pode ser utilizado para identificar o fornecedor do boleto
        // e confirmar se o fornecedor existe, podendo ser criado um novo fornecedor se necessário.
        public string Beneficiario { get; set; } = string.Empty; // Fornecedor
        public string? CnpjBeneficiario { get; set; } // Fornecedor
        public string? CepBeneficiario { get; set; } // Fornecedor
        public string? EstadoBeneficiario { get; set; } // Fornecedor
        public string? AgenciaCodigoBeneficiario { get; set; }


        // Pagador -> É sempre a empresa Radiadores Lemos, então pode ser usado para confirmar que
        // o boleto é realmente da empresa Radiadores Lemos.
        public string Pagador { get; set; } = string.Empty;
        public string? CnpjPagador { get; set; } = string.Empty;


        // Status do boleto -> Pendente, Pago, Vencido, Cancelado, Processando
        public StatusBoleto Status { get; set; } = StatusBoleto.Pendente;







        public string LinhaDigitavel { get; set; } = string.Empty;
        public string? NossoNumero { get; set; }
        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
        public string? UsuarioCadastro { get; set; }
        public string? Observacoes { get; set; }
    }


    public enum StatusBoleto
    {
        Pendente = 0,
        Pago = 1,
        Vencido = 2,
        Cancelado = 3,
        Processando = 4
    }
}
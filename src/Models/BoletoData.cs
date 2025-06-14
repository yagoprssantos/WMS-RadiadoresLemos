using LiteDB;
using System;
using System.ComponentModel.DataAnnotations;

namespace WMS_RadiadoresLemos_WPF.src.Models
{

    public class BoletoData
    {
        [BsonId]
        public string Id { get; set; }

        [Required(ErrorMessage = "O nome do beneficiário é obrigatório")]
        [StringLength(200, ErrorMessage = "O nome do beneficiário deve ter no máximo 200 caracteres")]
        public string Beneficiario { get; set; } = string.Empty;

        [StringLength(18, ErrorMessage = "CNPJ deve ter no máximo 18 caracteres")]
        public string? CnpjBeneficiario { get; set; }

        [StringLength(10, ErrorMessage = "CEP deve ter no máximo 10 caracteres")]
        public string? CepBeneficiario { get; set; }

        [StringLength(2, ErrorMessage = "Estado deve ter 2 caracteres")]
        public string? EstadoBeneficiario { get; set; }

        [Required(ErrorMessage = "O nome do pagador é obrigatório")]
        [StringLength(200, ErrorMessage = "O nome do pagador deve ter no máximo 200 caracteres")]
        public string Pagador { get; set; } = string.Empty;

        [Required(ErrorMessage = "A data de vencimento é obrigatória")]
        public DateTime DataVencimento { get; set; }

        [Required(ErrorMessage = "O valor é obrigatório")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal Valor { get; set; }

        [Required(ErrorMessage = "A linha digitável é obrigatória")]
        [StringLength(100, ErrorMessage = "Linha digitável deve ter no máximo 100 caracteres")]
        public string LinhaDigitavel { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Nosso número deve ter no máximo 50 caracteres")]
        public string? NossoNumero { get; set; }

        [StringLength(50, ErrorMessage = "Agência/Código deve ter no máximo 50 caracteres")]
        public string? AgenciaCodigoBeneficiario { get; set; }

        [Required]
        public StatusBoleto Status { get; set; } = StatusBoleto.Pendente;

        public DateTime? DataPagamento { get; set; }

        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

        [StringLength(100, ErrorMessage = "Nome do usuário deve ter no máximo 100 caracteres")]
        public string? UsuarioCadastro { get; set; }

        [StringLength(500, ErrorMessage = "Observações devem ter no máximo 500 caracteres")]
        public string? Observacoes { get; set; }

        public int? CompraId { get; set; }

        public int? VendaId { get; set; }

        [StringLength(500, ErrorMessage = "Caminho do arquivo deve ter no máximo 500 caracteres")]
        public string? CaminhoArquivo { get; set; }

        [StringLength(100, ErrorMessage = "ID do fornecedor deve ter no máximo 100 caracteres")]
        public string? FornecedorId { get; set; }

        [StringLength(50, ErrorMessage = "Nota fiscal deve ter no máximo 50 caracteres")]
        public string? NotaFiscal { get; set; }

        // Nome do arquivo do boleto na pasta
        [BsonField("nomeArquivo")]
        public string? NomeArquivo { get; set; }

        // Nota Fiscal associada à compra/venda
        [BsonField("notaFiscal")]
        public string? NotaFiscalBson
        {
            get => NotaFiscal;
            set => NotaFiscal = value;
        }

        [BsonField("pagamento")]
        public DateTime? Pagamento { get; set; } = null;

        // Número da parcela
        [BsonField("parcela")]
        public int Parcela { get; set; }

        public void SetIdFromNome()
        {
            Id = NomeArquivo;
        }
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
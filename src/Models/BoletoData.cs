using System;
using System.ComponentModel.DataAnnotations;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    /// <summary>
    /// Modelo de dados para representar um boleto bancário no sistema
    /// </summary>
    public class BoletoData
    {
        /// <summary>
        /// Identificador único do boleto
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome completo ou razão social do beneficiário
        /// </summary>
        [Required(ErrorMessage = "O nome do beneficiário é obrigatório")]
        [StringLength(200, ErrorMessage = "O nome do beneficiário deve ter no máximo 200 caracteres")]
        public string Beneficiario { get; set; } = string.Empty;


        /// <summary>
        /// CNPJ do beneficiário
        /// </summary>
        [StringLength(18, ErrorMessage = "CNPJ deve ter no máximo 18 caracteres")]
        public string? CnpjBeneficiario { get; set; }

        /// <summary>
        /// CEP do endereço do beneficiário
        /// </summary>
        [StringLength(10, ErrorMessage = "CEP deve ter no máximo 10 caracteres")]
        public string? CepBeneficiario { get; set; }

        /// <summary>
        /// Estado (UF) do beneficiário
        /// </summary>
        [StringLength(2, ErrorMessage = "Estado deve ter 2 caracteres")]
        public string? EstadoBeneficiario { get; set; }

        /// <summary>
        /// Nome completo ou razão social do pagador
        /// </summary>
        [Required(ErrorMessage = "O nome do pagador é obrigatório")]
        [StringLength(200, ErrorMessage = "O nome do pagador deve ter no máximo 200 caracteres")]
        public string Pagador { get; set; } = string.Empty;

        /// <summary>
        /// Data de vencimento do boleto
        /// </summary>
        [Required(ErrorMessage = "A data de vencimento é obrigatória")]
        public DateTime DataVencimento { get; set; }

        /// <summary>
        /// Valor do boleto em reais
        /// </summary>
        [Required(ErrorMessage = "O valor é obrigatório")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal Valor { get; set; }

        /// <summary>
        /// Linha digitável do boleto
        /// </summary>
        [Required(ErrorMessage = "A linha digitável é obrigatória")]
        [StringLength(100, ErrorMessage = "Linha digitável deve ter no máximo 100 caracteres")]
        public string LinhaDigitavel { get; set; } = string.Empty;

        /// <summary>
        /// Nosso número do boleto
        /// </summary>
        [StringLength(50, ErrorMessage = "Nosso número deve ter no máximo 50 caracteres")]
        public string? NossoNumero { get; set; }

        /// <summary>
        /// Agência e código do beneficiário
        /// </summary>
        [StringLength(50, ErrorMessage = "Agência/Código deve ter no máximo 50 caracteres")]
        public string? AgenciaCodigoBeneficiario { get; set; }

        /// <summary>
        /// Status atual do boleto
        /// </summary>
        [Required]
        public StatusBoleto Status { get; set; } = StatusBoleto.Pendente;

        /// <summary>
        /// Data de pagamento (se pago)
        /// </summary>
        public DateTime? DataPagamento { get; set; }

        /// <summary>
        /// Data de cadastro do boleto no sistema
        /// </summary>
        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Usuário que cadastrou o boleto
        /// </summary>
        [StringLength(100, ErrorMessage = "Nome do usuário deve ter no máximo 100 caracteres")]
        public string? UsuarioCadastro { get; set; }

        /// <summary>
        /// Observações adicionais sobre o boleto
        /// </summary>
        [StringLength(500, ErrorMessage = "Observações devem ter no máximo 500 caracteres")]
        public string? Observacoes { get; set; }

        /// <summary>
        /// Referência para compra relacionada (se aplicável)
        /// </summary>
        public int? CompraId { get; set; }

        /// <summary>
        /// Referência para venda relacionada (se aplicável)
        /// </summary>
        public int? VendaId { get; set; }

        /// <summary>
        /// Caminho do arquivo original do boleto (se salvo)
        /// </summary>
        [StringLength(500, ErrorMessage = "Caminho do arquivo deve ter no máximo 500 caracteres")]
        public string? CaminhoArquivo { get; set; }

        // 👈 ADICIONANDO AS PROPRIEDADES QUE FALTAM:

        /// <summary>
        /// ID do fornecedor relacionado ao boleto
        /// </summary>
        [StringLength(100, ErrorMessage = "ID do fornecedor deve ter no máximo 100 caracteres")]
        public string? FornecedorId { get; set; }

        /// <summary>
        /// Número da nota fiscal relacionada
        /// </summary>
        [StringLength(50, ErrorMessage = "Nota fiscal deve ter no máximo 50 caracteres")]

        // Nome do arquivo do boleto na pasta
        [BsonField("nomeArquivo")]
        public string? NomeArquivo { get; set; }

        // Nota Fiscal associada à compra/venda
        [BsonField("notaFiscal")]

        public string? NotaFiscal { get; set; }

        /// <summary>
        /// Número da parcela (para controle interno)
        /// </summary>
        public int Parcela { get; set; } = 1;
    }

    /// <summary>
    /// Enum para representar o status do boleto
    /// </summary>
    public enum StatusBoleto
    {
        /// <summary>
        /// Boleto pendente de pagamento
        /// </summary>
        Pendente = 0,

        /// <summary>
        /// Boleto pago
        /// </summary>
        Pago = 1,


        /// <summary>
        /// Boleto vencido
        /// </summary>
        Vencido = 2,

        [BsonField("pagamento")]
        public DateTime? Pagamento { get; set; } = null;

        // Número da parcela
        [BsonField("parcela")]
        public int Parcela { get; set; }


        /// <summary>
        /// Boleto cancelado
        /// </summary>
        Cancelado = 3,


        /// <summary>
        /// Boleto em processamento
        /// </summary>
        Processando = 4

        public void SetIdFromNome()
        {
            Id = NomeArquivo;
        }

    }
}
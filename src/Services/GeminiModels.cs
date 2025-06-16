using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    // Classe que representa a resposta da API Gemini
    public class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate> Candidates { get; set; }
    }

    // Candidato de resposta da API Gemini
    public class Candidate
    {
        [JsonPropertyName("content")]
        public Content Content { get; set; }
    }

    // Conteúdo da resposta da API Gemini
    public class Content
    {
        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }
    }

    // Parte do conteúdo da resposta da API Gemini
    public class Part
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    // Esquema para formatação das respostas da API Gemini
    public class GeminiSchema
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("properties")]
        public Dictionary<string, GeminiProperty> Properties { get; set; }
    }

    // Propriedade do esquema da API Gemini
    public class GeminiProperty
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    // Dados extraídos de um boleto pela API Gemini
    public class BoletoExtraidoData
    {
        [JsonPropertyName("beneficiario")]
        public string Beneficiario { get; set; }

        [JsonPropertyName("cnpjBeneficiario")]
        public string CnpjBeneficiario { get; set; }

        [JsonPropertyName("cepBeneficiario")]
        public string CepBeneficiario { get; set; }

        [JsonPropertyName("estadoBeneficiario")]
        public string EstadoBeneficiario { get; set; }

        [JsonPropertyName("pagador")]
        public string Pagador { get; set; }

        [JsonPropertyName("vencimento")]
        public string Vencimento { get; set; }

        [JsonPropertyName("valor")]
        public string Valor { get; set; }

        [JsonPropertyName("linhaDigitavel")]
        public string LinhaDigitavel { get; set; }

        [JsonPropertyName("nossoNumero")]
        public string NossoNumero { get; set; }

        [JsonPropertyName("agenciaCodigoBeneficiario")]
        public string AgenciaCodigoBeneficiario { get; set; }
    }
}

using System;
using System.Collections.Generic;
using LiteDB;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class NotaData
    {
        [BsonId]
        public string Id { get; set; } // Chave da nota ou número

        // Identificação da NF-e
        public string NumeroNota { get; set; }
        public DateTime DataEmissao { get; set; }
        public string NaturezaOperacao { get; set; }
 

        // Emitente
        public string EmitenteCNPJ { get; set; }
        public string EmitenteNome { get; set; }
        public string EmitenteEndereco { get; set; }
        public string EmitenteBairro { get; set; }
        public string EmitenteMunicipio { get; set; }
        public string EmitenteUF { get; set; }
        public string EmitenteCEP { get; set; }

        // Destinatário
        public string DestinatarioCNPJ { get; set; }
        public string DestinatarioNome { get; set; }
    }
} 
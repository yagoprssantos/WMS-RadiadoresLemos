using System;
using System.Collections.Generic;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    internal static class AlertaCache
    {
        public static Dictionary<string, List<NotificacaoData>> Alertas { get; set; } = new Dictionary<string, List<NotificacaoData>>()
        {
            { "Importante", new List<NotificacaoData>() },
            { "Erro", new List<NotificacaoData>() },
            { "Aviso", new List<NotificacaoData>() }
        };

        public static void AdicionarAlerta(string tipo, string mensagem, string acoes)
        {
            if (Alertas.ContainsKey(tipo))
            {
                Alertas[tipo].Add(new NotificacaoData
                {
                    Data = DateTime.Now.ToString("dd/MM/yyyy"),
                    Tipo = tipo,
                    Detalhes = mensagem,
                    Acoes = acoes, // Pode ser preenchido conforme necessário
                    Usuario = null, // Não é relevante para alertas
                    Nivel = null // Não é relevante para alertas
                });
            }
            else
            {
                throw new ArgumentException("Tipo de alerta inválido");
            }
        }

        public static List<NotificacaoData> ObterAlertas(string tipo)
        {
            if (Alertas.ContainsKey(tipo))
            {
                return Alertas[tipo];
            }
            else
            {
                throw new ArgumentException("Tipo de alerta inválido");
            }
        }
    }
}

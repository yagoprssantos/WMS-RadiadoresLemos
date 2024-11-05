using System;
using System.Collections.Generic;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    internal static class AlertaCache
    {
        public static Dictionary<string, List<string>> Notificacoes { get; set; } = new Dictionary<string, List<string>>()
        {
            { "Importante", new List<string>() },
            { "Erro", new List<string>() },
            { "Aviso", new List<string>() }
        };

        public static void AdicionarNotificacao(string tipo, string mensagem)
        {
            if (Notificacoes.ContainsKey(tipo))
            {
                Notificacoes[tipo].Add(mensagem);
            }
            else
            {
                throw new ArgumentException("Tipo de notificação inválido");
            }
        }

        public static List<string> ObterNotificacoes(string tipo)
        {
            if (Notificacoes.ContainsKey(tipo))
            {
                return Notificacoes[tipo];
            }
            else
            {
                throw new ArgumentException("Tipo de notificação inválido");
            }
        }
    }
}

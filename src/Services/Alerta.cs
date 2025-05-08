using System;
using System.Collections.Generic;
using System.Linq;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    internal static class Alerta
    {
        public static Dictionary<string, List<AlertaData>> Alertas { get; set; } = new Dictionary<string, List<AlertaData>>()
        {
            { "Importante", new List<AlertaData>() },
            { "Erro", new List<AlertaData>() },
            { "Aviso", new List<AlertaData>() }
        };

        // Contagem de novas notificações
        private static int _novasNotificacoes = 0;

        // Evento para notificar mudanças na contagem de novas notificações
        public static event Action<int>? ContagemAlterada;

        public static void AdicionarAlerta(string tipo, string sysmsg, string mensagem, string acoes)
        {
            if (Alertas.ContainsKey(tipo))
            {
                var novoAlerta = new AlertaData
                {
                    Data = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                    Tipo = tipo,
                    Sistema = sysmsg,
                    Detalhes = mensagem,
                    Acoes = acoes
                };

                Alertas[tipo].Add(novoAlerta);

                // Incrementa a contagem de novas notificações
                _novasNotificacoes++;

                // Dispara o evento para notificar a mudança na contagem de novas notificações
                ContagemAlterada?.Invoke(_novasNotificacoes);
            }
            else
            {
                throw new ArgumentException("Tipo de alerta inválido");
            }
        }

        public static void ResetarNovasNotificacoes()
        {
            _novasNotificacoes = 0;

            // Dispara o evento para atualizar a contagem
            ContagemAlterada?.Invoke(_novasNotificacoes);
        }
    }
}

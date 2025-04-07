using System;
using System.Collections.Generic;
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

        // Evento para notificar quando um novo alerta é adicionado
        public static event Action<AlertaData>? AlertaAdicionado;

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
                    Acoes = acoes // Pode ser preenchido conforme necessário
                };

                Alertas[tipo].Add(novoAlerta);

                // Disparar o evento quando um novo alerta é adicionado
                AlertaAdicionado?.Invoke(novoAlerta);
            }
            else
            {
                throw new ArgumentException("Tipo de alerta inválido");
            }
        }

        public static List<AlertaData> ObterAlertas(string tipo)
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

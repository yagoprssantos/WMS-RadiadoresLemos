using System;
using System.Collections.Generic;
using System.Linq;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public static class OrganizarBoleto
    {
        /// <summary>
        /// Organiza boletos por data de vencimento
        /// </summary>
        public static List<BoletoData> OrganizarPorVencimento(List<BoletoData> boletos)
        {
            return boletos.OrderBy(b => b.DataVencimento).ToList(); // 👈 CORRIGIDO: DataVencimento
        }

        /// <summary>
        /// Filtra boletos vencidos
        /// </summary>
        public static List<BoletoData> FiltrarVencidos(List<BoletoData> boletos)
        {
            return boletos.Where(b => b.DataVencimento < DateTime.Now).ToList(); // 👈 CORRIGIDO: DataVencimento
        }

        /// <summary>
        /// Filtra boletos por status
        /// </summary>
        public static List<BoletoData> FiltrarPorStatus(List<BoletoData> boletos, StatusBoleto status)
        {
            return boletos.Where(b => b.Status == status).ToList();
        }

        /// <summary>
        /// Filtra boletos por período
        /// </summary>
        public static List<BoletoData> FiltrarPorPeriodo(List<BoletoData> boletos, DateTime dataInicio, DateTime dataFim)
        {
            return boletos.Where(b => b.DataVencimento >= dataInicio && b.DataVencimento <= dataFim).ToList();
        }

        /// <summary>
        /// Organiza boletos por valor
        /// </summary>
        public static List<BoletoData> OrganizarPorValor(List<BoletoData> boletos, bool crescente = true)
        {
            return crescente
                ? boletos.OrderBy(b => b.Valor).ToList()
                : boletos.OrderByDescending(b => b.Valor).ToList();
        }

        /// <summary>
        /// Organiza boletos por beneficiário
        /// </summary>
        public static List<BoletoData> OrganizarPorBeneficiario(List<BoletoData> boletos)
        {
            return boletos.OrderBy(b => b.Beneficiario).ToList();
        }

        /// <summary>
        /// Calcula total de valores por status
        /// </summary>
        public static Dictionary<StatusBoleto, decimal> CalcularTotalPorStatus(List<BoletoData> boletos)
        {
            return boletos.GroupBy(b => b.Status)
                         .ToDictionary(g => g.Key, g => g.Sum(b => b.Valor));
        }
    }
}
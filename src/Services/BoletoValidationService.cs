using System;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public static class BoletoValidationService
    {
        private const string CNPJ_ESPERADO = "38.046.801/0001-60";
        private const string CNPJ_ESPERADO_LIMPO = "38046801000160";

        /// <summary>
        /// Valida se o CNPJ do pagador é exatamente o CNPJ da empresa Radiadores Lemos
        /// </summary>
        /// <param name="cnpjPagador">CNPJ do pagador a ser validado</param>
        /// <param name="numeroParcela">Número da parcela para mensagens de erro</param>
        /// <returns>True se o CNPJ é válido, false caso contrário</returns>
        public static bool ValidarCnpjPagador(string? cnpjPagador, int numeroParcela = 0)
        {
            if (string.IsNullOrWhiteSpace(cnpjPagador))
            {
                string mensagem = numeroParcela > 0 
                    ? $"CNPJ do pagador não foi informado no boleto da parcela {numeroParcela}!\n\nPor favor, preencha o CNPJ do pagador para continuar."
                    : "CNPJ do pagador não foi informado!\n\nPor favor, preencha o CNPJ do pagador para continuar.";
                
                System.Windows.MessageBox.Show(mensagem, "CNPJ Obrigatório", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return false;
            }

            string cnpjLimpo = cnpjPagador.Replace(".", "").Replace("/", "").Replace("-", "");
            
            if (cnpjLimpo != CNPJ_ESPERADO_LIMPO)
            {
                string mensagem = numeroParcela > 0
                    ? $"CNPJ do pagador inválido no boleto da parcela {numeroParcela}!\n\nCNPJ encontrado: {cnpjPagador}\nCNPJ esperado: {CNPJ_ESPERADO}\n\nPor favor, verifique se o boleto é realmente da empresa Radiadores Lemos."
                    : $"CNPJ do pagador inválido!\n\nCNPJ encontrado: {cnpjPagador}\nCNPJ esperado: {CNPJ_ESPERADO}\n\nPor favor, verifique se o boleto é realmente da empresa Radiadores Lemos.";
                
                System.Windows.MessageBox.Show(mensagem, "CNPJ Inválido", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Valida se o CNPJ do pagador está contido no texto do pagador (para casos onde não há campo específico)
        /// </summary>
        /// <param name="pagador">Texto do pagador que pode conter o CNPJ</param>
        /// <returns>True se o CNPJ está presente, false caso contrário</returns>
        public static bool ValidarCnpjPagadorNoTexto(string? pagador)
        {
            if (string.IsNullOrWhiteSpace(pagador))
            {
                System.Windows.MessageBox.Show("Pagador não foi informado!\n\nPor favor, preencha o pagador para continuar.", 
                    "Pagador Obrigatório", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return false;
            }

            if (!pagador.Contains(CNPJ_ESPERADO))
            {
                System.Windows.MessageBox.Show(
                    $"CNPJ do pagador inválido!\n\nPagador informado: {pagador}\nCNPJ esperado: {CNPJ_ESPERADO}\n\nPor favor, verifique se o boleto é realmente da empresa Radiadores Lemos.",
                    "CNPJ Inválido", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Obtém o CNPJ esperado da empresa Radiadores Lemos
        /// </summary>
        /// <returns>CNPJ formatado da empresa</returns>
        public static string GetCnpjEsperado()
        {
            return CNPJ_ESPERADO;
        }

        /// <summary>
        /// Obtém o CNPJ esperado da empresa Radiadores Lemos (sem formatação)
        /// </summary>
        /// <returns>CNPJ sem formatação da empresa</returns>
        public static string GetCnpjEsperadoLimpo()
        {
            return CNPJ_ESPERADO_LIMPO;
        }
    }
} 
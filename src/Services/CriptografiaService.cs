using System;
using System.Security.Cryptography;
using System.Text;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public static class CriptografiaService
    {
        public static string CriptografarSenha(string senha)
        {
            if (string.IsNullOrEmpty(senha))
                throw new ArgumentException("A senha não pode ser vazia", nameof(senha));

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(senha);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public static bool VerificarSenha(string senha, string hash)
        {
            if (string.IsNullOrEmpty(senha))
                throw new ArgumentException("A senha não pode ser vazia", nameof(senha));
            
            if (string.IsNullOrEmpty(hash))
                throw new ArgumentException("O hash não pode ser vazio", nameof(hash));

            string senhaHash = CriptografarSenha(senha);
            return senhaHash == hash;
        }
    }
} 
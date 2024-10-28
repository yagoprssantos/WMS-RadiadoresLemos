using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class Usuario
    {
        // Propriedades do usuário
        public int Id { get; set; } // Identificador único do usuário
        public string Nome { get; set; } // Nome do usuário
        public string Email { get; set; } // Email do usuário
        public string Permissao { get; set; } // Permissão do usuário (Admin, Usuário, Convidado)

        // Construtor padrão
        public Usuario() { }

        // Construtor com parâmetros
        public Usuario(int id, string nome, string email, string permissao)
        {
            Id = id;
            Nome = nome;
            Email = email;
            Permissao = permissao;
        }

        // Sobrescrevendo o método ToString para exibir o nome do usuário
        public override string ToString()
        {
            return $"{Nome} ({Permissao})";
        }

    }
}

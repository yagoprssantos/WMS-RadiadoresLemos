using LiteDB;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class UsuarioData
    {
        [BsonField("nome")]
        public required string Nome { get; set; }

        [BsonField("email")]
        public required string Email { get; set; }

        [BsonField("matricula")]
        public required string Matricula { get; set; }

        [BsonField("senha")]
        public required string Senha { get; set; }

        [BsonField("cargo")]
        public required string Cargo { get; set; }
        // Tipos de cargo:
        // 1. Administrador 
        // 2. Usuario 
        // 3. Moderador 

        [BsonId]
        public string Id { get; set; } // Identificador único do documento

        // Define Id como a matrícula do usuário SEMPRE que o objeto for criado
        public UsuarioData()
        {
            Id = Matricula;
        }
    }
}

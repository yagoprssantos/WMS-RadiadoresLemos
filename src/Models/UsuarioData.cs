using Google.Cloud.Firestore;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    [FirestoreData]
    public class UsuarioData
    {
        [FirestoreProperty]
        public required string Nome { get; set; }
        [FirestoreProperty]

        public required string Email { get; set; }
        [FirestoreProperty]

        public required string Matrícula { get; set; }
        [FirestoreProperty]

        public required string Senha { get; set; }
        [FirestoreProperty]

        public required string Cargo { get; set; }
        // Tipos de cargo:
        // 1. Administrador - Acesso total ao sistema, voltado para a equipe de desenvolvimento
        // 2. Gerente - Acesso total ao sistema, operacional e administrativo
        // 3. Operador - Acesso restrito ao sistema, funções específicas e básicas para operação
        // 4. Estagiário - Acesso restrito ao sistema, apenas para aprendizado e treinamento

        [FirestoreDocumentId]
        public string Id { get; set; } // Identificador único do documento

    }
}

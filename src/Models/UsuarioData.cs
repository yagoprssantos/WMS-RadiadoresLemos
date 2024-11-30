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


        [FirestoreDocumentId]
        public string Id { get; set; } // Identificador único do documento

    }
}

using Google.Cloud.Firestore;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    [FirestoreData]
    public class ProdutoData
    {
        [FirestoreProperty]
        public required string Nome { get; set; }

        [FirestoreProperty]
        public required string Tipo { get; set; }

        [FirestoreProperty]
        public required string Marca { get; set; }

        [FirestoreProperty]
        public required string Codigo { get; set; }

        [FirestoreProperty]
        public double Preço { get; set; }

        [FirestoreProperty]
        public int Quantidade { get; set; }


        [FirestoreDocumentId]
        public string Id { get; set; } // Identificador único do documento
    }
}

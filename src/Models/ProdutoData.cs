using Google.Cloud.Firestore;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    // Aqui estão armazenados os modelos de dados que serão utilizados para a comunicação com o banco de dados Firestore
    // Cada classe representa um tipo de dado que será armazenado no banco de dados, como um produto ou um usuário
    // Dessa forma, é possível recuperar os dados do banco de dados e armazená-los em objetos dessas classes, o que
    // diminui o uso de requisições ao banco de dados e aumenta a eficiência do programa

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
        public int Quantidade { get; set; }


        [FirestoreDocumentId]
        public string Id { get; set; } // Identificador único do documento
    }

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

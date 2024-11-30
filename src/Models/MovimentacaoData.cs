using Google.Cloud.Firestore;

[FirestoreData]
public class MovimentacaoData
{
    [FirestoreProperty]
    public required string ProdutoId { get; set; }
    // Id do produto que foi movimentado

    [FirestoreProperty]
    public required string Tipo { get; set; }
    // Tipo da movimentação (Entrada ou Saída)

    [FirestoreProperty]
    public required double Preço { get; set; }
    // Valor unitário do produto movimentado

    [FirestoreProperty]
    public required int Quantidade { get; set; }
    // Quantidade movimentada

    [FirestoreProperty]
    public required DateTime DataHora { get; set; }
    // Data e hora da movimentação
}
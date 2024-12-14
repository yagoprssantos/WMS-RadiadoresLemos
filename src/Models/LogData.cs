using Google.Cloud.Firestore;

[FirestoreData]
public class LogData
{
    [FirestoreProperty]
    public required DateTime Data { get; set; }
    // Data e hora da alteração

    [FirestoreProperty]
    public required string Tipo { get; set; }
    // OPERACIONAL, RESTRITIVA, CRÍTICA

    [FirestoreProperty]
    public required string Nivel { get; set; }
    // Cargo do usuário

    [FirestoreProperty]
    public required string Detalhes { get; set; }
    // Qual foi a alteração propriamente dita

    [FirestoreProperty]
    public required string Usuario { get; set; }
    // Nome do usuário que fez a alteração

    public string DataFormatada
    {
        get
        {
            return Data.ToString("dd/MM/yyyy HH:mm:ss");
        }
    }
}

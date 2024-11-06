using Google.Cloud.Firestore;

[FirestoreData]
public class LogData
{
    [FirestoreProperty]
    public DateTime Data { get; set; }
    [FirestoreProperty]
    public string Tipo { get; set; }
    // OPERACIONAL, RESTRITIVA, CRÍTICA

    [FirestoreProperty]
    public string Nivel { get; set; }
    // Cargo do usuário

    [FirestoreProperty]
    public string Detalhes { get; set; }
    // Qual foi a alteração propriamente dita

    [FirestoreProperty]
    public string Usuario { get; set; }
}

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

    [FirestoreProperty]
    public required DateTime Data { get; set; }


    // DataFormatada1 é uma propriedade que retorna a data e hora formatada, removendo a formatação gringa
    public string DataFormatada1
    {
        get
        {
            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(Data, timeZone);
            return localTime.ToString("dd/MM/yyyy HH:mm:ss");
        }
    }

    public string DataFormatada2
    {
        get
        {
            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(Data, timeZone);
            return localTime.ToString("dd/MM HH:mm:ss");
        }
    }
}
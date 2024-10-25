using Google.Cloud.Firestore;
using System;
using System.Threading.Tasks;

namespace WMS_RadiadoresLemos_WPF.Classes
{
    public static class LogEventos
    {
        private static FirestoreDb db = DatabaseConnect.Database;

        // Função para registrar um evento no Firestore
        public static async Task RegistrarEventoAsync(string descricao)
        {
            if (string.IsNullOrWhiteSpace(descricao))
            {
                Console.WriteLine("Descrição do evento não pode ser vazia.");
                return;
            }

            if (db == null)
            {
                Console.WriteLine("Conexão com o Firestore não estabelecida.");
                return;
            }

            try
            {
                var eventosRef = db.Collection("Eventos");
                var evento = new
                {
                    Descricao = descricao,
                    DataHora = DateTime.UtcNow  // Armazena a data/hora do evento
                };
                await eventosRef.AddAsync(evento);
                Console.WriteLine("Evento registrado com sucesso.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao registrar evento: {ex.Message}");
            }
        }
    }
}

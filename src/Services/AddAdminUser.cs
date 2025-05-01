using System;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public static class AddAdminUser
    {
        public static void AddAdmin()
        {
            try
            {
                // Garante que o banco de dados seja inicializado
                DatabaseConnect.SetEnvironmentVariable();

                if (DatabaseConnect.Database == null)
                {
                    throw new Exception("Não foi possível conectar ao banco de dados");
                }

                var collection = DatabaseConnect.Database.GetCollection<UsuarioData>("usuarios");
                
                // Verifica se já existe um administrador
                var adminExists = collection.Exists(u => u.Cargo == "Administrador");
                if (!adminExists)
                {
                    var adminUser = new UsuarioData
                    {
                        Id = Guid.NewGuid().ToString(),
                        Nome = "admin",
                        Email = "admin@radiadoreslemos.com",
                        Matricula = "ADM",
                        Senha = CriptografiaService.CriptografarSenha("admin"), // Senha inicial que deve ser alterada
                        Cargo = "Administrador"
                    };

                    collection.Insert(adminUser);
                    Console.WriteLine("Usuário administrador adicionado com sucesso!");
                }
                else
                {
                    Console.WriteLine("Já existe um usuário administrador no sistema.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao adicionar usuário administrador: {ex.Message}");
                throw; // Propaga a exceção para que a aplicação saiba que houve um erro
            }
        }
    }
} 
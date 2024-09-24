using System;
using System.Data;
using System.Data.SQLite;

public class BancoDeDados
{
    private string connectionString;

    public BancoDeDados()
    {
        // Definir o caminho do banco de dados (um arquivo .db local)
        connectionString = "Data Source=estoque.db;Version=3;";

        // Criar a tabela de produtos se ainda não existir
        CriarTabela();
    }

    // Método para criar a tabela de produtos
    private void CriarTabela()
    {
        using (SQLiteConnection conn = new SQLiteConnection(connectionString))
        {
            conn.Open();

            // SQL para criar a tabela de produtos
            string sql = @"
                CREATE TABLE IF NOT EXISTS Produtos (
                    codigo INTEGER PRIMARY KEY,
                    nome TEXT NOT NULL,
                    marca TEXT NOT NULL,
                    tipo TEXT NOT NULL
                );
            ";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.ExecuteNonQuery();  // Executar o comando para criar a tabela
            }
        }
    }

    // Método para inserir um novo produto
    public void InserirProduto(int codigo, string nome, string marca, string tipo)
    {
        using (SQLiteConnection conn = new SQLiteConnection(connectionString))
        {
            conn.Open();

            // Verificar se o código já existe
            string verificarSql = "SELECT COUNT(1) FROM Produtos WHERE codigo = @codigo";
            using (SQLiteCommand verificarCmd = new SQLiteCommand(verificarSql, conn))
            {
                verificarCmd.Parameters.AddWithValue("@codigo", codigo);
                int existe = Convert.ToInt32(verificarCmd.ExecuteScalar());

                if (existe > 0)
                {
                    throw new Exception("Já existe um produto com este código.");
                }
            }

            // SQL para inserir um novo produto
            string sql = "INSERT INTO Produtos (codigo, nome, marca, tipo) VALUES (@codigo, @nome, @marca, @tipo)";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@codigo", codigo);
                cmd.Parameters.AddWithValue("@nome", nome);
                cmd.Parameters.AddWithValue("@marca", marca);
                cmd.Parameters.AddWithValue("@tipo", tipo);

                cmd.ExecuteNonQuery();  // Executar o comando de inserção
            }
        }
    }

    // Método para listar todos os produtos
    public DataTable ListarProdutos()
    {
        using (SQLiteConnection conn = new SQLiteConnection("Data Source=estoque.db;Version=3;"))
        {
            conn.Open();
            string query = "SELECT codigo, nome, marca, tipo FROM Produtos";

            using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
            {
                SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;

            }
        }
    }


}

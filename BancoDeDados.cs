using System.Data.SQLite;
using System.Data;
using System.IO;

public class BancoDeDados
{
    private string connectionString;
    private DataTable produtosDataTable = new DataTable();

    public BancoDeDados()
    {
        // Definir o caminho do banco de dados (um arquivo .db local)
        connectionString = "Data Source=estoque.db;Version=3;";

        // Criar a tabela de produtos
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
                tipo TEXT NOT NULL,
                quantidade INTEGER NOT NULL
            );
        ";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.ExecuteNonQuery();  // Executar o comando para criar a tabela
            }
        }
    }

    // Método para verificar a estrutura da tabela
    public void VerificarEstruturaTabela()
    {
        using (SQLiteConnection conn = new SQLiteConnection(connectionString))
        {
            conn.Open();
            string sql = "PRAGMA table_info(Produtos);";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine($"{reader["name"]} - {reader["type"]}");
                    }
                }
            }
        }
    }

    // Método para excluir o banco de dados
    public void ExcluirBancoDeDados()
    {
        if (File.Exists("estoque.db"))
        {
            File.Delete("estoque.db");
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

            // SQL para inserir um novo produto com quantidade zero
            string sql = "INSERT INTO Produtos (codigo, nome, marca, tipo, quantidade) VALUES (@codigo, @nome, @marca, @tipo, 0)";

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
            string query = "SELECT codigo, nome, marca, tipo, quantidade FROM Produtos";

            using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
            {
                SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }
    }

    // Método para pesquisar produtos
    public DataTable PesquisarProduto(string termo)
    {
        using (SQLiteConnection conn = new SQLiteConnection(connectionString))
        {
            conn.Open();
            string searchQuery = "SELECT * FROM Produtos WHERE nome LIKE @Termo OR marca LIKE @Termo OR tipo LIKE @Termo OR quantidade LIKE @Termo";
            using (SQLiteCommand cmd = new SQLiteCommand(searchQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Termo", "%" + termo + "%");
                using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }
    }

    public void AdicionarProdutoAoEstoque(int codigoProduto, int quantidade)
    {
        using (SQLiteConnection conn = new SQLiteConnection(connectionString))
        {
            conn.Open();
            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Produtos SET quantidade = quantidade + @Quantidade WHERE codigo = @Codigo", conn))
            {
                cmd.Parameters.AddWithValue("@Quantidade", quantidade);
                cmd.Parameters.AddWithValue("@Codigo", codigoProduto);
                cmd.ExecuteNonQuery();
            }
        }
    }
}

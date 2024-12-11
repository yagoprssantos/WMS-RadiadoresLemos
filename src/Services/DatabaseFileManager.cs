using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

public class DatabaseFileManager
{
    private FirestoreDb? _firestoreDb;

    // Pasta onde os arquivos JSON serão salvos
    private const string DiretorioArquivos = "DadosBancoDeDadosOffline";

    // Caminhos dos arquivos JSON
    public string CaminhoArquivoUsuarios { get; private set; } = Path.Combine(DiretorioArquivos, "usuarios.json");
    public string CaminhoArquivoProdutos { get; private set; } = Path.Combine(DiretorioArquivos, "produtos.json");
    public string CaminhoArquivoLogs { get; private set; } = Path.Combine(DiretorioArquivos, "logs.json");
    public string CaminhoArquivoMovimentacoes { get; private set; } = Path.Combine(DiretorioArquivos, "movimentacoes.json");

    public DatabaseFileManager()
    {
        // Configura a variável de ambiente para a conexão com o banco de dados
        DatabaseConnect.SetEnvironmentVarible();
        if (DatabaseConnect.Database != null)
        {
            _firestoreDb = DatabaseConnect.Database;

            // Cria o diretório se ele não existir
            if (!Directory.Exists(DiretorioArquivos))
            {
                Directory.CreateDirectory(DiretorioArquivos);
            }
        }
    }

    // Função para inicializar os arquivos locais com os dados do banco de dados
    public async Task InicializarArquivosAsync()
    {
        try
        {
            // Inicializa os arquivos locais com dados do banco de dados, se ainda não existirem
            if (!File.Exists(CaminhoArquivoUsuarios))
            {
                // Obtém os dados do banco de dados e salva em um arquivo JSON
                List<UsuarioData> usuarios = await ObterColecaoFirebaseDB<UsuarioData>("usuarios");
                await SalvarNoArquivoAsync(CaminhoArquivoUsuarios, usuarios);
            }
            if (!File.Exists(CaminhoArquivoProdutos))
            {
                List<ProdutoData> produtos = await ObterColecaoFirebaseDB<ProdutoData>("produtos");
                await SalvarNoArquivoAsync(CaminhoArquivoProdutos, produtos);
            }

            if (!File.Exists(CaminhoArquivoLogs))
            {
                List<LogData> logs = await ObterColecaoFirebaseDB<LogData>("logs");
                await SalvarNoArquivoAsync(CaminhoArquivoLogs, logs);
            }

            if (!File.Exists(CaminhoArquivoMovimentacoes))
            {
                List<MovimentacaoData> movimentacoes = await ObterColecaoFirebaseDB<MovimentacaoData>("movimentacoes");
                await SalvarNoArquivoAsync(CaminhoArquivoMovimentacoes, movimentacoes);
            }
        }
        catch (Exception ex)
        {
            // Log de erro
            Console.WriteLine($"Erro ao inicializar arquivos: {ex.Message}");
        }
    }

    // Função para atualizar os arquivos locais com os dados mais recentes do banco de dados
    public async Task AtualizarArquivosAsync()
    {
        try
        {
            // Atualiza os arquivos locais com os dados mais recentes do banco de dados
            if (_firestoreDb != null)
            {
                // Se houver conexão com o banco de dados, reescreve os arquivos locais atualizando-os
                List<UsuarioData> usuarios = await ObterColecaoFirebaseDB<UsuarioData>("Usuarios");
                await SalvarNoArquivoAsync(CaminhoArquivoUsuarios, usuarios);

                List<ProdutoData> produtos = await ObterColecaoFirebaseDB<ProdutoData>("Produtos");
                await SalvarNoArquivoAsync(CaminhoArquivoProdutos, produtos);

                List<LogData> logs = await ObterColecaoFirebaseDB<LogData>("Historico");
                await SalvarNoArquivoAsync(CaminhoArquivoLogs, logs);

                List<MovimentacaoData> movimentacoes = await ObterColecaoFirebaseDB<MovimentacaoData>("Movimentacoes");
                await SalvarNoArquivoAsync(CaminhoArquivoMovimentacoes, movimentacoes);
            }
        }
        catch (Exception ex)
        {
            // Log de erro
            Console.WriteLine($"Erro ao atualizar arquivos: {ex.Message}");
        }
    }

    // Função para obter uma coleção de documentos do banco de dados Firestore
    public async Task<List<T>> ObterColecaoFirebaseDB<T>(string nomeColecao)
    {
        List<T> dados = new List<T>();

        if (_firestoreDb != null)
        {
            try
            {
                // Obtém uma coleção de documentos do banco de dados Firestore
                QuerySnapshot querySnapshot = await _firestoreDb.Collection(nomeColecao).GetSnapshotAsync();

                foreach (DocumentSnapshot documentSnapshot in querySnapshot.Documents)
                {
                    // Desserializa os documentos em objetos do tipo T
                    T dado = documentSnapshot.ConvertTo<T>();
                    dados.Add(dado);
                }
            }
            catch (Exception ex)
            {
                // Log de erro
                Console.WriteLine($"Erro ao obter coleção do banco de dados: {ex.Message}");
            }
        }

        return dados;
    }

    // Função para obter os dados de um arquivo JSON
    public async static Task<List<T>> ObterDadosDoArquivoAsync<T>(string caminhoArquivo)
    {
        try
        {
            // Lê o arquivo JSON
            string json = await File.ReadAllTextAsync(caminhoArquivo);

            // Desserializa o JSON em uma lista de objetos do tipo T
            List<T> dados = JsonSerializer.Deserialize<List<T>>(json);

            return dados;
        }
        catch (Exception ex)
        {
            // Log de erro
            Console.WriteLine($"Erro ao obter dados do arquivo {caminhoArquivo}: {ex.Message}");
            return new List<T>();
        }
    }

    // Funções de adicionar, remover e atualizar dados no banco de dados local
    public async static Task AdicionarDadoAsync<T>(string caminhoArquivo, T dado)
    {
        try
        {
            // Lê o arquivo JSON
            string json = await File.ReadAllTextAsync(caminhoArquivo);

            // Desserializa o JSON em uma lista de objetos do tipo T
            List<T> dados = JsonSerializer.Deserialize<List<T>>(json);

            // Adiciona o novo dado à lista
            dados.Add(dado);

            // Salva a lista atualizada no arquivo
            await SalvarNoArquivoAsync(caminhoArquivo, dados);
        }
        catch (Exception ex)
        {
            // Log de erro
            Console.WriteLine($"Erro ao adicionar dado ao arquivo {caminhoArquivo}: {ex.Message}");
        }
    }

    // Função para remover um dado do arquivo JSON
    public async Task RemoverDadoAsync<T>(string caminhoArquivo, T dado)
    {
        try
        {
            // Lê o arquivo JSON
            string json = await File.ReadAllTextAsync(caminhoArquivo);

            // Desserializa o JSON em uma lista de objetos do tipo T
            List<T> dados = JsonSerializer.Deserialize<List<T>>(json);

            // Remove o dado da lista
            dados.Remove(dado);

            // Salva a lista atualizada no arquivo
            await SalvarNoArquivoAsync(caminhoArquivo, dados);
        }
        catch (Exception ex)
        {
            // Log de erro
            Console.WriteLine($"Erro ao remover dado do arquivo {caminhoArquivo}: {ex.Message}");
        }
    }

    // Função para atualizar um dado no arquivo JSON
    public async Task AtualizarDadoAsync<T>(string caminhoArquivo, T dado)
    {
        try
        {
            // Lê o arquivo JSON
            string json = await File.ReadAllTextAsync(caminhoArquivo);

            // Desserializa o JSON em uma lista de objetos do tipo T
            List<T> dados = JsonSerializer.Deserialize<List<T>>(json);

            // Atualiza o dado na lista
            int index = dados.FindIndex(d => d.Equals(dado));
            dados[index] = dado;

            // Salva a lista atualizada no arquivo
            await SalvarNoArquivoAsync(caminhoArquivo, dados);
        }
        catch (Exception ex)
        {
            // Log de erro
            Console.WriteLine($"Erro ao atualizar dado no arquivo {caminhoArquivo}: {ex.Message}");
        }
    }

    // Função para salvar os dados em um arquivo JSON
    private async static Task SalvarNoArquivoAsync<T>(string caminhoArquivo, List<T> dados)
    {
        try
        {
            // Este método salva os dados em um arquivo JSON no caminho especificado
            // Primeiro converte os dados em JSON
            string json = JsonSerializer.Serialize(dados);

            // Depois, caso o arquivo tenha sido serializado corretamente, salva o JSON no arquivo
            if (!string.IsNullOrEmpty(json))
            {
                await File.WriteAllTextAsync(caminhoArquivo, json);
            }
            else
            {
                // Se o JSON estiver vazio, lança uma exceção
                throw new Exception("Erro ao salvar os dados no arquivo JSON.");
            }
        }
        catch (Exception ex)
        {
            // Log de erro
            Console.WriteLine($"Erro ao salvar no arquivo {caminhoArquivo}: {ex.Message}");
        }
    }
}

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

    // Função para salvar o cache nos arquivos JSON
    public async Task SalvarCacheNosArquivosAsync()
    {
        try
        {
            // Salva os dados do cache nos arquivos JSON
            if (DadosCache.Tabelas.ContainsKey("Usuarios"))
            {
                List<UsuarioData> usuarios = DadosCache.Tabelas["Usuarios"].Cast<UsuarioData>().ToList();
                await SalvarNoArquivoAsync(CaminhoArquivoUsuarios, usuarios);
            }

            if (DadosCache.Tabelas.ContainsKey("Produtos"))
            {
                List<ProdutoData> produtos = DadosCache.Tabelas["Produtos"].Cast<ProdutoData>().ToList();
                await SalvarNoArquivoAsync(CaminhoArquivoProdutos, produtos);
            }

            if (DadosCache.Tabelas.ContainsKey("Historico"))
            {
                List<LogData> logs = DadosCache.Tabelas["Historico"].Cast<LogData>().ToList();
                await SalvarNoArquivoAsync(CaminhoArquivoLogs, logs);
            }

            if (DadosCache.Tabelas.ContainsKey("Movimentacoes"))
            {
                List<MovimentacaoData> movimentacoes = DadosCache.Tabelas["Movimentacoes"].Cast<MovimentacaoData>().ToList();
                await SalvarNoArquivoAsync(CaminhoArquivoMovimentacoes, movimentacoes);
            }
        }
        catch (Exception ex)
        {
            // Log de erro
            Console.WriteLine($"Erro ao salvar cache nos arquivos: {ex.Message}");
        }
    }

}

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
        // PRECISA ESTAR CONECTADO COM A INTERNET PARA FUNCIONAR
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
                List<UsuarioData> usuarios = await ObterColecaoFirebaseDB<UsuarioData>("Usuarios");
                await SalvarNoArquivoAsync(CaminhoArquivoUsuarios, usuarios);
            }
            if (!File.Exists(CaminhoArquivoProdutos))
            {
                List<ProdutoData> produtos = await ObterColecaoFirebaseDB<ProdutoData>("Produtos");
                await SalvarNoArquivoAsync(CaminhoArquivoProdutos, produtos);
            }

            if (!File.Exists(CaminhoArquivoLogs))
            {
                List<LogData> logs = await ObterColecaoFirebaseDB<LogData>("Historico");
                await SalvarNoArquivoAsync(CaminhoArquivoLogs, logs);
            }

            if (!File.Exists(CaminhoArquivoMovimentacoes))
            {
                List<MovimentacaoData> movimentacoes = await ObterColecaoFirebaseDB<MovimentacaoData>("Movimentacoes");
                await SalvarNoArquivoAsync(CaminhoArquivoMovimentacoes, movimentacoes);
            }
        }
        catch (Exception ex)
        {
            // Log de erro
            Console.WriteLine($"Erro ao inicializar arquivos: {ex.Message}");
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

    // Função para atualizar os arquivos locais com os dados mais recentes do banco de dados
    public async Task AtualizarArquivosComBancoAsync()
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

    // Função para adicionar um item a uma tabela
    public async Task AtualizarValorItemAsync<T>(string tabela, string itemId, string propriedade, object novoValor) where T : class
    {
        try
        {
            // Obtém o caminho do arquivo JSON da tabela
            string caminhoArquivo = ObterCaminhoArquivo(tabela);

            // Lê os dados do arquivo JSON
            List<T> itens = await LerDoArquivoAsync<T>(caminhoArquivo);

            // Encontra o item pelo ID
            T? item = itens.FirstOrDefault(i => typeof(T).GetProperty("Id")?.GetValue(i)?.ToString() == itemId);
            if (item != null)
            {
                // Atualiza o valor da propriedade do item
                var propriedadeInfo = typeof(T).GetProperty(propriedade);
                if (propriedadeInfo != null && propriedadeInfo.CanWrite)
                {
                    propriedadeInfo.SetValue(item, Convert.ChangeType(novoValor, propriedadeInfo.PropertyType));

                    // Salva a lista de itens atualizada no arquivo JSON
                    await SalvarNoArquivoAsync(caminhoArquivo, itens);
                }
                else
                {
                    Console.WriteLine($"Propriedade {propriedade} não encontrada ou não pode ser escrita.");
                }
            }
            else
            {
                Console.WriteLine($"Item com ID {itemId} não encontrado.");
            }
        }
        catch (Exception ex)
        {
            // Log de erro
            Console.WriteLine($"Erro ao atualizar o valor do item: {ex.Message}");
        }
    }


    // Função para salvar os dados em um arquivo JSON
    public async static Task SalvarNoArquivoAsync<T>(string caminhoArquivo, List<T> dados)
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


    // Função para sincronizar dados com o banco, enviando os dados locais para o Firestore
    public async Task SincronizarDadosComBancoAsync()
    {
        try
        {
            // Sincroniza os dados locais com o banco de dados
            if (_firestoreDb != null)
            {
                // Se o arquivo existe, lê os dados do arquivo e sincroniza com o banco
                if (File.Exists(CaminhoArquivoUsuarios))
                {
                    List<UsuarioData> usuarios = await LerDoArquivoAsync<UsuarioData>(CaminhoArquivoUsuarios);
                    await SincronizarFirebaseDB(usuarios, "Usuarios");
                }

                if (File.Exists(CaminhoArquivoProdutos))
                {
                    List<ProdutoData> produtos = await LerDoArquivoAsync<ProdutoData>(CaminhoArquivoProdutos);
                    await SincronizarFirebaseDB(produtos, "Produtos");
                }

                if (File.Exists(CaminhoArquivoLogs))
                {
                    List<LogData> logs = await LerDoArquivoAsync<LogData>(CaminhoArquivoLogs);
                    await SincronizarFirebaseDB(logs, "Historico");
                }

                if (File.Exists(CaminhoArquivoMovimentacoes))
                {
                    List<MovimentacaoData> movimentacoes = await LerDoArquivoAsync<MovimentacaoData>(CaminhoArquivoMovimentacoes);
                    await SincronizarFirebaseDB(movimentacoes, "Movimentacoes");
                }
            }
        }
        catch (Exception ex)
        {
            // Log de erro
            Console.WriteLine($"Erro ao sincronizar dados com o banco: {ex.Message}");
        }
    }

    // Função para ler os dados de um arquivo JSON
    public async static Task<List<T>> LerDoArquivoAsync<T>(string caminhoArquivo)
    {
        List<T> dados = new List<T>();

        try
        {
            // Este método lê os dados de um arquivo JSON no caminho especificado
            // Primeiro lê o JSON do arquivo
            string json = await File.ReadAllTextAsync(caminhoArquivo);

            // Depois, caso o arquivo tenha sido lido corretamente, desserializa o JSON
            if (!string.IsNullOrEmpty(json))
            {
                dados = JsonSerializer.Deserialize<List<T>>(json);
            }
            else
            {
                // Se o JSON estiver vazio, lança uma exceção
                throw new Exception("Erro ao ler os dados do arquivo JSON.");
            }
        }
        catch (Exception ex)
        {
            // Log de erro
            Console.WriteLine($"Erro ao ler do arquivo {caminhoArquivo}: {ex.Message}");
        }

        return dados;
    }

    // Função para sincronizar uma coleção de documentos com o banco de dados Firestore
    public async Task SincronizarFirebaseDB<T>(List<T> dados, string tabela) where T : class
    {
        if (_firestoreDb != null)
        {
            try
            {
                // Deleta tabela do banco de dados
                QuerySnapshot querySnapshot = await _firestoreDb.Collection(tabela).GetSnapshotAsync();
                foreach (DocumentSnapshot documentSnapshot in querySnapshot.Documents)
                {
                    await documentSnapshot.Reference.DeleteAsync();
                }

                // Adiciona os novos documentos à coleção
                foreach (T dado in dados)
                {
                    // Verifica se o objeto possui a propriedade "Id"
                    var idProperty = typeof(T).GetProperty("Id");
                    if (idProperty != null)
                    {
                        string? id = idProperty.GetValue(dado)?.ToString();
                        if (!string.IsNullOrEmpty(id))
                        {
                            // Adiciona o documento com o ID especificado
                            DocumentReference docRef = _firestoreDb.Collection(tabela).Document(id);
                            await docRef.SetAsync(dado);
                        }
                    }
                    else
                    {
                        // Adiciona o documento com um ID gerado automaticamente
                        await _firestoreDb.Collection(tabela).AddAsync(dado);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log de erro
                Console.WriteLine($"Erro ao sincronizar coleção com o banco de dados: {ex.Message}");
            }
        }
    }

    // Função para retornar caminho do arquivo JSON de uma tabela
    public string ObterCaminhoArquivo(string tabela)
    {
        switch (tabela)
        {
            case "Usuarios":
                return CaminhoArquivoUsuarios;
            case "Produtos":
                return CaminhoArquivoProdutos;
            case "Historico":
                return CaminhoArquivoLogs;
            case "Movimentacoes":
                return CaminhoArquivoMovimentacoes;
            default:
                return "";
        }
    }
}

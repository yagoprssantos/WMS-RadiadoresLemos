using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ClosedXML.Excel;
using LiteDB;
using Microsoft.Win32;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class ExcelWindow : Window
    {
        private static readonly string[] TabelasDisponiveis = { "usuarios", "produtos", "historico", "movimentacoes" };
        private List<BsonDocument> DadosPreVisualizacao = new List<BsonDocument>();
        private string TabelaAtual = string.Empty;

        public ExcelWindow()
        {
            InitializeComponent();
        }

        // Importa os dados do Excel para o DataGrid
        private async void ImportarDados_Click(object sender, RoutedEventArgs e)
        {
            // Reseta a barra de progresso e a mensagem de status
            ResetarProgresso();
            AtualizarProgresso(10, "Status: Iniciando importação de dados...");
            await Task.Delay(200); // Simula um pequeno atraso para visualização do progresso

            // Esconde a pré-visualização dos dados
            PreviewPanel.Visibility = Visibility.Collapsed;

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls",
                Title = "Selecione um arquivo Excel"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    AtualizarProgresso(30, "Status: Carregando dados do arquivo Excel...");

                    // Carrega os dados do Excel
                    var dadosExcel = CarregarDadosExcel(openFileDialog.FileName);

                    AtualizarProgresso(50, "Status: Formatando dados para pré-visualização...");
                    await Task.Delay(200); // Simula um pequeno atraso para visualização do progresso

                    // Converte os dados do Excel para BsonDocument e preenche colunas não preenchidas
                    DadosPreVisualizacao = TratarDadosParaBanco(dadosExcel);
                    

                    AtualizarProgresso(70, "Status: Apresentando dados para importação...");
                    await Task.Delay(200); // Simula um pequeno atraso para visualização do progresso

                    // Atualiza o DataGrid com os dados tratados
                    AtualizarPreviewDatagrid(dadosExcel);
                    
                    /* DEPURAÇÃO */
                    // Define o caminho do arquivo TXT
                    string caminhoArquivoTxt = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DadosTratados.txt");

                    // Salva os dados tratados no arquivo TXT
                    SalvarDadosTratadosEmTxt(DadosPreVisualizacao, caminhoArquivoTxt);
                    /* DEPURAÇÃO */

                    AtualizarProgresso(100, "Status: Dados carregados com sucesso!");

                    // Mostra a pré-visualização
                    PreviewPanel.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    AtualizarProgresso(0, "Status: Erro ao carregar os dados.");
                    MessageBox.Show($"Erro ao carregar o arquivo Excel: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                AtualizarProgresso(0, "Status: Importação cancelada pelo usuário.");
            }
        }
        /* DEPURAÇÃO */
        private void SalvarDadosTratadosEmTxt(List<BsonDocument> dadosTratados, string caminhoArquivo)
        {
            try
            {
                using (var writer = new System.IO.StreamWriter(caminhoArquivo, false, Encoding.UTF8))
                {
                    writer.WriteLine("### DADOS TRATADOS PARA IMPORTAÇÃO ###");
                    writer.WriteLine($"Data de Geração: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                    writer.WriteLine($"Quantidade de Registros: {dadosTratados.Count}");
                    writer.WriteLine(new string('-', 50));

                    foreach (var documento in dadosTratados)
                    {
                        writer.WriteLine(documento.ToString()); // Serializa o BsonDocument como string
                        writer.WriteLine(new string('-', 50)); // Separador entre documentos
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar os dados tratados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /* DEPURAÇÃO */

        // Função para carregar os dados do Excel  
        private Dictionary<string, DataTable> CarregarDadosExcel(string caminhoArquivo)
        {
            var dadosPorAba = new Dictionary<string, DataTable>();

            using (var workbook = new XLWorkbook(caminhoArquivo))
            {
                foreach (var worksheet in workbook.Worksheets)
                {
                    var dataTable = new DataTable(worksheet.Name);

                    // Adiciona as colunas com base nos cabeçalhos
                    var headers = worksheet.Row(1).CellsUsed().Select(c => c.GetValue<string>().Trim()).ToList();
                    foreach (var header in headers)
                    {
                        dataTable.Columns.Add(header); // Usa os nomes originais das colunas
                    }

                    // Adiciona as linhas com base nos dados
                    foreach (var row in worksheet.RowsUsed().Skip(1)) // Ignora a linha de cabeçalho
                    {
                        var dataRow = dataTable.NewRow();
                        for (int i = 0; i < headers.Count; i++)
                        {
                            try
                            {
                                var cell = row.Cell(i + 1);

                                // Trata o valor da célula de forma genérica
                                if (cell.IsEmpty())
                                {
                                    dataRow[i] = string.Empty; // Célula vazia
                                }
                                else if (cell.DataType == XLDataType.DateTime)
                                {
                                    dataRow[i] = cell.GetDateTime().ToString("dd/MM/yyyy HH:mm:ss"); // Formata como data
                                }
                                else if (cell.DataType == XLDataType.Number)
                                {
                                    // Verifica se o número é inteiro ou decimal
                                    if (cell.TryGetValue(out int intValue))
                                    {
                                        dataRow[i] = intValue.ToString(); // Converte inteiro para string
                                    }
                                    else if (cell.TryGetValue(out double doubleValue))
                                    {
                                        dataRow[i] = doubleValue.ToString("G", System.Globalization.CultureInfo.InvariantCulture); // Converte decimal para string
                                    }
                                    else
                                    {
                                        dataRow[i] = cell.GetValue<string>(); // Fallback para string
                                    }
                                }
                                else if (cell.DataType == XLDataType.Boolean)
                                {
                                    dataRow[i] = cell.GetBoolean() ? "true" : "false"; // Converte booleano para string
                                }
                                else if (cell.DataType == XLDataType.Text)
                                {
                                    dataRow[i] = cell.GetValue<string>(); // Trata como string
                                }
                                else
                                {
                                    dataRow[i] = cell.GetValue<string>(); // Fallback para string
                                }
                            }
                            catch (Exception ex)
                            {
                                // Adiciona informações detalhadas sobre o erro
                                throw new Exception($"Erro ao processar a célula na aba '{worksheet.Name}', linha {row.RowNumber()}, coluna {headers[i]}: {ex.Message}", ex);
                            }
                        }
                        dataTable.Rows.Add(dataRow);
                    }

                    dadosPorAba.Add(worksheet.Name, dataTable);
                }
            }

            return dadosPorAba;
        }


        // Atualiza o DataGrid para exibir os dados como aparecem no Excel
        private void AtualizarPreviewDatagrid(Dictionary<string, DataTable> dadosPorAba)
        {
            // Limpa o ComboBox, o DataGrid e DadosPreVisualizacao
            TabelaComboBox.Items.Clear();
            PreviewDataGrid.ItemsSource = null;
            DadosPreVisualizacao.Clear();

            // Adiciona as abas (nomes das tabelas) ao ComboBox
            foreach (var aba in dadosPorAba.Keys)
            {
                TabelaComboBox.Items.Add(aba);

                // Converte os dados da aba para BsonDocument e adiciona a DadosPreVisualizacao
                var dataTable = dadosPorAba[aba];
                foreach (DataRow row in dataTable.Rows)
                {
                    var bsonDocument = new BsonDocument();
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        bsonDocument[column.ColumnName] = row[column]?.ToString() ?? string.Empty;
                    }
                    bsonDocument["Tabela"] = aba; // Adiciona o nome da aba como referência
                    DadosPreVisualizacao.Add(bsonDocument);
                }
            }

            // Define o evento de seleção de tabela
            TabelaComboBox.SelectionChanged += (s, e) =>
            {
                if (TabelaComboBox.SelectedItem is string tabelaSelecionada && dadosPorAba.ContainsKey(tabelaSelecionada))
                {
                    PreviewDataGrid.ItemsSource = dadosPorAba[tabelaSelecionada].DefaultView;
                }
            };

            // Seleciona a primeira aba por padrão
            if (TabelaComboBox.Items.Count > 0)
            {
                TabelaComboBox.SelectedIndex = 0;
                PreviewPanel.Visibility = Visibility.Visible;
            }
        }
        // Função para tratar os dados e criar documentos BSON
        private List<BsonDocument> TratarDadosParaBanco(Dictionary<string, DataTable> dadosPorAba)
        {
            var listaBson = new List<BsonDocument>();

            // 1. Normalizar os nomes das colunas para o padrão do banco
            NormalizarNomesColunas(dadosPorAba);

            // 2. Mapear os dados para as tabelas correspondentes
            var mapeamentoColunas = ObterMapeamentoColunas();

            // Dicionário para rastrear os IDs criados por aba
            var idsCriadosPorAba = new Dictionary<string, HashSet<int>>();

            foreach (var aba in dadosPorAba.Keys)
            {
                var dataTable = dadosPorAba[aba];
                int ultimoIdNumerico = 0; // Inicializa o ID numérico para tabelas específicas

                foreach (DataRow row in dataTable.Rows)
                {
                    try
                    {
                        // 3. Criar um documento BSON para cada linha
                        var bsonDocument = CriarDocumentoBson(row, dataTable.Columns, mapeamentoColunas, aba, ref ultimoIdNumerico, idsCriadosPorAba);

                        listaBson.Add(bsonDocument);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Erro ao processar a linha na aba '{aba}', linha {dataTable.Rows.IndexOf(row) + 1}: {ex.Message}", ex);
                    }
                }
            }

            return listaBson;
        }

        // Função para normalizar os nomes das colunas e aplicar o mapeamento
        private void NormalizarNomesColunas(Dictionary<string, DataTable> dadosPorAba)
        {
            foreach (var aba in dadosPorAba.Keys.ToList())
            {
                var dataTable = dadosPorAba[aba];

                // Obter o mapeamento de colunas
                var mapeamentoColunas = ObterMapeamentoColunas();

                foreach (DataColumn column in dataTable.Columns)
                {
                    // Verifica se o nome da coluna existe no mapeamento
                    if (mapeamentoColunas.TryGetValue(column.ColumnName.ToLower(), out var novoNome))
                    {
                        column.ColumnName = novoNome; // Aplica o novo nome da coluna
                    }
                    else
                    {
                        column.ColumnName = column.ColumnName.ToLower(); // Converte para minúsculo se não estiver no mapeamento
                    }
                }

                dadosPorAba[aba] = dataTable;
            }
        }

        // Função para obter o mapeamento de colunas
        private Dictionary<string, string> ObterMapeamentoColunas()
        {
            return new Dictionary<string, string>
            {
                { "nível", "nivel" },
                { "usuário", "usuario" },
                { "id do produto", "produtoId" },
                { "preço", "preco" },
                { "e-mail", "email" },
                { "matrícula", "matricula" },
                { "código", "codigo" },
            };
        }

        // Função para criar um documento BSON a partir de uma linha do DataTable  
        private BsonDocument CriarDocumentoBson(
            DataRow row,
            DataColumnCollection columns,
            Dictionary<string, string> mapeamentoColunas,
            string aba,
            ref int ultimoIdNumerico,
            Dictionary<string, HashSet<int>> idsCriadosPorAba)
        {
            // Inicializa um novo documento BSON vazio  
            var bsonDocument = new BsonDocument();

            // Cria a coluna _id com base na tabela (aba)  
            if (!columns.Contains("_id"))
            {
                columns.Add("_id");
            }

            // Garante que o dicionário de IDs criados tenha uma entrada para a aba atual
            if (!idsCriadosPorAba.ContainsKey(aba))
            {
                idsCriadosPorAba[aba] = new HashSet<int>();
            }

            // Define o valor do campo _id e outros campos extras com base na aba/tabela
            switch (aba.ToLower())
            {
                case "usuarios":
                    // Usa a matrícula como _id ou gera um novo ObjectId
                    row["_id"] = row.Table.Columns.Contains("matricula") && !string.IsNullOrEmpty(row["matricula"]?.ToString())
                        ? row["matricula"].ToString()
                        : ObjectId.NewObjectId().ToString();
                    break;

                case "produtos":
                    // Usa o código como _id ou gera um novo ObjectId
                    row["_id"] = row.Table.Columns.Contains("codigo") && !string.IsNullOrEmpty(row["codigo"]?.ToString())
                        ? row["codigo"].ToString()
                        : ObjectId.NewObjectId().ToString();
                    break;

                case "movimentacoes":
                case "historico":
                    // Obtém a coleção correspondente com base na aba
                    var colecao = DatabaseConnect.Database.GetCollection<BsonDocument>(aba);

                    // Busca o maior valor de _id na coleção, considerando apenas valores numéricos
                    var maiorId = colecao.Query()
                        .Where(x => x["_id"].IsInt32) // Filtra apenas os _id que são inteiros
                        .OrderByDescending(x => x["_id"])
                        .Select(x => x["_id"].AsInt32) // Converte para int
                        .FirstOrDefault();

                    // Define o último ID numérico como o maior encontrado ou 0 se não houver registros
                    ultimoIdNumerico = Math.Max(ultimoIdNumerico, maiorId);

                    // Garante que o ID seja único dentro da aba
                    do
                    {
                        ultimoIdNumerico++;
                    } while (idsCriadosPorAba[aba].Contains(ultimoIdNumerico));

                    // Adiciona o novo ID à lista de IDs criados para a aba
                    idsCriadosPorAba[aba].Add(ultimoIdNumerico);

                    // Atribui o ID à linha
                    row["_id"] = ultimoIdNumerico;

                    // Adiciona o campo "DataFormatadaSemAno" e "DataFormatadaComAno"
                    if (columns.Contains("data") && DateTime.TryParse(row["data"]?.ToString(), out var data))
                    {
                        if (!columns.Contains("DataFormatadaSemAno"))
                        {
                            columns.Add("DataFormatadaSemAno");
                        }
                        row["DataFormatadaSemAno"] = data.ToString("dd/MM HH:mm:ss");

                        if (!columns.Contains("DataFormatadaComAno"))
                        {
                            columns.Add("DataFormatadaComAno");
                        }
                        row["DataFormatadaComAno"] = data.ToString("dd/MM/yyyy HH:mm:ss");
                    }
                    break;

                default:
                    // Gera um ObjectId para tabelas não especificadas
                    row["_id"] = ObjectId.NewObjectId().ToString();
                    break;
            }

            // Retorna o documento BSON completamente formatado  
            return bsonDocument;
        }


        // Evento disparado ao alterar a seleção no ComboBox de tabelas
        private void TabelaComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e){}


        // Confirma a importação dos dados para o banco de dados
        private async void ConfirmarImportacao_Click(object sender, RoutedEventArgs e)
        {
            if (ImportModeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedMode = selectedItem.Content.ToString();

                if (!DadosPreVisualizacao.Any())
                {
                    AtualizarProgresso(0, "Status: Nenhum dado disponível para importação.");
                    MessageBox.Show("Nenhum dado carregado para importação.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    AtualizarProgresso(10, "Status: Iniciando importação...");
                    await Task.Delay(100); // Simula um pequeno atraso para visualização do progresso

                    foreach (var tabela in TabelasDisponiveis)
                    {
                        AtualizarProgresso(20, $"Status: Processando tabela '{tabela}'...");

                        var collection = DatabaseConnect.Database.GetCollection<BsonDocument>(tabela);

                        if (selectedMode == "Substituir Dados")
                        {
                            AtualizarProgresso(40, $"Status: Limpando dados existentes na tabela '{tabela}'...");
                            collection.DeleteAll();
                        }

                        AtualizarProgresso(60, $"Status: Inserindo novos dados na tabela '{tabela}'...");

                        // Filtra os dados da tabela atual e converte os tipos corretamente
                        var dadosTabela = DadosPreVisualizacao
                            .Where(d => d["Tabela"].AsString == tabela)
                            .Select(d =>
                            {
                                var documento = ConverterTipos(d);
                                documento.Remove("Tabela"); // Remove a coluna "Tabela" do documento
                                return documento; // Retorna documento BsonDocument 
                            })
                            .ToList();

                        foreach (var documento in dadosTabela)
                        {
                            collection.Insert(documento);
                        }
                    }

                    AtualizarProgresso(100, "Status: Dados importados com sucesso!");
                    MessageBox.Show($"Dados importados com sucesso no modo '{selectedMode}'.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    ResetarProgresso();
                    PreviewPanel.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    AtualizarProgresso(0, "Status: Erro ao importar os dados.");
                    MessageBox.Show($"Erro ao importar dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                AtualizarProgresso(0, "Status: Nenhum modo de importação selecionado.");
                MessageBox.Show("Selecione um modo de importação antes de confirmar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Função auxiliar para converter os tipos dos campos no BsonDocument
        private BsonDocument ConverterTipos(BsonDocument documento)
        {
            var camposParaConverter = new Dictionary<string, Func<string, BsonValue>>
            {
                { "_id", valor => int.TryParse(valor, out var id) ? new BsonValue(id) : new BsonValue(valor) },
                { "preco", valor => double.TryParse(valor, out var preco) ? new BsonValue(preco) : new BsonValue(valor) },
                { "quantidade", valor => int.TryParse(valor, out var quantidade) ? new BsonValue(quantidade) : new BsonValue(valor) },
                { "data", valor =>
                    {
                        if (DateTime.TryParseExact(valor, "dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var data))
                        {
                            return new BsonValue(data);
                        }
                        return new BsonValue(valor);
                    }
                }
            };

            foreach (var campo in camposParaConverter.Keys)
            {
                if (documento.ContainsKey(campo) && documento[campo].IsString)
                {
                    var valor = documento[campo].AsString;
                    documento[campo] = camposParaConverter[campo](valor);
                }
            }
            return documento;
        }

        // Exporta os dados do banco de dados para o Excel
        private async void ExportarDados_Click(object sender, RoutedEventArgs e)
        {
            // Reseta a barra de progresso
            ResetarProgresso();

            AtualizarProgresso(10, "Status: Iniciando exportação de dados...");
            await Task.Delay(200); // Simula um pequeno atraso para visualização do progresso

            try
            {
                // Configura o diálogo para salvar o arquivo Excel
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    Title = "Exportar dados para Excel",
                    FileName = "dados-exportados.xlsx"
                };

                // Verifica se o usuário selecionou um local para salvar
                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        int totalTabelas = TabelasDisponiveis.Length;
                        int tabelaAtual = 0;

                        // Itera sobre as tabelas disponíveis
                        foreach (var tabela in TabelasDisponiveis)
                        {
                            // Obtém a coleção do banco de dados
                            var collection = DatabaseConnect.Database?.GetCollection<BsonDocument>(tabela);
                            if (collection == null)
                            {
                                MessageBox.Show($"Erro ao acessar a coleção '{tabela}' do banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                                continue;
                            }

                            // Obtém os dados da tabela
                            var dadosTabela = collection.FindAll().ToList();
                            var worksheet = workbook.Worksheets.Add(tabela);

                            if (dadosTabela.Any())
                            {
                                // Adiciona os cabeçalhos dinamicamente
                                var headers = dadosTabela.First().Keys
                                    .Where(header => !header.Equals("ID", StringComparison.OrdinalIgnoreCase) &&
                                                     !header.Equals("DataFormatadaSemAno", StringComparison.OrdinalIgnoreCase) &&
                                                     !header.Equals("DataFormatadaComAno", StringComparison.OrdinalIgnoreCase))
                                    .ToList();

                                for (int i = 0; i < headers.Count; i++)
                                {
                                    var cell = worksheet.Cell(1, i + 1);
                                    cell.Value = RenomearColuna(headers[i]);
                                    cell.Style.Font.Bold = true;
                                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6680E8");
                                    cell.Style.Font.FontColor = XLColor.White;
                                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                }

                                // Adiciona os dados às células
                                for (int i = 0; i < dadosTabela.Count; i++)
                                {
                                    var item = dadosTabela[i];
                                    for (int j = 0; j < headers.Count; j++)
                                    {
                                        var cell = worksheet.Cell(i + 2, j + 1);
                                        var value = item[headers[j]];

                                        if (value != null)
                                        {
                                            // Trata o campo de data no formato JSON
                                            if (headers[j].ToLower().Contains("data") && value.ToString().Contains("\"$date\":"))
                                            {
                                                try
                                                {
                                                    // Extrai a data do formato JSON
                                                    var dataStr = value.ToString().Replace("{\"$date\":\"", "").Replace("\"}", "");
                                                    if (DateTime.TryParse(dataStr, out var dataConvertida))
                                                    {
                                                        cell.Value = dataConvertida; // Define como DateTime
                                                        cell.Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss"; // Formata como data
                                                    }
                                                    else
                                                    {
                                                        cell.Value = value.ToString(); // Mantém o valor original se não puder converter
                                                    }
                                                }
                                                catch
                                                {
                                                    cell.Value = value.ToString(); // Mantém o valor original em caso de erro
                                                }
                                            }
                                            else if (double.TryParse(value.ToString(), out var numero))
                                            {
                                                cell.Value = numero; // Define como número
                                            }
                                            else
                                            {
                                                cell.Value = value.ToString(); // Define como texto

                                                // Remove aspas duplas
                                                if (cell.Value.ToString().Contains("\""))
                                                {
                                                    cell.Value = cell.Value.ToString().Replace("\"", "");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            cell.Value = string.Empty; // Define como vazio
                                        }

                                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                                        // Alterna a cor de fundo das linhas
                                        cell.Style.Fill.BackgroundColor = i % 2 == 0 ? XLColor.White : XLColor.FromHtml("#F0F0F0");
                                    }
                                }

                                // Ajusta as colunas para caberem no conteúdo
                                worksheet.Columns().AdjustToContents();
                            }

                            tabelaAtual++;
                            AtualizarProgresso((double)tabelaAtual / totalTabelas * 100, $"Status: Exportando tabela '{tabela}'...");
                            await Task.Delay(100); // Simula um pequeno atraso para visualização do progresso
                        }

                        // Salva o arquivo Excel
                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    AtualizarProgresso(100, "Status: Dados exportados com sucesso!");
                    MessageBox.Show("Dados exportados com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    AtualizarProgresso(0, "Status: Exportação cancelada pelo usuário.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao exportar dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Gera uma tabela padrão com cabeçalhos dinâmicos
        private async void GerarTabela_Click(object sender, RoutedEventArgs e)
        {
            // Reseta a barra de progresso
            ResetarProgresso();

            AtualizarProgresso(10, "Status: Iniciando geração da tabela padrão...");
            await Task.Delay(200); // Simula um pequeno atraso para visualização do progresso

            try
            {
                // Configura o diálogo para salvar o arquivo Excel
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    Title = "Gerar tabela padrão",
                    FileName = "tabela-padrao.xlsx"
                };

                // Verifica se o usuário selecionou um local para salvar
                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        // Itera sobre as tabelas disponíveis
                        foreach (var tabela in TabelasDisponiveis)
                        {
                            var worksheet = workbook.Worksheets.Add(tabela);

                            // Obtém os cabeçalhos dinamicamente com base no tipo de dado
                            var headers = GetHeadersForTable(tabela)
                                .Where(header => !header.Equals("ID", StringComparison.OrdinalIgnoreCase) &&
                                                 !header.Equals("DataFormatadaSemAno", StringComparison.OrdinalIgnoreCase) &&
                                                 !header.Equals("DataFormatadaComAno", StringComparison.OrdinalIgnoreCase))
                                .ToList();

                            // Adiciona os cabeçalhos
                            for (int i = 0; i < headers.Count; i++)
                            {
                                var cell = worksheet.Cell(1, i + 1);
                                cell.Value = RenomearColuna(headers[i]);
                                cell.Style.Font.Bold = true;
                                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6680E8");
                                cell.Style.Font.FontColor = XLColor.White;
                                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                                // Define o tipo de dado para as colunas
                                if (headers[i].ToLower().Contains("data"))
                                {
                                    worksheet.Column(i + 1).Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
                                }
                                else if (headers[i].ToLower().Contains("preco") || headers[i].ToLower().Contains("quantidade"))
                                {
                                    worksheet.Column(i + 1).Style.NumberFormat.Format = "#,##0.00"; // Formato numérico
                                }
                            }

                            // Ajusta as colunas para caberem no conteúdo
                            worksheet.Columns().AdjustToContents();
                        }

                        // Salva o arquivo Excel
                        workbook.SaveAs(saveFileDialog.FileName);
                    }
                    AtualizarProgresso(100, "Status: Tabela padrão gerada com sucesso!");
                    MessageBox.Show("Tabela padrão gerada com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    AtualizarProgresso(0, "Status: Geração de Tabela cancelada pelo usuário.");
                }
            }
            catch (Exception ex)
            {
                AtualizarProgresso(0, "Status: Erro ao gerar tabela padrão.");
                MessageBox.Show($"Erro ao gerar tabela padrão: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // Reseta a barra de progresso
        private void ResetarProgresso()
        {
            ProgressBar.Value = 0;
            StatusMessage.Text = "Status: Aguardando ação...";
        }

        // Atualiza a barra de progresso
        private void AtualizarProgresso(double progresso, string mensagem)
        {
            ProgressBar.Value = progresso;
            StatusMessage.Text = mensagem;
        }

        // Evento disparado ao alterar o texto na barra de pesquisa
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PreviewDataGrid.ItemsSource is IEnumerable<Dictionary<string, object>> dados)
            {
                string filtro = SearchBox.Text.ToLower();
                PreviewDataGrid.ItemsSource = dados
                    .Where(d => d.Values.Any(v => v.ToString().ToLower().Contains(filtro)))
                    .ToList();
            }
        }

        // Evento disparado ao clicar no botão "Filtrar por"
        private void FiltrarButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidade de filtro ainda não implementada.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Evento disparado ao clicar no botão "Cancelar"
        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            // Limpa a pré-visualização e reseta o estado
            PreviewDataGrid.ItemsSource = null;
            DadosPreVisualizacao.Clear();
            TabelaAtual = string.Empty;
            PreviewPanel.Visibility = Visibility.Collapsed;
            StatusMessage.Text = "Status: Ação cancelada.";
        }


        // Obtém os cabeçalhos dinamicamente com base no tipo de dado
        private List<string> GetHeadersForTable(string tabela)
        {
            return tabela.ToLower() switch
            {
                "usuarios" => typeof(UsuarioData).GetProperties().Select(p => p.Name).ToList(),
                "movimentacoes" => typeof(MovimentacaoData).GetProperties().Select(p => p.Name).ToList(),
                "produtos" => typeof(ProdutoData).GetProperties().Select(p => p.Name).ToList(),
                "historico" => typeof(LogData).GetProperties().Select(p => p.Name).ToList(),
                _ => new List<string> { "Id", "Nome", "Descrição" }
            };
        }

        // Função auxiliar para renomear colunas
        private string RenomearColuna(string nomeOriginal)
        {
            // Normaliza o nome da coluna
            nomeOriginal = nomeOriginal.Trim().ToLower();

            // Mapeamento genérico de nomes de colunas
            var mapeamento = new Dictionary<string, string>
            {
                { "_id", "ID" },
                { "nome", "Nome" },
                { "email", "E-mail" },
                { "matricula", "Matrícula" },
                { "senha", "Senha" },
                { "cargo", "Cargo" },
                { "tipo", "Tipo" },
                { "marca", "Marca" },
                { "codigo", "Código" },
                { "preco", "Preço" },
                { "quantidade", "Quantidade" },
                { "produtoid", "ID do Produto" },
                { "data", "Data" },
                { "nivel", "Nível" },
                { "detalhes", "Detalhes" },
                { "usuario", "Usuário" },
                { "dataformatadasemano", "Data (Sem Ano)" },
                { "dataformatadacomano", "Data (Com Ano)" }
            };

            // Retorna o nome amigável ou o nome original se não houver mapeamento
            return mapeamento.ContainsKey(nomeOriginal) ? mapeamento[nomeOriginal] : nomeOriginal;
        }

        // Evento disparado ao alterar a visibilidade do painel de pré-visualização
        private void PreviewPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (PreviewPanel.Visibility == Visibility.Visible)
            {
                // Altera a altura da janela para 800 quando a pré-visualização estiver visível  
                this.Height = 700;
            }
            else if (PreviewPanel.Visibility == Visibility.Collapsed)
            {
                // Altera a altura da janela para 320 quando a pré-visualização estiver oculta  
                this.Height = 320;
            }

            // Reposiciona a janela no centro da tela  com base na nova altura
            this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
            this.Top = (SystemParameters.PrimaryScreenHeight - this.Height) / 2;
        }
    }
}

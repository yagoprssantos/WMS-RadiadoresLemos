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

                    // Etapa 1: Tratar os dados para apresentação
                    var dadosFormatados = new Dictionary<string, DataTable>();
                    foreach (var aba in dadosExcel.Keys)
                    {
                        dadosFormatados[aba] = FormatarDadosParaApresentacao(dadosExcel[aba]);
                    }

                    AtualizarProgresso(70, "Status: Atualizando pré-visualização...");

                    // Exibe os dados na pré-visualização
                    AtualizarPreviewDatagrid(dadosFormatados);

                    // Etapa 2: Tratar os dados para o banco de dados
                    DadosPreVisualizacao = ConverterExcelParaBsonDocument(dadosExcel);

                    AtualizarProgresso(100, "Status: Dados carregados com sucesso!");
                    MessageBox.Show("Dados carregados com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

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
                    var headers = worksheet.Row(1).CellsUsed().Select(c => c.Value.ToString().Trim()).ToList();
                    foreach (var header in headers)
                    {
                        dataTable.Columns.Add(header);
                    }

                    // Adiciona as linhas com base nos dados
                    foreach (var row in worksheet.RowsUsed().Skip(1)) // Ignora a linha de cabeçalho
                    {
                        var dataRow = dataTable.NewRow();
                        for (int i = 0; i < headers.Count; i++)
                        {
                            dataRow[i] = row.Cell(i + 1).Value.ToString();
                        }
                        dataTable.Rows.Add(dataRow);
                    }

                    // Log para verificar os nomes das colunas
                    Console.WriteLine($"Tabela: {worksheet.Name}");
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        Console.WriteLine($"Coluna: {column.ColumnName}");
                    }

                    dadosPorAba.Add(worksheet.Name, dataTable);
                }
            }

            return dadosPorAba;
        }

        // Formata os dados para apresentação  
        private DataTable FormatarDadosParaApresentacao(DataTable dataTable)
        {
            // Cria uma nova tabela formatada
            var tabelaFormatada = new DataTable();

            // Adiciona as colunas renomeadas à nova tabela
            foreach (DataColumn coluna in dataTable.Columns)
            {
                tabelaFormatada.Columns.Add(RenomearColuna(coluna.ColumnName), typeof(string));
            }

            // Adiciona as linhas formatadas à nova tabela
            foreach (DataRow row in dataTable.Rows)
            {
                var novaLinha = tabelaFormatada.NewRow();

                foreach (DataColumn coluna in dataTable.Columns)
                {
                    var valor = row[coluna]?.ToString() ?? string.Empty;

                    // Verifica e formata datas no formato {$date:...}
                    if (coluna.ColumnName.ToLower().Contains("data") && valor.StartsWith("{$date:") && valor.EndsWith("}"))
                    {
                        try
                        {
                            // Extrai a data do formato {$date:...}
                            var dataStr = valor.Replace("{$date:", "").Replace("}", "");
                            if (DateTime.TryParse(dataStr, out var dataConvertida))
                            {
                                novaLinha[RenomearColuna(coluna.ColumnName)] = dataConvertida.ToString("dd/MM/yyyy HH:mm:ss");
                            }
                            else
                            {
                                novaLinha[RenomearColuna(coluna.ColumnName)] = valor; // Mantém o valor original se não puder converter
                            }
                        }
                        catch
                        {
                            novaLinha[RenomearColuna(coluna.ColumnName)] = valor; // Mantém o valor original em caso de erro
                        }
                    }
                    // Formata datas normais
                    else if (coluna.ColumnName.ToLower().Contains("data") && DateTime.TryParse(valor, out var dataNormal))
                    {
                        novaLinha[RenomearColuna(coluna.ColumnName)] = dataNormal.ToString("dd/MM/yyyy HH:mm:ss");
                    }
                    else
                    {
                        novaLinha[RenomearColuna(coluna.ColumnName)] = valor;
                    }
                }

                tabelaFormatada.Rows.Add(novaLinha);
            }

            return tabelaFormatada;
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


        // Converte os dados do Excel para BsonDocument
        private List<BsonDocument> ConverterExcelParaBsonDocument(Dictionary<string, DataTable> dadosPorAba)
        {
            var listaBson = new List<BsonDocument>();

            foreach (var aba in dadosPorAba.Keys)
            {
                var dataTable = dadosPorAba[aba];

                foreach (DataRow row in dataTable.Rows)
                {
                    var bsonDocument = new BsonDocument();

                    foreach (DataColumn column in dataTable.Columns)
                    {
                        var valor = row[column]?.ToString() ?? string.Empty;

                        // Tenta converter valores de data no formato ISO para DateTime
                        if (column.ColumnName.ToLower().Contains("data") && DateTime.TryParse(valor, out var dataConvertida))
                        {
                            bsonDocument[column.ColumnName] = dataConvertida;
                        }
                        else
                        {
                            bsonDocument[column.ColumnName] = valor;
                        }
                    }

                    // Adiciona o nome da aba como referência
                    bsonDocument["Tabela"] = aba;

                    listaBson.Add(bsonDocument);
                }
            }

            return listaBson;
        }



        // Função para exibir os dados na pré-visualização e preparar DadosPreVisualizacao
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
                    AtualizarProgresso(10, "Status: Conectando ao banco de dados...");
                    using (var db = DatabaseConnect.Database)
                    {
                        if (db == null)
                        {
                            AtualizarProgresso(0, "Status: Erro ao conectar ao banco de dados.");
                            MessageBox.Show("Erro ao conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        foreach (var tabela in TabelasDisponiveis)
                        {
                            AtualizarProgresso(20, $"Status: Processando tabela '{tabela}'...");

                            var collection = db.GetCollection<BsonDocument>(tabela);

                            if (selectedMode == "Substituir Dados")
                            {
                                AtualizarProgresso(40, $"Status: Limpando dados existentes na tabela '{tabela}'...");
                                collection.DeleteAll();
                            }

                            AtualizarProgresso(60, $"Status: Inserindo novos dados na tabela '{tabela}'...");

                            // Filtra os dados da tabela atual
                            var dadosTabela = DadosPreVisualizacao
                                .Where(d => d["Tabela"].AsString == tabela)
                                .Select(d =>
                                {
                                    d.Remove("Tabela"); // Remove o atributo "Tabela" antes de inserir no banco
                                    return d;
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

        // Exporta os dados do banco de dados para o Excel
        private async void ExportarDados_Click(object sender, RoutedEventArgs e)
        {
            ResetarProgresso();

            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    Title = "Exportar dados para Excel",
                    FileName = "dados-exportados.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        int totalTabelas = TabelasDisponiveis.Length;
                        int tabelaAtual = 0;

                        foreach (var tabela in TabelasDisponiveis)
                        {
                            var collection = DatabaseConnect.Database?.GetCollection<BsonDocument>(tabela);
                            if (collection == null)
                            {
                                MessageBox.Show($"Erro ao acessar a coleção '{tabela}' do banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                                continue;
                            }

                            var dadosTabela = collection.FindAll().ToList();
                            var worksheet = workbook.Worksheets.Add(tabela);

                            if (dadosTabela.Any())
                            {
                                // Adiciona cabeçalhos dinamicamente
                                var headers = dadosTabela.First().Keys.ToList();
                                for (int i = 0; i < headers.Count; i++)
                                {
                                    var cell = worksheet.Cell(1, i + 1);
                                    cell.Value = headers[i];
                                    cell.Style.Font.Bold = true;
                                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6680E8"); // Cor alterada
                                    cell.Style.Font.FontColor = XLColor.White;
                                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                }

                                // Adiciona os dados
                                for (int i = 0; i < dadosTabela.Count; i++)
                                {
                                    var item = dadosTabela[i];
                                    for (int j = 0; j < headers.Count; j++)
                                    {
                                        var cell = worksheet.Cell(i + 2, j + 1);
                                        var value = item[headers[j]]?.ToString().Replace("\"", "") ?? "";

                                        // Formata o campo de data, se aplicável
                                        if (headers[j].ToLower().Contains("data") && DateTime.TryParse(value, out var dateValue))
                                        {
                                            value = dateValue.ToString("dd/MM/yyyy HH:mm:ss");
                                        }

                                        cell.Value = value;
                                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                                        // Alterna a cor de fundo das linhas
                                        cell.Style.Fill.BackgroundColor = i % 2 == 0 ? XLColor.White : XLColor.FromHtml("#F0F0F0");
                                    }
                                }

                                // Ajusta as colunas
                                worksheet.Columns().AdjustToContents();
                            }

                            tabelaAtual++;
                            AtualizarProgresso((double)tabelaAtual / totalTabelas * 100, $"Status: Exportando tabela '{tabela}'.");
                            await Task.Delay(500); // Simula um pequeno atraso para visualização do progresso
                        }

                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    AtualizarProgresso(100, "Status: Dados exportados com sucesso!");
                    MessageBox.Show("Dados exportados com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao exportar dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // Gera uma tabela padrão com cabeçalhos dinâmicos
        private void GerarTabela_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    Title = "Gerar tabela padrão",
                    FileName = "tabela-padrao.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        foreach (var tabela in TabelasDisponiveis)
                        {
                            var worksheet = workbook.Worksheets.Add(tabela);

                            // Obtem os cabeçalhos dinamicamente com base nos dados exportados
                            var collection = DatabaseConnect.Database?.GetCollection<BsonDocument>(tabela);
                            if (collection == null)
                            {
                                MessageBox.Show($"Erro ao acessar a coleção '{tabela}' do banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                                continue;
                            }

                            var dadosTabela = collection.FindAll().ToList();
                            if (dadosTabela.Any())
                            {
                                // Adiciona cabeçalhos dinamicamente
                                var headers = dadosTabela.First().Keys.ToList();
                                for (int i = 0; i < headers.Count; i++)
                                {
                                    var cell = worksheet.Cell(1, i + 1);
                                    cell.Value = headers[i];
                                    cell.Style.Font.Bold = true;
                                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6680E8"); // Cor alterada
                                    cell.Style.Font.FontColor = XLColor.White;
                                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                }

                                // Ajusta as colunas
                                worksheet.Columns().AdjustToContents();
                            }
                        }

                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    StatusMessage.Text = "Status: Tabela padrão gerada com sucesso!";
                    MessageBox.Show("Tabela padrão gerada com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
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
                "historico" => typeof(LogData).GetProperties().Select(p => p.Name).ToList(),
                "alertas" => typeof(AlertaData).GetProperties().Select(p => p.Name).ToList(),
                _ => new List<string> { "Id", "Nome", "Descrição" }
            };
        }

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

using LiveCharts;
using LiveCharts.Wpf;
using System.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WMS_RadiadoresLemos_WPF.src.Models;
using System.Windows;
using System.Windows.Media;
using System.Collections.ObjectModel;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class DashboardUserControl : UserControl
    {
        // Variáveis necessárias para os gráficos
        public SeriesCollection EstoqueMarcasSeries { get; set; } = new SeriesCollection();
        public SeriesCollection MovimentacaoProdutosSeries { get; set; } = new SeriesCollection();
        public SeriesCollection TendenciaMovimentacaoSeries { get; set; } = new SeriesCollection();
        public SeriesCollection ProdutosMaiorMovimentacaoSeries { get; set; } = new SeriesCollection();
        public SeriesCollection LucroMensalSeries { get; set; } = new SeriesCollection();
        public SeriesCollection ProdutosVendidosSeries { get; set; } = new SeriesCollection();
        public string[] MarcasLabels { get; set; } = Array.Empty<string>();
        public string[] ProdutosLabels { get; set; } = Array.Empty<string>();
        public string[] PeriodoLabels { get; set; } = Array.Empty<string>();
        public string[] MesesLabels { get; set; } = Array.Empty<string>();
        public Func<double, string> FormatadorDeEixoY { get; set; } = value => value.ToString("N");

        // Propriedade para armazenar os logs
        public ObservableCollection<object> Logs { get; set; } = new ObservableCollection<object>();

        public DashboardUserControl()
        {
            InitializeComponent();
            DataContext = this;
            CarregarDados();
        }

        // Função para carregar os dados do dashboard
        private void CarregarDados()
        {
            ExibirContadores();
            ExibirGraficos();
        }

        // Exibe os contadores do dashboard
        private void ExibirContadores()
        {
            TotalUsuarios();
            TotalProdutos();
            BaixoEstoque();
        }

        // Exibe os gráficos do dashboard
        private void ExibirGraficos()
        {
            GraficoMovimentacaoProdutos();
            GraficoTendenciaMovimentacao();
            GraficoProdutosMaiorMovimentacao();
            GraficoLucroMensal("Janeiro");
            GraficoEstoqueMarcas();
            GraficoProdutosVendidos("Hoje");
        }

        // Exibe o total de usuários
        private void TotalUsuarios()
        {
            if (DadosCache.Tabelas.TryGetValue("Usuarios", out List<object>? usuarios))
            {
                int totalUsuarios = usuarios.Count;
                TotalUsuariosTextBlock.Text = totalUsuarios.ToString();
            }
        }

        // Exibe o total de produtos
        private void TotalProdutos()
        {
            if (DadosCache.Tabelas.TryGetValue("Produtos", out List<object>? produtos))
            {
                int totalProdutos = produtos.Count;
                TotalProdutosTextBlock.Text = totalProdutos.ToString();
            }
        }

        // Exibe a quantidade de produtos com baixo estoque
        private void BaixoEstoque()
        {
            int produtosBaixoEstoque = VerificarBaixoEstoque();
            ProdutosBaixoEstoqueTextBlock.Text = produtosBaixoEstoque.ToString();
        }

        // Exibe o gráfico de barras com as marcas de maior estoque
        private void GraficoEstoqueMarcas()
        {
            var marcasQuantidade = new Dictionary<string, int>();

            if (DadosCache.Tabelas.TryGetValue("Produtos", out List<object>? produtos))
            {
                foreach (ProdutoData produto in produtos)
                {
                    if (marcasQuantidade.ContainsKey(produto.Marca))
                    {
                        marcasQuantidade[produto.Marca] += produto.Quantidade;
                    }
                    else
                    {
                        marcasQuantidade[produto.Marca] = produto.Quantidade;
                    }
                }

                var topMarcas = marcasQuantidade.OrderByDescending(m => m.Value).Take(5).ToList();

                EstoqueMarcasSeries.Clear();

                int colorIndex = 0;
                var colors = new List<SolidColorBrush>
        {
            new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Blue
            new SolidColorBrush(Color.FromRgb(76, 175, 80)),  // Green
            new SolidColorBrush(Color.FromRgb(255, 193, 7)),  // Yellow
            new SolidColorBrush(Color.FromRgb(244, 67, 54)),  // Red
            new SolidColorBrush(Color.FromRgb(156, 39, 176))  // Purple
        };

                foreach (var marca in topMarcas)
                {
                    EstoqueMarcasSeries.Add(new ColumnSeries
                    {
                        Title = marca.Key,
                        Values = new ChartValues<int> { marca.Value },
                        Fill = colors[colorIndex % colors.Count],
                        ColumnPadding = 10, // Adiciona espaçamento entre as barras
                        MaxColumnWidth = 100 // Define o limite máximo da largura das colunas
                    });
                    colorIndex++;
                }

                MarcasLabels = topMarcas.Select(m => m.Key).ToArray();
            }
            else
            {
                Console.WriteLine("Tabela 'Produtos' não encontrada no cache de dados.");
            }

            // Atualiza os bindings
            DataContext = null;
            DataContext = this;

            // Configura o eixo Y do gráfico existente
            if (this.FindName("cartesianChart") is CartesianChart cartesianChart)
            {
                cartesianChart.AxisY.Clear();
                cartesianChart.AxisY.Add(new Axis
                {
                    Title = "Quantidade em Estoque",
                    MaxValue = 100, // Limite máximo do eixo Y definido para 100
                    MinValue = 0,
                    IsMerged = false, // Impede que o valor seja ajustado automaticamente
                    Separator = new LiveCharts.Wpf.Separator
                    {
                        Step = 20 // Define o intervalo entre os marcadores do eixo Y
                    },
                    LabelFormatter = FormatadorDeEixoY
                });
            }
        }


        // Verifica a quantidade de produtos com baixo estoque
        public int VerificarBaixoEstoque()
        {
            if (DadosCache.Tabelas.TryGetValue("Produtos", out List<object>? produtos))
            {
                return produtos.Count(p => ((ProdutoData)p).Quantidade < 10);
            }
            return 0;
        }

        // Exibe o gráfico de linhas com a quantidade de produtos vendidos
        private void GraficoProdutosVendidos(string periodo)
        {
            if (DadosCache.Tabelas.TryGetValue("Movimentacoes", out List<object>? movimentacoes))
            {
                var vendas = movimentacoes
                    .Where(m => ((MovimentacaoData)m).Tipo == "Saída" && ((MovimentacaoData)m).DataHora.ToString("MMMM") == periodo)
                    .GroupBy(m => ((MovimentacaoData)m).DataHora.Date)
                    .Select(g => new { Data = g.Key, Quantidade = g.Sum(m => ((MovimentacaoData)m).Quantidade) })
                    .OrderBy(v => v.Data)
                    .ToList();

                ProdutosVendidosSeries.Clear();

                ProdutosVendidosSeries.Add(new LineSeries
                {
                    Title = "Produtos Vendidos",
                    Values = new ChartValues<int>(vendas.Select(v => v.Quantidade)),
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 10,
                    Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243)) // Blue
                });

                PeriodoLabels = vendas.Select(v => v.Data.ToString("dd/MM/yyyy")).ToArray();
            }
            else
            {
                Console.WriteLine("Tabela 'Movimentacoes' não encontrada no cache de dados.");
            }

            DataContext = this;
        }

        private void SelecionarPeriodoProdutosVendidos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var periodoSelecionado = (sender as ComboBox)?.SelectedItem as ComboBoxItem;
            if (periodoSelecionado != null)
            {
                GraficoProdutosVendidos(periodoSelecionado.Content.ToString());
            }
        }

        // Exibe o gráfico de movimentação de produtos
        private void GraficoMovimentacaoProdutos()
        {
            if (DadosCache.Tabelas.TryGetValue("Movimentacoes", out List<object>? movimentacoes))
            {
                var movimentacao = movimentacoes
                    .GroupBy(m => ((MovimentacaoData)m).ProdutoId)
                    .Select(g => new { ProdutoId = g.Key, Quantidade = g.Sum(m => ((MovimentacaoData)m).Quantidade) })
                    .OrderByDescending(m => m.Quantidade)
                    .Take(5)
                    .ToList();

                MovimentacaoProdutosSeries.Clear();

                int colorIndex = 0;
                var colors = new List<SolidColorBrush>
                {
                    new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Blue
                    new SolidColorBrush(Color.FromRgb(76, 175, 80)),  // Green
                    new SolidColorBrush(Color.FromRgb(255, 193, 7)),  // Yellow
                    new SolidColorBrush(Color.FromRgb(244, 67, 54)),  // Red
                    new SolidColorBrush(Color.FromRgb(156, 39, 176))  // Purple
                };

                foreach (var item in movimentacao)
                {
                    MovimentacaoProdutosSeries.Add(new ColumnSeries
                    {
                        Title = item.ProdutoId,
                        Values = new ChartValues<int> { item.Quantidade },
                        Fill = colors[colorIndex % colors.Count],
                        ColumnPadding = 10,
                        MaxColumnWidth = 100
                    });
                    colorIndex++;
                }

                ProdutosLabels = movimentacao.Select(m => m.ProdutoId).ToArray();
            }
            else
            {
                Console.WriteLine("Tabela 'Movimentacoes' não encontrada no cache de dados.");
            }

            DataContext = this;
        }

        // Exibe o gráfico de tendência de movimentação
        private void GraficoTendenciaMovimentacao()
        {
            if (DadosCache.Tabelas.TryGetValue("Movimentacoes", out List<object>? movimentacoes))
            {
                var tendencia = movimentacoes
                    .GroupBy(m => ((MovimentacaoData)m).DataHora.Date)
                    .Select(g => new { Data = g.Key, Quantidade = g.Sum(m => ((MovimentacaoData)m).Quantidade) })
                    .OrderBy(t => t.Data)
                    .ToList();

                TendenciaMovimentacaoSeries.Clear();

                TendenciaMovimentacaoSeries.Add(new LineSeries
                {
                    Title = "Tendência de Movimentação",
                    Values = new ChartValues<int>(tendencia.Select(t => t.Quantidade)),
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 10,
                    Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243)) // Blue
                });

                PeriodoLabels = tendencia.Select(t => t.Data.ToString("dd/MM/yyyy")).ToArray();
            }
            else
            {
                Console.WriteLine("Tabela 'Movimentacoes' não encontrada no cache de dados.");
            }

            DataContext = this;
        }

        // Exibe o gráfico de produtos com maior movimentação
        private void GraficoProdutosMaiorMovimentacao()
        {
            if (DadosCache.Tabelas.TryGetValue("Movimentacoes", out List<object>? movimentacoes))
            {
                var maiorMovimentacao = movimentacoes
                    .GroupBy(m => ((MovimentacaoData)m).ProdutoId)
                    .Select(g => new { ProdutoId = g.Key, Quantidade = g.Sum(m => ((MovimentacaoData)m).Quantidade) })
                    .OrderByDescending(m => m.Quantidade)
                    .Take(5)
                    .ToList();

                ProdutosMaiorMovimentacaoSeries.Clear();

                int colorIndex = 0;
                var colors = new List<SolidColorBrush>
                {
                    new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Blue
                    new SolidColorBrush(Color.FromRgb(76, 175, 80)),  // Green
                    new SolidColorBrush(Color.FromRgb(255, 193, 7)),  // Yellow
                    new SolidColorBrush(Color.FromRgb(244, 67, 54)),  // Red
                    new SolidColorBrush(Color.FromRgb(156, 39, 176))  // Purple
                };

                foreach (var item in maiorMovimentacao)
                {
                    ProdutosMaiorMovimentacaoSeries.Add(new PieSeries
                    {
                        Title = item.ProdutoId,
                        Values = new ChartValues<int> { item.Quantidade },
                        Fill = colors[colorIndex % colors.Count],
                        DataLabels = true
                    });
                    colorIndex++;
                }

                ProdutosLabels = maiorMovimentacao.Select(m => m.ProdutoId).ToArray();
            }
            else
            {
                Console.WriteLine("Tabela 'Movimentacoes' não encontrada no cache de dados.");
            }

            DataContext = this;
        }

        // Exibe o gráfico de lucro mensal
        private void GraficoLucroMensal(string mes)
        {
            // Obter todas as movimentações do cache
            var movimentacoes = MovimentacoesCache.ObterMovimentacoes();

            // Verificar se há movimentações
            if (movimentacoes == null || !movimentacoes.Any())
            {
                Console.WriteLine("Nenhuma movimentação encontrada no cache.");
                return;
            }

            // Filtrar as movimentações pelo mês desejado
            var lucroMensal = movimentacoes
                .Where(m => m.DataHora.ToString("MMMM", new System.Globalization.CultureInfo("pt-BR")).Equals(mes, StringComparison.OrdinalIgnoreCase) && m.Tipo == "Saída")
                .GroupBy(m => m.DataHora.Date)
                .Select(g => new { Data = g.Key, Lucro = g.Sum(m => m.Quantidade * 10) }) // Supondo que o lucro por unidade seja 10
                .OrderBy(l => l.Data)
                .ToList();

            // Verificar se há dados após a filtragem
            if (!lucroMensal.Any())
            {
                Console.WriteLine($"Nenhum dado de lucro encontrado para o mês: {mes}");
                return;
            }

            // Limpar a série existente
            LucroMensalSeries.Clear();

            // Definir cores para as colunas
            int colorIndex = 0;
            var colors = new List<SolidColorBrush>
            {
                new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Azul
                new SolidColorBrush(Color.FromRgb(76, 175, 80)),  // Verde
                new SolidColorBrush(Color.FromRgb(255, 193, 7)),  // Amarelo
                new SolidColorBrush(Color.FromRgb(244, 67, 54)),  // Vermelho
                new SolidColorBrush(Color.FromRgb(156, 39, 176))  // Roxo
            };

            // Adicionar os dados ao gráfico
            foreach (var item in lucroMensal)
            {
                LucroMensalSeries.Add(new ColumnSeries
                {
                    Title = item.Data.ToString("dd/MM/yyyy"),
                    Values = new ChartValues<int> { item.Lucro },
                    Fill = colors[colorIndex % colors.Count],
                    ColumnPadding = 10,
                    MaxColumnWidth = 100
                });
                colorIndex++;
            }

            // Atualizar os rótulos do eixo X
            MesesLabels = lucroMensal.Select(l => l.Data.ToString("dd/MM/yyyy")).ToArray();

            // Atualizar o DataContext para refletir as mudanças
            DataContext = null;
            DataContext = this;
        }



        private void SelecionarPeriodoLucroMensal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var periodoSelecionado = (sender as ComboBox)?.SelectedItem as ComboBoxItem;
            if (periodoSelecionado != null)
            {
                GraficoLucroMensal(periodoSelecionado.Content.ToString());
            }
        }
    }
}
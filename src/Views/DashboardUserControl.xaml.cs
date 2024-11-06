using LiveCharts;
using LiveCharts.Wpf;
using System.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WMS_RadiadoresLemos_WPF.src.Models;
using System.Windows.Media;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class DashboardUserControl : UserControl
    {
        // Variáveis necessárias para os gráficos
        public SeriesCollection EstoqueMarcasSeries { get; set; } = new SeriesCollection();
        public string[] MarcasLabels { get; set; } = Array.Empty<string>();
        public Func<double, string> FormatadorDeEixoY { get; set; } = value => value.ToString("N");

        public DashboardUserControl()
        {
            InitializeComponent();
            CarregarDadosDoDashboard();
        }

        // Função para carregar os dados do dashboard
        private void CarregarDadosDoDashboard()
        {
            ExibirTotalUsuarios();
            ExibirTotalProdutos();
            ExibirProdutosBaixoEstoque();
            ExibirLogsRecentes();
            ExibirGraficoDeEstoqueMarcas();
        }

        // Exibe o gráfico de barras com as marcas de maior estoque
        private void ExibirGraficoDeEstoqueMarcas()
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

                EstoqueMarcasSeries = new SeriesCollection();

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

        // Exibe o total de usuários
        private void ExibirTotalUsuarios()
        {
            if (DadosCache.Tabelas.TryGetValue("Usuarios", out List<object>? usuarios))
            {
                int totalUsuarios = usuarios.Count;
                TotalUsuariosTextBlock.Text = totalUsuarios.ToString();
            }
        }

        // Exibe o total de produtos
        private void ExibirTotalProdutos()
        {
            if (DadosCache.Tabelas.TryGetValue("Produtos", out List<object>? produtos))
            {
                int totalProdutos = produtos.Count;
                TotalProdutosTextBlock.Text = totalProdutos.ToString();
            }
        }

        // Exibe a quantidade de produtos com baixo estoque
        private void ExibirProdutosBaixoEstoque()
        {
            if (DadosCache.Tabelas.TryGetValue("Produtos", out List<object>? produtos))
            {
                int produtosBaixoEstoque = produtos.Count(p => ((ProdutoData)p).Quantidade < 10);
                ProdutosBaixoEstoqueTextBlock.Text = produtosBaixoEstoque.ToString();
            }
        }

        // Exibe os logs recentes
        private void ExibirLogsRecentes()
        {
            // TODO: Implementar
        }
    }
}

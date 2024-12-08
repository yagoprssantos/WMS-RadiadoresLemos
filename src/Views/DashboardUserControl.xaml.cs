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

        // Dicionário para armazenar as cores dos produtos
        private Dictionary<string, SolidColorBrush> produtoCores = new Dictionary<string, SolidColorBrush>();


        public DashboardUserControl()
        {
            InitializeComponent();
            DataContext = this;
            CarregarDados();
        }

        // Função para gerar uma cor única
        private SolidColorBrush GerarCorUnica(int index)
        {
            // Usar a paleta HSL para gerar cores mais vivas
            double hue = (index * 137.508) % 360; // Usar o número áureo para distribuir uniformemente as cores
            double saturation = 0.7 + 0.3 * ((index / 360) % 2); // Alternar entre 0.7 e 1.0 para saturação
            double lightness = 0.5 + 0.2 * ((index / 720) % 2); // Alternar entre 0.5 e 0.7 para luminosidade

            // Converter HSL para RGB
            (byte r, byte g, byte b) = HslToRgb(hue, saturation, lightness);

            return new SolidColorBrush(Color.FromRgb(r, g, b));
        }

        // Função para obter a cor de um produto
        private SolidColorBrush ObterCorProduto(string produtoId)
        {
            if (!produtoCores.ContainsKey(produtoId))
            {
                produtoCores[produtoId] = GerarCorUnica(produtoCores.Count);
            }
            return produtoCores[produtoId];
        }


        // Função para carregar os dados do dashboard
        private void CarregarDados()
        {
            ExibirContadores();
            ExibirGraficos();
            CarregarComboBoxLucroMensal();
        }

        // Função para carregar o ComboBox de Lucro Mensal
        private void CarregarComboBoxLucroMensal()
        {
            if (DadosCache.Tabelas.TryGetValue("Movimentacoes", out List<object>? movimentacoes))
            {
                var meses = movimentacoes
                    .Select(m => ((MovimentacaoData)m).DataHora.ToString("MMM/yyyy"))
                    .Distinct()
                    .OrderBy(m => DateTime.ParseExact(m, "MMM/yyyy", null))
                    .ToList();

                SelecionarPeriodoLucroMensal.Items.Clear();
                SelecionarPeriodoLucroMensal.Items.Add("Sem filtros");

                // Adiciona os meses existentes nas movimentações ao ComboBox
                foreach (var mes in meses)
                {
                    SelecionarPeriodoLucroMensal.Items.Add(mes);
                }

                // Seleciona o primeiro item por padrão
                SelecionarPeriodoLucroMensal.SelectedIndex = 0;
            }
        }

        // Exibe os contadores do dashboard
        private void ExibirContadores()
        {
            TotalUsuarios();
            TotalProdutos();
            BaixoEstoque();
            TotalMarcas();
            TotalMovimentacoes();
            TotalEntradas();
            TotalSaidas();
        }

        // Exibe os gráficos do dashboard
        private void ExibirGraficos()
        {
            GraficoMovimentacaoProdutos();
            GraficoTendenciaMovimentacao();
            GraficoProdutosMaiorMovimentacao();
            GraficoLucroMensal("Sem filtros");
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

        // Exibe o total de marcas
        private void TotalMarcas()
        {
            if (DadosCache.Tabelas.TryGetValue("Produtos", out List<object>? produtos))
            {
                int totalMarcas = produtos.Select(p => ((ProdutoData)p).Marca).Distinct().Count();
                TotalMarcasTextBlock.Text = totalMarcas.ToString();
            }
        }

        // Exibe o total de movimentações
        private void TotalMovimentacoes()
        {
            if (DadosCache.Tabelas.TryGetValue("Movimentacoes", out List<object>? movimentacoes))
            {
                int totalMovimentacoes = movimentacoes.Count;
                TotalMovimentacoesTextBlock.Text = totalMovimentacoes.ToString();
            }
        }

        // Exibe o total de entradas
        private void TotalEntradas()
        {
            if (DadosCache.Tabelas.TryGetValue("Movimentacoes", out List<object>? movimentacoes))
            {
                int totalEntradas = movimentacoes.Count(m => ((MovimentacaoData)m).Tipo == "Entrada");
                TotalEntradasTextBlock.Text = totalEntradas.ToString();
            }
        }

        // Exibe o total de saídas
        private void TotalSaidas()
        {
            if (DadosCache.Tabelas.TryGetValue("Movimentacoes", out List<object>? movimentacoes))
            {
                int totalSaidas = movimentacoes.Count(m => ((MovimentacaoData)m).Tipo == "Saída");
                TotalSaidasTextBlock.Text = totalSaidas.ToString();
            }
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

                foreach (var marca in topMarcas)
                {
                    EstoqueMarcasSeries.Add(new ColumnSeries
                    {
                        Title = marca.Key,
                        Values = new ChartValues<int> { marca.Value },
                        Fill = ObterCorProduto(marca.Key),
                        ColumnPadding = 10, // Adiciona espaçamento entre as barras
                        MaxColumnWidth = 100 // Define o limite máximo da largura das colunas
                    });
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
                DateTime dataInicio = periodo switch
                {
                    "Hoje" => DateTime.Today,
                    "Última Semana" => DateTime.Today.AddDays(-7),
                    "Último Mês" => DateTime.Today.AddMonths(-1),
                    "Último Ano" => DateTime.Today.AddYears(-1),
                    _ => DateTime.MinValue
                };

                var produtosVendidos = movimentacoes
                    .Where(m => ((MovimentacaoData)m).Tipo == "Saída" && ((MovimentacaoData)m).DataHora >= dataInicio)
                    .GroupBy(m => ((MovimentacaoData)m).ProdutoId)
                    .Select(g => new { ProdutoId = g.Key, Quantidade = g.Sum(m => ((MovimentacaoData)m).Quantidade) })
                    .OrderByDescending(p => p.Quantidade)
                    .ToList();

                ProdutosVendidosSeries.Clear();

                foreach (var produto in produtosVendidos)
                {
                    ProdutosVendidosSeries.Add(new ColumnSeries
                    {
                        Title = produto.ProdutoId,
                        Values = new ChartValues<int> { produto.Quantidade },
                        Fill = ObterCorProduto(produto.ProdutoId)
                    });
                }

                ProdutosLabels = produtosVendidos.Select(p => p.ProdutoId).ToArray();
            }
            else
            {
                Console.WriteLine("Tabela 'Movimentacoes' não encontrada no cache de dados.");
            }

            DataContext = this;
        }

        private void SelecionarPeriodoProdutosVendidos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem is ComboBoxItem periodoSelecionado && periodoSelecionado.Content != null)
            {
                var periodo = periodoSelecionado.Content.ToString();
                if (!string.IsNullOrEmpty(periodo))
                {
                    GraficoProdutosVendidos(periodo);
                }
            }
        }

        // Exibe o gráfico de movimentação de produtos
        private void GraficoMovimentacaoProdutos()
        {
            if (DadosCache.Tabelas.TryGetValue("Movimentacoes", out List<object>? movimentacoes))
            {
                var movimentacao = movimentacoes
                    .GroupBy(m => new { ((MovimentacaoData)m).ProdutoId, ((MovimentacaoData)m).Tipo })
                    .Select(g => new { g.Key.ProdutoId, g.Key.Tipo, Quantidade = g.Sum(m => ((MovimentacaoData)m).Quantidade) })
                    .ToList();

                var entradas = movimentacao.Where(m => m.Tipo == "Entrada").OrderByDescending(m => m.Quantidade).Take(5).ToList();
                var saidas = movimentacao.Where(m => m.Tipo == "Saída").OrderByDescending(m => m.Quantidade).Take(5).ToList();

                MovimentacaoProdutosSeries.Clear();

                MovimentacaoProdutosSeries.Add(new ColumnSeries
                {
                    Title = "Entradas",
                    Values = new ChartValues<int>(entradas.Select(e => e.Quantidade)),
                    Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Verde
                    ColumnPadding = 10,
                    MaxColumnWidth = 100
                });

                MovimentacaoProdutosSeries.Add(new ColumnSeries
                {
                    Title = "Saídas",
                    Values = new ChartValues<int>(saidas.Select(s => s.Quantidade)),
                    Fill = new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Vermelho
                    ColumnPadding = 10,
                    MaxColumnWidth = 100
                });

                ProdutosLabels = entradas.Select(e => e.ProdutoId).Union(saidas.Select(s => s.ProdutoId)).Distinct().ToArray();
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

                foreach (var item in maiorMovimentacao)
                {
                    ProdutosMaiorMovimentacaoSeries.Add(new PieSeries
                    {
                        Title = item.ProdutoId,
                        Values = new ChartValues<int> { item.Quantidade },
                        Fill = ObterCorProduto(item.ProdutoId),
                        DataLabels = true
                    });
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
        private void GraficoLucroMensal(string? mesSelecionado)
        {
            if (DadosCache.Tabelas.TryGetValue("Movimentacoes", out List<object>? movimentacoes))
            {
                var lucroMensal = movimentacoes
                    .Where(m => ((MovimentacaoData)m).Tipo == "Saída")
                    .GroupBy(m => ((MovimentacaoData)m).DataHora.ToString("MMM/yyyy"))
                    .Select(g => new { Mes = g.Key, Lucro = g.Sum(m => ((MovimentacaoData)m).Quantidade * ((MovimentacaoData)m).Preço) })
                    .OrderBy(l => DateTime.ParseExact(l.Mes, "MMM/yyyy", null))
                    .ToList();

                if (!string.IsNullOrEmpty(mesSelecionado) && mesSelecionado != "Sem filtros")
                {
                    var dataSelecionada = DateTime.ParseExact(mesSelecionado, "MMM/yyyy", null);
                    lucroMensal = lucroMensal.Where(l => DateTime.ParseExact(l.Mes, "MMM/yyyy", null) <= dataSelecionada).ToList();
                }

                LucroMensalSeries.Clear();

                LucroMensalSeries.Add(new LineSeries
                {
                    Title = "Lucro Mensal Acumulado",
                    Values = new ChartValues<double>(lucroMensal.Select(l => l.Lucro)),
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 10,
                    Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243)) // Azul
                });

                MesesLabels = lucroMensal.Select(l => l.Mes).ToArray();
            }
            else
            {
                Console.WriteLine("Tabela 'Movimentacoes' não encontrada no cache de dados.");
            }

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


        private (byte, byte, byte) HslToRgb(double h, double s, double l)
        {
            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = l - c / 2;
            double r = 0, g = 0, b = 0;

            if (h >= 0 && h < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (h >= 60 && h < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (h >= 120 && h < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (h >= 180 && h < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (h >= 240 && h < 300)
            {
                r = x; g = 0; b = c;
            }
            else if (h >= 300 && h < 360)
            {
                r = c; g = 0; b = x;
            }

            byte R = (byte)((r + m) * 255);
            byte G = (byte)((g + m) * 255);
            byte B = (byte)((b + m) * 255);

            return (R, G, B);
        }
    }
}
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
using Separator = LiveCharts.Wpf.Separator;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class DashboardUserControl : UserControl
    {
        // Variáveis necessárias para os gráficos
        public SeriesCollection EstoqueMarcasSeries { get; set; } = new SeriesCollection();
        public SeriesCollection MovimentacaoProdutosSeries { get; set; } = new SeriesCollection();
        public SeriesCollection HistoricoMovimentacaoSeries { get; set; } = new SeriesCollection();
        public SeriesCollection ProdutosMaiorMovimentacaoSeries { get; set; } = new SeriesCollection();
        public SeriesCollection LucroMensalSeries { get; set; } = new SeriesCollection();
        public SeriesCollection ProdutosVendidosSeries { get; set; } = new SeriesCollection();
        public string[] MarcasLabels { get; set; } = Array.Empty<string>();
        public string[] ProdutosLabels { get; set; } = Array.Empty<string>();
        public string[] PeriodoLabels { get; set; } = Array.Empty<string>();
        public string[] MesesLabels { get; set; } = Array.Empty<string>();
        public Func<double, string> FormatadorDeEixoY { get; set; } = value => value.ToString("N");

        // Formatadores para valores monetários e numéricos
        public Func<double, string> FormatadorMonetario { get; set; } = value => 
            string.Format(System.Globalization.CultureInfo.GetCultureInfo("pt-BR"), "R$ {0:N2}", value);
        public Func<double, string> FormatadorNumerico { get; set; } = value => value.ToString("N0");

        // Propriedade para armazenar os logs
        public ObservableCollection<object> Logs { get; set; } = new ObservableCollection<object>();

        // Dicionário para armazenar as cores dos produtos
        private Dictionary<string, SolidColorBrush> produtoCores = new Dictionary<string, SolidColorBrush>();

        public DashboardUserControl()
        {
            InitializeComponent();
            DataContext = this;
            Setup();
        }

        // Função para carregar os dados do dashboard
        private void Setup()
        {
            // Carrega ComboBoxes - seleção de gráfico
            CarregarComboBox();

            // Define o botão "Semanal" como selecionado inicialmente
            AtualizarEstiloBotoes(SemanalButton);
            
            // Garante que o primeiro item do ComboBox esteja selecionado
            GraficoComboBox.SelectedIndex = 0;

            // Seleciona o período inicial
            string periodoInicial = "Última Semana";

            // Carrega todos os dados necessários para os gráficos
            CarregarGraficos(periodoInicial);

            // Força a atualização do gráfico selecionado com o período inicial
            if (GraficoComboBox.SelectedItem is ComboBoxItem item && item.Content != null)
            {
                AtualizarGrafico(item.Content.ToString(), periodoInicial);
            }
            
            // Atualiza o DataContext para garantir que as alterações sejam refletidas na UI
            DataContext = null;
            DataContext = this;
        }

        // Exibe os gráficos do dashboard
        private void CarregarGraficos(string periodo)
        {
            GraficoMovimentacaoProdutos(periodo);
            GraficoHistoricoMovimentacao(periodo);
            GraficoProdutosMaiorMovimentacao(periodo);
            GraficoLucro(periodo);
            GraficoEstoqueMarcas(periodo);
        }

        // Função para carregar os ComboBox de seleção de gráfico
        private void CarregarComboBox()
        {
            GraficoComboBox.Items.Clear();

            GraficoComboBox.Items.Add(new ComboBoxItem { Content = "Movimentação de Produtos" });
            GraficoComboBox.Items.Add(new ComboBoxItem { Content = "Histórico de Movimentação" });
            GraficoComboBox.Items.Add(new ComboBoxItem { Content = "Produtos com Maior Movimentação" });
            GraficoComboBox.Items.Add(new ComboBoxItem { Content = "Lucro" });
            GraficoComboBox.Items.Add(new ComboBoxItem { Content = "Marcas com Maior Estoque" });

            GraficoComboBox.SelectedIndex = 0;
        }

        // Gráficos
        // Exibe a movimentação de produtos de forma concisa e intuitiva
        private void GraficoMovimentacaoProdutos(string periodo)
        {
            if (DatabaseConnect.Database == null)
            {
                MessageBox.Show("Erro ao conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var collection = DatabaseConnect.Database.GetCollection<MovimentacaoData>("movimentacoes");
            var movimentacoes = collection.FindAll().ToList();

            DateTime dataInicio = ObterDataInicio(periodo);

            // Filtrar e agrupar movimentações de forma mais direta
            var movimentacao = movimentacoes
                .Where(m => ((MovimentacaoData)m).Data >= dataInicio)
                .GroupBy(m => new
                {
                    ProdutoId = ((MovimentacaoData)m).ProdutoId,
                    ProdutoNome = ((MovimentacaoData)m).ProdutoNome,
                    Tipo = ((MovimentacaoData)m).Tipo
                })
                .Select(g => new
                {
                    g.Key.ProdutoId,
                    g.Key.ProdutoNome,
                    g.Key.Tipo,
                    Quantidade = g.Sum(m => ((MovimentacaoData)m).Quantidade),
                    Valor = g.Sum(m => ((MovimentacaoData)m).Quantidade * ((MovimentacaoData)m).Preco)
                })
                .ToList();

            // Obter os produtos com maior movimentação (top 5)
            var entradas = movimentacao.Where(m => m.Tipo == "Entrada").OrderByDescending(m => m.Quantidade).Take(5).ToList();
            var saidas = movimentacao.Where(m => m.Tipo == "Saída").OrderByDescending(m => m.Quantidade).Take(5).ToList();

            // Calcular totais para contextualização
            int totalEntradas = entradas.Sum(e => e.Quantidade);
            int totalSaidas = saidas.Sum(s => s.Quantidade);
            double valorTotalEntradas = entradas.Sum(e => e.Valor);
            double valorTotalSaidas = saidas.Sum(s => s.Valor);

            MovimentacaoProdutosSeries.Clear();

            // Gradiente para entradas - visual mais agradável
            var entradasGradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };
            entradasGradient.GradientStops.Add(new GradientStop(Color.FromArgb(230, 76, 175, 80), 0));
            entradasGradient.GradientStops.Add(new GradientStop(Color.FromArgb(160, 76, 175, 80), 1));

            // Série para entradas com informações contextuais
            MovimentacaoProdutosSeries.Add(new ColumnSeries
            {
                Title = $"Entradas (Total: {totalEntradas} un. | R$ {valorTotalEntradas:N2})",
                Values = new ChartValues<int>(entradas.Select(e => e.Quantidade)),
                Fill = entradasGradient,
                ColumnPadding = 10,
                MaxColumnWidth = 80,
                DataLabels = true,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                LabelPoint = point =>
                {
                    var entrada = entradas[(int)point.X];
                    double porcentagem = Math.Round((double)entrada.Quantidade / totalEntradas * 100, 1);
                    return $"{entrada.Quantidade} un.\n({porcentagem}%)";
                }
            });

            // Gradiente para saídas - visual mais agradável
            var saidasGradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };
            saidasGradient.GradientStops.Add(new GradientStop(Color.FromArgb(230, 244, 67, 54), 0));
            saidasGradient.GradientStops.Add(new GradientStop(Color.FromArgb(160, 244, 67, 54), 1));

            // Série para saídas com informações contextuais
            MovimentacaoProdutosSeries.Add(new ColumnSeries
            {
                Title = $"Saídas (Total: {totalSaidas} un. | R$ {valorTotalSaidas:N2})",
                Values = new ChartValues<int>(saidas.Select(s => s.Quantidade)),
                Fill = saidasGradient,
                ColumnPadding = 10,
                MaxColumnWidth = 80,
                DataLabels = true,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                LabelPoint = point =>
                {
                    var saida = saidas[(int)point.X];
                    double porcentagem = Math.Round((double)saida.Quantidade / totalSaidas * 100, 1);
                    return $"{saida.Quantidade} un.\n({porcentagem}%)";
                }
            });

            // Simplificar labels com tooltips para detalhes completos
            ProdutosLabels = entradas
                .Select((e, i) => $"{(i + 1)}. {TruncateNome(e.ProdutoNome)}")
                .Union(saidas
                    .Select((s, i) => $"{(i + 1)}. {TruncateNome(s.ProdutoNome)}"))
                .Distinct()
                .ToArray();

            DataContext = this;

            // Configurar tooltips para mostrar detalhes completos dos produtos
            if (GraficoContentControl.Content is CartesianChart chart)
            {
                chart.DataTooltip = new DefaultTooltip
                {
                    SelectionMode = TooltipSelectionMode.SharedXValues,
                    ShowTitle = true,
                    ShowSeries = true,
                    Background = (Brush)FindResource("AccentBrush"),
                    Foreground = Brushes.White,
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(1),
                    FontSize = 14
                };

                // Melhorar separador de eixo para melhor legibilidade
                chart.AxisX[0].Separator = new Separator
                {
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 3 },
                    Stroke = new SolidColorBrush(Color.FromArgb(30, 128, 128, 128))
                };
            }
        }

        // Método auxiliar para truncar nomes de produtos de forma mais inteligente
        private string TruncateNome(string nome)
        {
            if (string.IsNullOrEmpty(nome)) return "Sem Nome";
            if (nome.Length <= 12) return nome;

            // Tentar truncar em espaços ou hífens para manter palavras completas
            int pos = nome.LastIndexOf(' ', 11);
            if (pos <= 0) pos = nome.LastIndexOf('-', 11);

            return pos > 0 ? nome.Substring(0, pos) + "..." : nome.Substring(0, 10) + "...";
        }

        // Exibe o histórico de movimentação
        // Útil para identificar a movimentação de produtos ao longo do tempo
        private void GraficoHistoricoMovimentacao(string periodo)
        {
            if (DatabaseConnect.Database == null)
            {
                MessageBox.Show("Erro ao conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var collection = DatabaseConnect.Database.GetCollection<MovimentacaoData>("movimentacoes");
            var movimentacoes = collection.FindAll().ToList();
            DateTime dataInicio = ObterDataInicio(periodo);

            // Formato de data adaptado ao período para melhor legibilidade
            Func<DateTime, string> formatadorData = periodo switch
            {
                "Hoje" => d => d.ToString("HH:00") + "h",
                "Última Semana" => d => d.ToString("ddd\ndd/MM"), // Adiciona quebra de linha para melhor visualização
                "Último Mês" => d => d.ToString("dd/MM"),
                "Último Ano" => d => d.ToString("MMM/yy"),
                _ => d => d.ToString("dd/MM")
            };

            // Agrupar dados de forma mais eficiente
            var dadosAgrupados = movimentacoes
                .Where(m => ((MovimentacaoData)m).Data >= dataInicio)
                .GroupBy(m => new
                {
                    Data = periodo == "Hoje"
                        ? new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, ((MovimentacaoData)m).Data.Hour, 0, 0)
                        : ((MovimentacaoData)m).Data.Date,
                    Tipo = ((MovimentacaoData)m).Tipo
                })
                .Select(g => new
                {
                    g.Key.Data,
                    g.Key.Tipo,
                    Quantidade = g.Sum(m => ((MovimentacaoData)m).Quantidade),
                    Valor = g.Sum(m => ((MovimentacaoData)m).Quantidade * ((MovimentacaoData)m).Preco)
                })
                .ToList();

            // Separar em entradas e saídas
            var entradas = dadosAgrupados.Where(d => d.Tipo == "Entrada").ToDictionary(d => d.Data, d => d);
            var saidas = dadosAgrupados.Where(d => d.Tipo == "Saída").ToDictionary(d => d.Data, d => d);

            // Obter todas as datas ordenadas
            var todasDatas = dadosAgrupados
                .Select(d => d.Data)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            // Preparar dados para o gráfico
            HistoricoMovimentacaoSeries.Clear();
            var datas = new List<string>();
            var entradasValues = new ChartValues<int>();
            var saidasValues = new ChartValues<int>();
            var saldoValues = new ChartValues<int>();
            var valoresEntradasSeries = new ChartValues<double>();
            var valoresSaidasSeries = new ChartValues<double>();

            int saldoAcumulado = 0;
            double totalValorEntradas = 0;
            double totalValorSaidas = 0;
            int totalQuantidadeEntradas = 0;
            int totalQuantidadeSaidas = 0;

            // Calcular os valores para cada data
            foreach (var data in todasDatas)
            {
                datas.Add(formatadorData(data));

                // Obter valores ou usar zero como padrão
                var entrada = entradas.ContainsKey(data) ? entradas[data] : null;
                var saida = saidas.ContainsKey(data) ? saidas[data] : null;

                int qtdEntrada = entrada?.Quantidade ?? 0;
                int qtdSaida = saida?.Quantidade ?? 0;
                double valorEntrada = entrada?.Valor ?? 0;
                double valorSaida = saida?.Valor ?? 0;

                // Acumular totais
                totalQuantidadeEntradas += qtdEntrada;
                totalQuantidadeSaidas += qtdSaida;
                totalValorEntradas += valorEntrada;
                totalValorSaidas += valorSaida;
                saldoAcumulado += (qtdEntrada - qtdSaida);

                // Adicionar valores às séries
                entradasValues.Add(qtdEntrada);
                saidasValues.Add(qtdSaida);
                saldoValues.Add(saldoAcumulado);
                valoresEntradasSeries.Add(valorEntrada);
                valoresSaidasSeries.Add(valorSaida);
            }

            // Média diária para contextualização
            double mediaEntradas = todasDatas.Count > 0 ? totalQuantidadeEntradas / (double)todasDatas.Count : 0;
            double mediaSaidas = todasDatas.Count > 0 ? totalQuantidadeSaidas / (double)todasDatas.Count : 0;

            // Gradiente para entradas (verde)
            var entradasGradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };
            entradasGradient.GradientStops.Add(new GradientStop(Color.FromArgb(180, 76, 175, 80), 0));
            entradasGradient.GradientStops.Add(new GradientStop(Color.FromArgb(40, 76, 175, 80), 1));

            // Série para entradas com informações mais contextualizadas
            HistoricoMovimentacaoSeries.Add(new LineSeries
            {
                Title = $"Entradas (Total: {totalQuantidadeEntradas} | R$ {totalValorEntradas:N2})",
                Values = entradasValues,
                PointGeometry = DefaultGeometries.Diamond,
                PointGeometrySize = 8,
                PointForeground = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Stroke = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                StrokeThickness = 3,
                Fill = entradasGradient,
                LineSmoothness = 0.7,
                DataLabels = periodo == "Hoje" || periodo == "Última Semana", // Mostrar labels apenas em períodos curtos
                LabelPoint = point => $"{entradasValues[(int)point.X]}"
            });

            // Gradiente para saídas (vermelho)
            var saidasGradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };
            saidasGradient.GradientStops.Add(new GradientStop(Color.FromArgb(180, 244, 67, 54), 0));
            saidasGradient.GradientStops.Add(new GradientStop(Color.FromArgb(40, 244, 67, 54), 1));

            // Série para saídas com informações mais contextualizadas
            HistoricoMovimentacaoSeries.Add(new LineSeries
            {
                Title = $"Saídas (Total: {totalQuantidadeSaidas} | R$ {totalValorSaidas:N2})",
                Values = saidasValues,
                PointGeometry = DefaultGeometries.Square,
                PointGeometrySize = 8,
                PointForeground = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                Stroke = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                StrokeThickness = 3,
                Fill = saidasGradient,
                LineSmoothness = 0.7,
                DataLabels = periodo == "Hoje" || periodo == "Última Semana", // Mostrar labels apenas em períodos curtos
                LabelPoint = point => $"{saidasValues[(int)point.X]}"
            });

            // Série para saldo acumulado com detalhes sobre situação do estoque
            string statusSaldo = saldoAcumulado > 0 ? "positivo" : (saldoAcumulado < 0 ? "negativo" : "neutro");
            HistoricoMovimentacaoSeries.Add(new LineSeries
            {
                Title = $"Saldo: {saldoAcumulado} ({statusSaldo})",
                Values = saldoValues,
                PointGeometry = DefaultGeometries.Circle,
                PointGeometrySize = 10,
                Stroke = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                StrokeThickness = 4,
                Fill = Brushes.Transparent,
                LineSmoothness = 0,
                ScalesYAt = 1, // Usar eixo Y secundário
                DataLabels = true,
                LabelPoint = point => $"{saldoValues[(int)point.X]}"
            });

            PeriodoLabels = datas.ToArray();
            DataContext = this;

            // TODO: Resolver

            //// Configurar o gráfico com melhorias visuais
            //if (GraficoContentControl.Content is CartesianChart chart)
            //{
            //    // Eixo Y secundário para saldo
            //    chart.AxisY.Add(new Axis
            //    {
            //        Title = $"Saldo (Média diária: {mediaEntradas - mediaSaidas:N1})",
            //        LabelFormatter = value => value.ToString("N0"),
            //        Position = AxisPosition.RightTop,
            //        Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
            //        FontWeight = FontWeights.Bold,
            //        MinValue = saldoValues.Min() < 0 ? saldoValues.Min() * 1.1 : 0 // Ajusta o eixo para valores negativos
            //    });

            //    // Linha de referência no zero
            //    chart.AxisY[0].Sections = new SectionsCollection
            //    {
            //        new AxisSection
            //        {
            //            Value = 0,
            //            SectionWidth = 0.5,
            //            Stroke = new SolidColorBrush(Color.FromArgb(80, 128, 128, 128))
            //        }
            //    };

            //    // Tooltip aprimorado com o estilo comum
            //    chart.DataTooltip = new DefaultTooltip
            //    {
            //        SelectionMode = TooltipSelectionMode.SharedXValues,
            //        ShowTitle = true,
            //        ShowSeries = true,
            //        Background = (Brush)FindResource("AccentBrush"),
            //        Foreground = Brushes.White,
            //        BorderBrush = Brushes.White,
            //        BorderThickness = new Thickness(1),
            //        FontSize = 14
            //    };
            //}
        }

        // Exibe produtos com maior movimentação
        // Útil para identificar os produtos mais vendidos
        private void GraficoProdutosMaiorMovimentacao(string periodo)
        {
            if (DatabaseConnect.Database == null)
            {
                MessageBox.Show("Erro ao conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var collection = DatabaseConnect.Database.GetCollection<MovimentacaoData>("movimentacoes");
            var movimentacoes = collection.FindAll().ToList();

            DateTime dataInicio = ObterDataInicio(periodo);

            // Agrupar dados de forma mais eficiente e calcular métricas relevantes
            var maiorMovimentacao = movimentacoes
                .Where(m => ((MovimentacaoData)m).Data >= dataInicio)
                .GroupBy(m => new
                {
                    ProdutoId = ((MovimentacaoData)m).ProdutoId,
                    ProdutoNome = ((MovimentacaoData)m).ProdutoNome
                })
                .Select(g => new
                {
                    g.Key.ProdutoId,
                    g.Key.ProdutoNome,
                    Quantidade = g.Sum(m => ((MovimentacaoData)m).Quantidade),
                    Valor = g.Sum(m => ((MovimentacaoData)m).Quantidade * ((MovimentacaoData)m).Preco),
                    MediaUnitaria = g.Sum(m => ((MovimentacaoData)m).Quantidade * ((MovimentacaoData)m).Preco) /
                                   g.Sum(m => ((MovimentacaoData)m).Quantidade)
                })
                .OrderByDescending(m => m.Quantidade)
                .Take(8)
                .ToList();

            ProdutosMaiorMovimentacaoSeries.Clear();

            // Cálculos para contextualização
            double totalQtd = maiorMovimentacao.Sum(m => m.Quantidade);
            double totalValor = maiorMovimentacao.Sum(m => m.Valor);

            // Criar série com dados mais contextualizados
            foreach (var item in maiorMovimentacao)
            {
                // Truncar nomes longos para melhor visualização
                string nomeProduto = !string.IsNullOrEmpty(item.ProdutoNome) ?
                    (item.ProdutoNome.Length > 20 ? item.ProdutoNome.Substring(0, 17) + "..." : item.ProdutoNome) :
                    item.ProdutoId;

                double porcentagem = Math.Round((item.Quantidade / totalQtd) * 100, 1);
                double porcentagemValor = Math.Round((item.Valor / totalValor) * 100, 1);

                // Criar série com visual aprimorado
                ProdutosMaiorMovimentacaoSeries.Add(new PieSeries
                {
                    Title = nomeProduto,
                    Values = new ChartValues<double> { item.Quantidade },
                    Fill = ObterCorProduto(item.ProdutoId),
                    DataLabels = true,
                    // Mostrar apenas informações essenciais no label
                    LabelPoint = point => porcentagem >= 5 ?
                        $"{porcentagem}%" :
                        "",
                    FontSize = 12,
                    FontWeight = porcentagem > 15 ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = Brushes.White,
                    LabelPosition = PieLabelPosition.OutsideSlice,
                    PushOut = porcentagem > 10 ? 10 : 0,
                    // Tooltip com todas as informações detalhadas
                    ToolTip = $"{nomeProduto}\n" +
                             $"Quantidade: {item.Quantidade:N0} un. ({porcentagem}%)\n" +
                             $"Valor: R$ {item.Valor:N2} ({porcentagemValor}%)\n" +
                             $"Média: R$ {item.MediaUnitaria:N2}/un"
                });
            }

            // Armazenar labels para referência
            ProdutosLabels = maiorMovimentacao
                .Select(m => !string.IsNullOrEmpty(m.ProdutoNome) ? m.ProdutoNome : m.ProdutoId)
                .ToArray();

            // Atualizar contexto para refletir mudanças na UI
            DataContext = this;

            // Configurar visualização aprimorada para o gráfico
            if (GraficoContentControl.Content is PieChart pieChart)
            {
                pieChart.LegendLocation = LegendLocation.Right;
                pieChart.InnerRadius = 0.2;
                pieChart.HoverPushOut = 7;
                pieChart.AnimationsSpeed = TimeSpan.FromMilliseconds(400);
                pieChart.Hoverable = true;
                
                // Garantir que o tooltip tenha o estilo padrão
                pieChart.DataTooltip = new DefaultTooltip
                {
                    ShowTitle = true,
                    ShowSeries = true,
                    Background = (Brush)FindResource("AccentBrush"),
                    Foreground = Brushes.White,
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(1),
                    FontSize = 14
                };
            }
        }

        // Exibe o gráfico de lucro
        // Útil para identificar o lucro ao longo do tempo
        private void GraficoLucro(string periodo)
        {
            if (DatabaseConnect.Database == null)
            {
                MessageBox.Show("Erro ao conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var movimentacoesCollection = DatabaseConnect.Database.GetCollection<MovimentacaoData>("movimentacoes");
            var movimentacoes = movimentacoesCollection.FindAll().ToList();

            DateTime dataInicio = ObterDataInicio(periodo);

            // Agrupamento conforme o período com formatação mais intuitiva
            Func<DateTime, string> agrupador = periodo switch
            {
                "Hoje" => d => d.ToString("HH:00") + "h",
                "Última Semana" => d => d.ToString("ddd, dd/MM"), // Inclui nome do dia
                "Último Mês" => d => "Dia " + d.ToString("dd/MM"),
                "Último Ano" => d => d.ToString("MMM/yyyy"),
                _ => d => d.ToString("dd/MM/yy")
            };

            var receitas = movimentacoes
                .Where(m => ((MovimentacaoData)m).Tipo == "Saída" && ((MovimentacaoData)m).Data >= dataInicio)
                .GroupBy(m => agrupador(((MovimentacaoData)m).Data))
                .Select(g => new {
                    Periodo = g.Key,
                    Valor = g.Sum(m => ((MovimentacaoData)m).Quantidade * ((MovimentacaoData)m).Preco)
                })
                .ToDictionary(r => r.Periodo, r => r.Valor);

            var despesas = movimentacoes
                .Where(m => ((MovimentacaoData)m).Tipo == "Entrada" && ((MovimentacaoData)m).Data >= dataInicio)
                .GroupBy(m => agrupador(((MovimentacaoData)m).Data))
                .Select(g => new {
                    Periodo = g.Key,
                    Valor = g.Sum(m => ((MovimentacaoData)m).Quantidade * ((MovimentacaoData)m).Preco)
                })
                .ToDictionary(d => d.Periodo, d => d.Valor);

            var periodos = receitas.Keys.Union(despesas.Keys).OrderBy(p => {
                if (periodo == "Último Ano")
                    return DateTime.ParseExact(p, "MMM/yyyy", null);
                else if (periodo == "Hoje")
                    return DateTime.ParseExact(p.TrimEnd('h'), "HH:00", null);
                else if (periodo == "Última Semana")
                    return DateTime.ParseExact(p.Substring(p.IndexOf(',') + 2), "dd/MM", null);
                else
                    return DateTime.ParseExact(p.Contains("Dia ") ? p.Substring(4) : p, "dd/MM", null);
            }).ToList();

            var receitasSeries = new ChartValues<double>();
            var despesasSeries = new ChartValues<double>();
            var lucroSeries = new ChartValues<double>();
            var lucroPorcentagemSeries = new ChartValues<double>();
            double lucroTotalPeriodo = 0;

            foreach (var p in periodos)
            {
                double receitaValor = receitas.ContainsKey(p) ? receitas[p] : 0;
                double despesaValor = despesas.ContainsKey(p) ? despesas[p] : 0;
                double lucroValor = receitaValor - despesaValor;
                lucroTotalPeriodo += lucroValor;

                receitasSeries.Add(receitaValor);
                despesasSeries.Add(despesaValor);
                lucroSeries.Add(lucroValor);

                // Calcula a porcentagem de lucro
                double porcentagemLucro = despesaValor > 0 ? ((receitaValor - despesaValor) / despesaValor) * 100 : 0;
                lucroPorcentagemSeries.Add(porcentagemLucro);
            }

            LucroMensalSeries.Clear();

            // Receitas (verde com gradiente e mais transparente)
            var receitaGradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };
            receitaGradient.GradientStops.Add(new GradientStop(Color.FromArgb(180, 76, 175, 80), 0));
            receitaGradient.GradientStops.Add(new GradientStop(Color.FromArgb(40, 76, 175, 80), 1));

            LucroMensalSeries.Add(new StackedAreaSeries
            {
                Title = "Receitas",
                Values = receitasSeries,
                LineSmoothness = 0.6,
                Fill = receitaGradient,
                Stroke = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                StrokeThickness = 3,
                PointGeometry = null
            });

            // Despesas (vermelho com gradiente e mais transparente)
            var despesaGradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };
            despesaGradient.GradientStops.Add(new GradientStop(Color.FromArgb(180, 244, 67, 54), 0));
            despesaGradient.GradientStops.Add(new GradientStop(Color.FromArgb(40, 244, 67, 54), 1));

            LucroMensalSeries.Add(new StackedAreaSeries
            {
                Title = "Despesas",
                Values = despesasSeries,
                LineSmoothness = 0.6,
                Fill = despesaGradient,
                Stroke = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                StrokeThickness = 3,
                PointGeometry = null
            });

            // Linha de Lucro (azul mais vibrante)
            LucroMensalSeries.Add(new LineSeries
            {
                Title = $"Lucro (Total: R$ {lucroTotalPeriodo:N2})",
                Values = lucroSeries,
                PointGeometry = DefaultGeometries.Diamond,
                PointGeometrySize = 15,
                Stroke = new SolidColorBrush(Color.FromRgb(0, 119, 255)),
                StrokeThickness = 4,
                Fill = Brushes.Transparent,
                DataLabels = true,
                LabelPoint = point =>
                {
                    double valor = point.Y;
                    double porcentagem = lucroPorcentagemSeries[(int)point.X];
                    string sinal = valor >= 0 ? "+" : "";
                    return $"{sinal}R$ {valor:N0}\n({sinal}{porcentagem:N1}%)";
                }
            });

            MesesLabels = periodos.ToArray();

            DataContext = this;

            // Atualizar o cartesiano para incluir as melhorias visuais
            if (GraficoContentControl.Content is CartesianChart chart)
            {
                // Configurar eixo Y para melhor visualização dos valores negativos
                var axisY = chart.AxisY[0];
                axisY.Separator = new Separator
                {
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 3 },
                    Stroke = new SolidColorBrush(Color.FromArgb(64, 128, 128, 128))
                };

                // Garantir que o zero esteja visível e adicionar linha de referência
                if (lucroSeries.Any(v => v < 0))
                {
                    double minValue = lucroSeries.Min() * 1.1;
                    axisY.MinValue = minValue;
                }
                
                // Definir as seções do eixo para destacar valores negativos e positivos
                axisY.Sections = new SectionsCollection
                {
                    new AxisSection
                    {
                        Value = 0,
                        SectionWidth = 0.5,
                        Stroke = new SolidColorBrush(Color.FromArgb(128, 128, 128, 128)),
                        StrokeThickness = 2
                    }
                };
                
                // Garantir que o tooltip tenha o estilo padrão
                chart.DataTooltip = new DefaultTooltip
                {
                    SelectionMode = TooltipSelectionMode.SharedXValues,
                    ShowTitle = true,
                    ShowSeries = true,
                    Background = (Brush)FindResource("AccentBrush"),
                    Foreground = Brushes.White,
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(1),
                    FontSize = 14
                };
            }
        }

        // Exibe o gráfico de barras com as marcas de maior estoque
        // Útil para identificar as marcas com maior quantidade de produtos em estoque
        private void GraficoEstoqueMarcas(string periodo)
        {
            if (DatabaseConnect.Database == null)
            {
                MessageBox.Show("Erro ao conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var produtosCollection = DatabaseConnect.Database.GetCollection<ProdutoData>("produtos");
            var produtos = produtosCollection.FindAll().ToList();

            // Agrupar por marca com informações essenciais
            var marcasData = produtos
                .GroupBy(p => p.Marca)
                .Select(g => new
                {
                    Marca = string.IsNullOrEmpty(g.Key) ? "Sem Marca" : g.Key,
                    Quantidade = g.Sum(p => p.Quantidade),
                    Valor = g.Sum(p => p.Quantidade * p.Preco),
                    NumProdutos = g.Count(),
                    MediaPreco = g.Sum(p => p.Quantidade * p.Preco) / g.Sum(p => p.Quantidade)
                })
                .OrderByDescending(m => m.Quantidade)
                .Take(8)
                .ToList();

            // Calcular totais para porcentagens
            int quantidadeTotal = marcasData.Sum(m => m.Quantidade);
            double valorTotal = marcasData.Sum(m => m.Valor);

            EstoqueMarcasSeries.Clear();

            // Criar gradiente para as barras
            var barGradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0)
            };
            barGradient.GradientStops.Add(new GradientStop(Color.FromRgb(33, 150, 243), 0));
            barGradient.GradientStops.Add(new GradientStop(Color.FromRgb(3, 169, 244), 1));

            // Usar barras horizontais com visual aprimorado
            var quantidadeSeries = new RowSeries
            {
                Title = $"Estoque por Marca (Total: {quantidadeTotal} unidades)",
                Values = new ChartValues<int>(marcasData.Select(m => m.Quantidade)),
                Fill = barGradient,
                Stroke = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                StrokeThickness = 1,
                RowPadding = 8,
                DataLabels = true,
                LabelPoint = point => {
                    var marca = marcasData[(int)point.Y];
                    double porcentagem = Math.Round(((double)marca.Quantidade / quantidadeTotal) * 100, 1);
                    return $"{marca.Quantidade} un. ({porcentagem}%)\nR$ {marca.Valor:N2}";
                },
                Foreground = (Brush)FindResource("TextBrush")
            };

            // Adicionar informação de valor em outra série
            var valorSeries = new RowSeries
            {
                Title = $"Valor em Estoque (Total: R$ {valorTotal:N2})",
                Values = new ChartValues<double>(marcasData.Select(m => m.Valor)),
                Fill = new SolidColorBrush(Color.FromArgb(120, 76, 175, 80)),
                Stroke = new SolidColorBrush(Color.FromRgb(56, 142, 60)),
                StrokeThickness = 1,
                RowPadding = 5,
                DataLabels = false
            };

            EstoqueMarcasSeries.Add(quantidadeSeries);
            EstoqueMarcasSeries.Add(valorSeries);

            // Adicionar marcas com informações extras
            MarcasLabels = marcasData.Select(m => $"{m.Marca} ({m.NumProdutos} {(m.NumProdutos == 1 ? "produto" : "produtos")})").ToArray();

            DataContext = this;

            // Atualizar o cartesiano para incluir as melhorias visuais
            if (GraficoContentControl.Content is CartesianChart chart)
            {
                // Linhas de grade mais suaves e espaçadas
                chart.AxisX[0].Separator = new Separator
                {
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 3 },
                    Stroke = new SolidColorBrush(Color.FromArgb(30, 30, 30, 30))
                };
                
                // Garantir que o tooltip tenha o estilo padrão
                chart.DataTooltip = new DefaultTooltip
                {
                    SelectionMode = TooltipSelectionMode.SharedXValues,
                    ShowTitle = true,
                    ShowSeries = true,
                    Background = (Brush)FindResource("AccentBrush"),
                    Foreground = Brushes.White,
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(1),
                    FontSize = 14
                };
            }
        }

        // Função para obter a data de início com base no período selecionado
        private DateTime ObterDataInicio(string periodo)
        {
            return periodo switch
            {
                "Hoje" => DateTime.Today,
                "Última Semana" => DateTime.Today.AddDays(-7),
                "Último Mês" => DateTime.Today.AddMonths(-1),
                "Último Ano" => DateTime.Today.AddYears(-1),
                _ => DateTime.MinValue
            };
        }

        // Evento de seleção do ComboBox para gráficos
        private void GraficoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem is ComboBoxItem graficoSelecionado && graficoSelecionado.Content != null)
            {
                var grafico = graficoSelecionado.Content.ToString();
                if (!string.IsNullOrEmpty(grafico))
                {
                    // Obtém o período atualmente selecionado com base no botão ativo
                    string periodoAtual = "Última Semana"; // Valor padrão
                    
                    if (DiarioButton.Style == FindResource("SelectedDashboardButtonStyle"))
                        periodoAtual = "Hoje";
                    else if (SemanalButton.Style == FindResource("SelectedDashboardButtonStyle"))
                        periodoAtual = "Última Semana";
                    else if (MensalButton.Style == FindResource("SelectedDashboardButtonStyle"))
                        periodoAtual = "Último Mês";
                    else if (AnualButton.Style == FindResource("SelectedDashboardButtonStyle"))
                        periodoAtual = "Último Ano";
                        
                    // Atualiza o gráfico com o período atual em vez de reiniciar com "Hoje"
                    AtualizarGrafico(grafico, periodoAtual);
                }
            }
        }

        // Evento de clique para os botões de filtro por período
        private void FiltrarPorPeriodo_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag != null)
            {
                var periodo = button.Tag.ToString();
                var graficoSelecionado = GraficoComboBox.SelectedItem as ComboBoxItem;
                if (graficoSelecionado != null)
                {
                    AtualizarGrafico(graficoSelecionado.Content.ToString(), periodo);
                    AtualizarEstiloBotoes(button);
                }
            }
        }

        // Função para atualizar o estilo dos botões de filtro por período
        private void AtualizarEstiloBotoes(Button botaoSelecionado)
        {
            DiarioButton.Style = (Style)FindResource("DashboardButtonStyle");
            SemanalButton.Style = (Style)FindResource("DashboardButtonStyle");
            MensalButton.Style = (Style)FindResource("DashboardButtonStyle");
            AnualButton.Style = (Style)FindResource("DashboardButtonStyle");

            botaoSelecionado.Style = (Style)FindResource("SelectedDashboardButtonStyle");
        }

        // Função para atualizar o gráfico exibido
        private void AtualizarGrafico(string grafico, string periodo)
        {
            // Configura o título do gráfico
            string titulo = $"{grafico} - {periodo}";

            switch (grafico)
            {
                case "Movimentação de Produtos":
                    GraficoContentControl.Content = new CartesianChart
                    {
                        Series = MovimentacaoProdutosSeries,
                        LegendLocation = LegendLocation.Bottom,
                        Margin = new Thickness(10),
                        Foreground = (Brush)FindResource("TextBrush"),
                        AnimationsSpeed = TimeSpan.FromMilliseconds(350),
                        DisableAnimations = false,
                        Hoverable = true,
                        Height = double.NaN, // Auto height
                        MinHeight = 300,
                        DataTooltip = new DefaultTooltip
                        {
                            Background = (Brush)FindResource("AccentBrush"),
                            Foreground = Brushes.White,
                            BorderBrush = Brushes.White,
                            BorderThickness = new Thickness(1),
                            FontSize = 14,
                            SelectionMode = TooltipSelectionMode.SharedXValues
                        },
                        AxisX = new AxesCollection
                        {
                            new Axis
                            {
                                Title = "Produtos",
                                Labels = ProdutosLabels,
                                LabelsRotation = 45,
                                Foreground = (Brush)FindResource("TextBrush"),
                                MaxWidth = double.PositiveInfinity
                            }
                        },
                        AxisY = new AxesCollection
                        {
                            new Axis
                            {
                                Title = "Quantidade Movimentada",
                                LabelFormatter = FormatadorNumerico,
                                Foreground = (Brush)FindResource("TextBrush")
                            }
                        }
                    };
                    GraficoMovimentacaoProdutos(periodo);
                    break;

                case "Histórico de Movimentação":
                    GraficoContentControl.Content = new CartesianChart
                    {
                        Series = HistoricoMovimentacaoSeries,
                        LegendLocation = LegendLocation.Bottom,
                        Margin = new Thickness(10),
                        Foreground = (Brush)FindResource("TextBrush"),
                        Height = double.NaN, // Auto height
                        MinHeight = 300,
                        DataTooltip = new DefaultTooltip
                        {
                            Background = (Brush)FindResource("AccentBrush"),
                            Foreground = Brushes.White,
                            BorderBrush = Brushes.White,
                            BorderThickness = new Thickness(1),
                            FontSize = 14
                        },
                        AxisX = new AxesCollection
                        {
                            new Axis
                            {
                                Title = "Período",
                                Labels = PeriodoLabels,
                                MaxWidth = double.PositiveInfinity,
                                LabelsRotation = 45,
                                Separator = new Separator { Step = periodo == "Último Ano" ? 2 : 1 }
                            }
                        },
                        AxisY = new AxesCollection
                        {
                            new Axis
                            {
                                Title = "Quantidade Movimentada",
                                LabelFormatter = FormatadorDeEixoY,
                                MinValue = 0
                            }
                        }
                    };
                    GraficoHistoricoMovimentacao(periodo);
                    break;

                case "Produtos com Maior Movimentação":
                    GraficoContentControl.Content = new PieChart
                    {
                        Series = ProdutosMaiorMovimentacaoSeries,
                        LegendLocation = LegendLocation.Bottom,
                        Margin = new Thickness(10),
                        Foreground = (Brush)FindResource("TextBrush"),
                        Height = double.NaN, // Auto height
                        MinHeight = 300,
                        InnerRadius = 10,
                        DataTooltip = new DefaultTooltip
                        {
                            Background = (Brush)FindResource("AccentBrush"),
                            Foreground = Brushes.White,
                            BorderBrush = Brushes.White,
                            BorderThickness = new Thickness(1),
                            FontSize = 14
                        }
                    };
                    GraficoProdutosMaiorMovimentacao(periodo);
                    break;

                case "Lucro":
                    GraficoContentControl.Content = new CartesianChart
                    {
                        Series = LucroMensalSeries,
                        LegendLocation = LegendLocation.Bottom,
                        Margin = new Thickness(10),
                        Foreground = (Brush)FindResource("TextBrush"),
                        Height = double.NaN, // Auto height
                        MinHeight = 300,
                        DataTooltip = new DefaultTooltip
                        {
                            Background = (Brush)FindResource("AccentBrush"),
                            Foreground = Brushes.White,
                            BorderBrush = Brushes.White,
                            BorderThickness = new Thickness(1),
                            FontSize = 14
                        },
                        AxisX = new AxesCollection
                        {
                            new Axis
                            {
                                Title = "Período",
                                Labels = MesesLabels,
                                MaxWidth = double.PositiveInfinity,
                                LabelsRotation = 45,
                                Separator = new Separator { Step = periodo == "Último Ano" ? 2 : 1 }
                            }
                        },
                        AxisY = new AxesCollection
                        {
                            new Axis
                            {
                                Title = "Valor (R$)",
                                LabelFormatter = FormatadorMonetario,
                                MinValue = 0
                            }
                        }
                    };
                    GraficoLucro(periodo);
                    break;

                case "Marcas com Maior Estoque":
                    GraficoContentControl.Content = new CartesianChart
                    {
                        Series = EstoqueMarcasSeries,
                        LegendLocation = LegendLocation.Bottom,
                        Margin = new Thickness(10),
                        Foreground = (Brush)FindResource("TextBrush"),
                        Height = double.NaN, // Auto height
                        MinHeight = 300,
                        DataTooltip = new DefaultTooltip
                        {
                            Background = (Brush)FindResource("AccentBrush"),
                            Foreground = Brushes.White,
                            BorderBrush = Brushes.White,
                            BorderThickness = new Thickness(1),
                            FontSize = 14
                        },
                        AxisY = new AxesCollection
                        {
                            new Axis
                            {
                                Title = "Marcas",
                                Labels = MarcasLabels,
                                MaxWidth = double.PositiveInfinity
                            }
                        },
                        AxisX = new AxesCollection
                        {
                            new Axis
                            {
                                Title = "Quantidade em Estoque",
                                LabelFormatter = FormatadorNumerico,
                                Foreground = (Brush)FindResource("TextBrush")
                            }
                        }
                    };
                    GraficoEstoqueMarcas(periodo);
                    break;
            }
        }

        // Cores
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

        // Função para converter HSL para RGB - melhor para gerar cores vivas
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

        // Função para obter a cor de um produto
        private SolidColorBrush ObterCorProduto(string produtoId)
        {
            if (!produtoCores.ContainsKey(produtoId))
            {
                produtoCores[produtoId] = GerarCorUnica(produtoCores.Count);
            }
            return produtoCores[produtoId];
        }
    }
}
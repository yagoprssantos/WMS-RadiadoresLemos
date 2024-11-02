using LiveCharts;
using LiveCharts.Wpf;
using System.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class DashboardUserControl : UserControl
    {
        // Variáveis necessárias para os gráficos
        public ChartValues<int> ProdutosVendidosSeries { get; set; } = new ChartValues<int>();
        public string[] DiasVendas { get; set; } = Array.Empty<string>();
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
            ExibirGraficoDeVendas();
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

        // Exibe o gráfico de vendas
        private void ExibirGraficoDeVendas()
        {
            ProdutosVendidosSeries = new ChartValues<int> { 15, 30, 50, 40, 45, 60, 70 };
            DiasVendas = new[] { "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom" };
            FormatadorDeEixoY = value => value.ToString("N");

            // Atualiza os bindings
            DataContext = this;
        }

        // Exibe os logs recentes
        private void ExibirLogsRecentes()
        {
            // TODO: Implementar
        }
    }
}

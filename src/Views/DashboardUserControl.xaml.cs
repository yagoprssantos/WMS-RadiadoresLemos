using LiveCharts;
using LiveCharts.Wpf;
using System.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WMS_RadiadoresLemos_WPF.src.Models;
using System.Windows;

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
            NotificarProdutosBaixoEstoque();
        }

        // Exibe o total de usuários
        private void ExibirTotalUsuarios()
        {
            if (Cache.Tabelas.TryGetValue("Usuarios", out List<object>? usuarios))
            {
                int totalUsuarios = usuarios.Count;
                TotalUsuariosTextBlock.Text = totalUsuarios.ToString();
            }
        }

        // Exibe o total de produtos
        private void ExibirTotalProdutos()
        {
            if (Cache.Tabelas.TryGetValue("Produtos", out List<object>? produtos))
            {
                int totalProdutos = produtos.Count;
                TotalProdutosTextBlock.Text = totalProdutos.ToString();
            }
        }

        // Exibe a quantidade de produtos com baixo estoque
        private void ExibirProdutosBaixoEstoque()
        {
            int produtosBaixoEstoque = VerificarProdutosBaixoEstoque();
            ProdutosBaixoEstoqueTextBlock.Text = produtosBaixoEstoque.ToString();
        }

        // Verifica a quantidade de produtos com baixo estoque
        public int VerificarProdutosBaixoEstoque()
        {
            if (Cache.Tabelas.TryGetValue("Produtos", out List<object>? produtos))
            {
                return produtos.Count(p => ((ProdutoData)p).Quantidade < 10);
            }
            return 0;
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

        // Notifica a quantidade de produtos com baixo estoque
        private void NotificarProdutosBaixoEstoque()
        {
            int produtosBaixoEstoque = VerificarProdutosBaixoEstoque();
            if (produtosBaixoEstoque > 0)
            {
                MessageBox.Show($"Existem {produtosBaixoEstoque} produtos com baixo estoque!", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
    
using LiveCharts;
using LiveCharts.Wpf;
using Google.Cloud.Firestore;
using System.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WMS_RadiadoresLemos_WPF.Classes;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class DashboardUserControl : UserControl
    {
        // Variáveis necessárias para os gráficos
        public ChartValues<int> ProdutosVendidosSeries { get; set; } = new ChartValues<int>();
        public string[] DiasVendas { get; set; } = Array.Empty<string>();
        public Func<double, string> FormatadorDeEixoY { get; set; } = value => value.ToString("N");

        private FirestoreDb db = null!;

        public DashboardUserControl()
        {
            InitializeComponent();
            SetupDatabaseConnection();
            CarregarDadosDoDashboardAsync().ConfigureAwait(false);
        }

        private void SetupDatabaseConnection()
        {
            DatabaseConnect.SetEnvironmentVarible();
            db = DatabaseConnect.Database ?? throw new InvalidOperationException("Banco de dados não configurado");
        }

        // Função para carregar os dados do dashboard
        private async Task CarregarDadosDoDashboardAsync()
        {
            var usuariosTask = ExibirTotalUsuariosAsync();
            var produtosTask = ExibirTotalProdutosAsync();
            var baixoEstoqueTask = ExibirProdutosBaixoEstoqueAsync();
            var logsTask = ExibirLogsRecentesAsync();

            await Task.WhenAll(usuariosTask, produtosTask, baixoEstoqueTask, logsTask);

            ExibirGraficoDeVendas();
        }

        private async Task ExibirTotalUsuariosAsync()
        {
            var usuariosRef = db.Collection("Usuarios");
            var snapshot = await usuariosRef.GetSnapshotAsync();
            int totalUsuarios = snapshot.Count;
            TotalUsuariosTextBlock.Text = totalUsuarios.ToString();
        }

        private async Task ExibirTotalProdutosAsync()
        {
            var produtosRef = db.Collection("Produtos");
            var snapshot = await produtosRef.GetSnapshotAsync();
            int totalProdutos = snapshot.Count;
            TotalProdutosTextBlock.Text = totalProdutos.ToString();
        }

        private async Task ExibirProdutosBaixoEstoqueAsync()
        {
            var produtosRef = db.Collection("Produtos");
            var snapshot = await produtosRef.GetSnapshotAsync();
            int produtosBaixoEstoque = snapshot.Documents.Count(doc => doc.GetValue<int>("Quantidade") < 10);
            ProdutosBaixoEstoqueTextBlock.Text = produtosBaixoEstoque.ToString();
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

        private async Task ExibirLogsRecentesAsync()
        {
            var eventosRef = db.Collection("Eventos");
            var snapshot = await eventosRef.OrderByDescending("DataHora").Limit(10).GetSnapshotAsync();

            UltimosEventosListBox.Items.Clear();
            foreach (var doc in snapshot.Documents)
            {
                var descricao = doc.GetValue<string>("Descricao");
                var dataHora = doc.GetValue<Timestamp>("DataHora").ToDateTime().ToLocalTime();
                UltimosEventosListBox.Items.Add($"{dataHora}: {descricao}");
            }
        }
    }
}

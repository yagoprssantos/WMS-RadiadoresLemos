using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using WMS_RadiadoresLemos_WPF.src.Services;
using WMS_RadiadoresLemos_WPF.src.Models;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class NotificacoesUserControl : UserControl
    {
        private List<AlertaData> alertas;
        private DispatcherTimer verificarBoletosTimer;

        // Evento para notificar o MainWindow sobre novas notificações
        public static event Action<AlertaData>? NovaNotificacaoAdicionada;

        public NotificacoesUserControl()
        {
            InitializeComponent();
            CarregarNotificacoes();
            CarregarFiltros();
            
            // Inicializa o timer para verificar boletos periodicamente
            InicializarTimerVerificacaoBoletos();
            
            // Verifica boletos próximos do vencimento ao carregar
            _ = VerificarBoletosProximosVencimento();
        }

        // Inicializa o timer para verificar boletos a cada hora
        private void InicializarTimerVerificacaoBoletos()
        {
            verificarBoletosTimer = new DispatcherTimer();
            verificarBoletosTimer.Interval = TimeSpan.FromHours(1); // Verifica a cada hora
            verificarBoletosTimer.Tick += async (sender, e) => await VerificarBoletosProximosVencimento();
            verificarBoletosTimer.Start();
        }

        // Método público para forçar verificação de boletos (pode ser chamado de outras telas)
        public async Task ForcarVerificacaoBoletos()
        {
            await VerificarBoletosProximosVencimento();
        }

        // Método para carregar todas as notificações
        private void CarregarNotificacoes()
        {
            alertas = Alerta.Alertas.Values.SelectMany(lista => lista).ToList();

            if (AlertaDataGrid != null)
            {
                AlertaDataGrid.ItemsSource = alertas;
            }
        }

        // Método para carregar os filtros
        private void CarregarFiltros()
        {
            CarregarDadosComboBoxes();
        }

        // Método para carregar dados nos ComboBoxes
        private void CarregarDadosComboBoxes()
        {
            TipoComboBox.ItemsSource = alertas.Select(a => a.Tipo).Distinct().ToList();
        }

        // Método chamado ao clicar no botão de aplicar filtro
        private void AplicarFiltroButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string tipo = TipoComboBox.SelectedItem?.ToString();
            DateTime? dataInicio = DataInicioHistoricoPicker.SelectedDate;
            DateTime? dataFim = DataFimHistoricoPicker.SelectedDate;

            AplicarFiltro(tipo, dataInicio, dataFim);
            FiltroPopup.IsOpen = false;
        }

        // Método para aplicar os filtros na tabela de notificações
        private void AplicarFiltro(string tipo, DateTime? dataInicio, DateTime? dataFim)
        {
            try
            {
                var alertasFiltrados = alertas.Where(a =>
                    (string.IsNullOrEmpty(tipo) || a.Tipo.Equals(tipo, StringComparison.OrdinalIgnoreCase)) &&
                    (!dataInicio.HasValue || DateTime.Parse(a.Data).Date >= dataInicio.Value.Date) &&
                    (!dataFim.HasValue || DateTime.Parse(a.Data).Date <= dataFim.Value.Date)).ToList();

                AlertaDataGrid.ItemsSource = alertasFiltrados;
            }
            catch (Exception ex)
            {
                // Log ou mensagem de erro
                //MessageBox.Show($"Erro ao aplicar filtro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Evento para limpar os filtros
        private void LimparFiltroButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            TipoComboBox.SelectedItem = null;
            DataInicioHistoricoPicker.SelectedDate = null;
            DataFimHistoricoPicker.SelectedDate = null;

            // Recarregar todas as notificações
            AlertaDataGrid.ItemsSource = alertas;
            FiltroPopup.IsOpen = false;
        }

        // Método chamado ao clicar no botão de filtrar
        private void FiltrarButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            FiltroPopup.IsOpen = true;
        }

        private void AlertaDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        // Método para parar o timer quando o controle for descarregado
        public void PararTimer()
        {
            verificarBoletosTimer?.Stop();
        }

        // Método para verificar boletos próximos do vencimento
        private async Task VerificarBoletosProximosVencimento()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db == null) return;

                var collection = db.GetCollection<BoletoData>("boletos");
                var boletos = collection.FindAll().ToList();

                var hoje = DateTime.Today;
                var boletosPendentes = boletos.Where(b => b.Status == StatusBoleto.Pendente).ToList();

                int notificacoesAdicionadas = 0;

                foreach (var boleto in boletosPendentes)
                {
                    var diasAteVencimento = (boleto.DataVencimento.Date - hoje).Days;

                    // Verifica se já foi notificado hoje para evitar duplicatas
                    var notificacaoHoje = alertas?.Any(a => 
                        a.Sistema.Contains($"Boleto {boleto.NotaFiscal}") && 
                        a.Data.StartsWith(hoje.ToString("dd/MM/yyyy"))) ?? false;

                    if (notificacaoHoje) continue;

                    // 7 dias antes do vencimento
                    if (diasAteVencimento == 7)
                    {
                        Alerta.AdicionarAlerta("Aviso",
                            $"Boleto {boleto.NotaFiscal} - Parcela {boleto.Parcela}",
                            $"Boleto vence em uma semana (${boleto.Valor:N2})",
                            $"- Verificar disponibilidade de pagamento\n- Data de vencimento: {boleto.DataVencimento:dd/MM/yyyy}");
                        notificacoesAdicionadas++;
                    }
                    // 1 dia antes do vencimento
                    else if (diasAteVencimento == 1)
                    {
                        Alerta.AdicionarAlerta("Importante",
                            $"Boleto {boleto.NotaFiscal} - Parcela {boleto.Parcela}",
                            $"Boleto vence amanhã(${boleto.Valor:N2})",
                            $"- URGENTE: Realizar pagamento\n- Data de vencimento: {boleto.DataVencimento:dd/MM/yyyy}");
                        notificacoesAdicionadas++;
                    }
                    // Vence hoje
                    else if (diasAteVencimento == 0)
                    {
                        Alerta.AdicionarAlerta("Importante",
                            $"Boleto {boleto.NotaFiscal} - Parcela {boleto.Parcela}",
                            $"Boleto vence hoje (${boleto.Valor:N2})",
                            $"- URGENTE: Realizar pagamento\n- Data de vencimento: {boleto.DataVencimento:dd/MM/yyyy}");
                        notificacoesAdicionadas++;
                    }
                    // Já venceu
                    else if (diasAteVencimento < 0)
                    {
                        Alerta.AdicionarAlerta("Importante",
                            $"Boleto {boleto.NotaFiscal} - Parcela {boleto.Parcela}",
                            $"Boleto vencido há {Math.Abs(diasAteVencimento)} dia(s) (${boleto.Valor:N2})",
                            $"- URGENTE: Regularizar situação\n- Verificar multas e juros\n- Data de vencimento: {boleto.DataVencimento:dd/MM/yyyy}");
                        notificacoesAdicionadas++;
                    }
                }

                // Recarrega as notificações após adicionar as novas
                if (notificacoesAdicionadas > 0)
                {
                    CarregarNotificacoes();
                    Console.WriteLine($"Verificação de boletos: {notificacoesAdicionadas} notificação(ões) adicionada(s)");
                }
            }
            catch (Exception ex)
            {
                // Log do erro sem interromper a aplicação
                Console.WriteLine($"Erro ao verificar boletos próximos do vencimento: {ex.Message}");
            }
        }

        // Método para o botão de verificar boletos
        private async void VerificarBoletosButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                VerificarBoletosButton.IsEnabled = false;
                VerificarBoletosButton.Content = "Verificando...";
                
                await ForcarVerificacaoBoletos();
                
                System.Windows.MessageBox.Show("Verificação de boletos concluída!", "Sucesso", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro ao verificar boletos: {ex.Message}", "Erro", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                VerificarBoletosButton.IsEnabled = true;
                VerificarBoletosButton.Content = "Verificar Boletos";
            }
        }
    }
}

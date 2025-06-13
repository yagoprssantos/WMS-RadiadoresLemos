using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class BoletoTestUserControl : UserControl
    {
        private BoletoIntegrationService? integrationService;

        public BoletoTestUserControl()
        {
            InitializeComponent();
            InicializarServico();
            CarregarBoletosSalvos();
        }

        private async void InicializarServico()
        {
            try
            {
                integrationService = new BoletoIntegrationService();
                integrationService.DadosRecebidos += OnDadosRecebidos;
                await integrationService.IniciarServicoAsync();

                int portaAtiva = integrationService.ObterPorta();
                TxtStatusExtracao.Text = $"🌐 Aguardando dados da aplicação web na porta {portaAtiva}...";
                TxtStatusExtracao.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao inicializar serviço de integração: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🚀 MÉTODO PRINCIPAL - Abre a janela de extração
        private void BtnTestarExtracao_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var boletoExtractionWindow = new BoletoExtractionWindow();
                boletoExtractionWindow.Owner = Window.GetWindow(this);

                if (boletoExtractionWindow.ShowDialog() == true)
                {
                    // Se salvou um boleto na janela, atualiza a lista
                    if (boletoExtractionWindow.BoletoSalvo != null)
                    {
                        CarregarBoletosSalvos();
                        MessageBox.Show("Boleto extraído e salvo com sucesso!", "Sucesso",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir janela de extração: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método que recebe dados da aplicação web (se ainda estiver sendo usado)
        private void OnDadosRecebidos(BoletoExtraidoWebData dados)
        {
            // 🔧 CORREÇÃO: Trata "null" como string vazia
            string beneficiario = (dados.beneficiario == "null" || dados.beneficiario == null) ? "" : dados.beneficiario;
            string cnpj = (dados.cnpjBeneficiario == "null" || dados.cnpjBeneficiario == null) ? "" : dados.cnpjBeneficiario;
            string pagador = (dados.pagador == "null" || dados.pagador == null) ? "" : dados.pagador;
            string valor = (dados.valor == "null" || dados.valor == null) ? "" : dados.valor;
            string vencimento = (dados.vencimento == "null" || dados.vencimento == null) ? "" : dados.vencimento;
            string nossoNumero = (dados.nossoNumero == "null" || dados.nossoNumero == null) ? "" : dados.nossoNumero;
            string linhaDigitavel = (dados.linhaDigitavel == "null" || dados.linhaDigitavel == null) ? "" : dados.linhaDigitavel;

            // Preenche automaticamente os campos
            TxtBeneficiario.Text = beneficiario;
            TxtCNPJ.Text = cnpj;
            TxtPagador.Text = pagador;
            TxtValor.Text = valor;
            TxtVencimento.Text = vencimento;
            TxtNossoNumero.Text = nossoNumero;
            TxtLinhaDigitavel.Text = linhaDigitavel;

            ResultadoBorder.Visibility = Visibility.Visible;
            TxtStatusExtracao.Text = "✅ Dados extraídos e preenchidos automaticamente!";

            // Notificação amigável para o usuário
            MessageBox.Show(
                "🎉 Dados do boleto extraídos com sucesso!\n\n" +
                "Os campos foram preenchidos automaticamente.\n" +
                "Verifique as informações e clique em 'Salvar Boleto' se estiver tudo correto.",
                "Extração Concluída",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        // Métodos para os botões de resultado
        private async void SalvarBoleto_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TxtBeneficiario.Text))
                {
                    MessageBox.Show("Não há dados para salvar.", "Atenção",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var boleto = new BoletoData
                {
                    Beneficiario = TxtBeneficiario.Text,
                    CnpjBeneficiario = TxtCNPJ.Text,
                    Pagador = TxtPagador.Text,
                    LinhaDigitavel = TxtLinhaDigitavel.Text,
                    NossoNumero = TxtNossoNumero.Text,
                    DataCadastro = DateTime.UtcNow,
                    UsuarioCadastro = MainWindow.UsuarioLogado?.Nome,
                    Status = StatusBoleto.Pendente
                };

                // Parse do valor
                if (decimal.TryParse(TxtValor.Text.Replace("R$", "").Replace(".", "").Replace(",", "."),
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal valor))
                {
                    boleto.Valor = valor;
                }

                // Parse da data
                if (DateTime.TryParseExact(TxtVencimento.Text, "dd/MM/yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime vencimento))
                {
                    boleto.DataVencimento = vencimento;
                    boleto.Status = vencimento < DateTime.Now ? StatusBoleto.Vencido : StatusBoleto.Pendente;
                }

                // Salva no banco
                var db = DatabaseConnect.Database;
                if (db != null)
                {
                    var collection = db.GetCollection<BoletoData>("boletos");
                    collection.Insert(boleto);

                    MessageBox.Show("Boleto salvo com sucesso!", "Sucesso",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    LimparResultado_Click(sender, e);
                    CarregarBoletosSalvos();
                }
                else
                {
                    MessageBox.Show("Erro ao conectar com o banco de dados.", "Erro",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar boleto: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LimparResultado_Click(object sender, RoutedEventArgs e)
        {
            // Limpa campos de resultado
            TxtBeneficiario.Text = "";
            TxtCNPJ.Text = "";
            TxtPagador.Text = "";
            TxtValor.Text = "";
            TxtVencimento.Text = "";
            TxtNossoNumero.Text = "";
            TxtLinhaDigitavel.Text = "";

            ResultadoBorder.Visibility = Visibility.Collapsed;
            TxtStatusExtracao.Text = "🌐 Aguardando extração...";
        }

        // Carrega boletos salvos
        private void CarregarBoletosSalvos()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db != null)
                {
                    var collection = db.GetCollection<BoletoData>("boletos");
                    var boletos = collection.FindAll().OrderByDescending(b => b.DataCadastro).ToList();
                    BoletosContainer.ItemsSource = boletos;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar boletos: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AtualizarLista_Click(object sender, RoutedEventArgs e)
        {
            CarregarBoletosSalvos();
        }

        private void VerDetalhes_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is BoletoData boleto)
            {
                MessageBox.Show($"Detalhes do Boleto:\n\n" +
                    $"Beneficiário: {boleto.Beneficiario}\n" +
                    $"Valor: {boleto.Valor:C}\n" +
                    $"Vencimento: {boleto.DataVencimento:dd/MM/yyyy}\n" +
                    $"Status: {boleto.Status}\n" +
                    $"Cadastrado: {boleto.DataCadastro:dd/MM/yyyy HH:mm}",
                    "Detalhes do Boleto", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
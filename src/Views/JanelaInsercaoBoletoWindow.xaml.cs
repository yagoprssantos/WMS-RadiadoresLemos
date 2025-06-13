using System.Windows;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class JanelaInsercaoBoletoWindow : Window
    {
        public BoletoExtraidoData? DadosExtraidos { get; private set; }

        public JanelaInsercaoBoletoWindow()
        {
            InitializeComponent();
        }

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            DadosExtraidos = new BoletoExtraidoData
            {
                Beneficiario = TxtBeneficiario.Text,
                CnpjBeneficiario = TxtCNPJ.Text,
                Pagador = TxtPagador.Text,
                Valor = TxtValor.Text,
                Vencimento = TxtVencimento.Text,
                NossoNumero = TxtNossoNumero.Text,
                LinhaDigitavel = TxtLinhaDigitavel.Text
            };

            DialogResult = true;
            Close();
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
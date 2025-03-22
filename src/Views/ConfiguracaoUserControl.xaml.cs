using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class ConfiguracaoUserControl : UserControl
    {
        public ConfiguracaoUserControl()
        {
            InitializeComponent();
        }

        private void BtnUsuarios_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new UsuariosUserControl();
        }

        private void BtnBancoDados_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new BancoDadosUserControl();
        }

        private void BtnTema_Click(object sender, RoutedEventArgs e)
        {
           // ContentArea.Content = new TemaUserControl();
        }
    }
}

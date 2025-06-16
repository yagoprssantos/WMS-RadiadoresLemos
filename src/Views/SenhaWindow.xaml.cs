using System.Windows;
using System.Windows.Input;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class SenhaWindow : Window
    {
        public string Senha { get; private set; }

        public SenhaWindow(string mensagem = "Digite a senha:")
        {
            InitializeComponent();
            TituloTextBlock.Text = mensagem;
            SenhaPasswordBox.Focus();
        }

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            Senha = SenhaPasswordBox.Password;
            DialogResult = true;
            Close();
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SenhaPasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Confirmar_Click(sender, e);
            }
            else if (e.Key == Key.Escape)
            {
                Cancelar_Click(sender, e);
            }
        }
    }
} 
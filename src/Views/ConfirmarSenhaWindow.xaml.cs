using System.Windows;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class ConfirmarSenhaWindow : Window
    {
        private int _tentativas;

        public bool IsConfirmed { get; private set; }

        public ConfirmarSenhaWindow()
        {
            InitializeComponent();
            IsConfirmed = false;
            _tentativas = 0;
        }

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            string senhaInserida = SenhaPasswordBox.Password;

            if (MainWindow.UsuarioLogado != null && MainWindow.UsuarioLogado.Senha == senhaInserida)
            {
                IsConfirmed = true;
                this.Close();
            }
            else
            {
                _tentativas++;
                if (_tentativas >= 3)
                {
                    MessageBox.Show("Número máximo de tentativas excedido. Ação cancelada.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    IsConfirmed = false;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Senha incorreta! Tente novamente.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    SenhaPasswordBox.Clear();
                }
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            this.Close();
        }
    }
}

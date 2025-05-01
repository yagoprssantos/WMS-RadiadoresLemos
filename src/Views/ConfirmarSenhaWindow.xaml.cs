using System.Windows;
using WMS_RadiadoresLemos_WPF.src.Services;

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

        // Evento disparado quando a janela é carregada
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Define o foco no campo de senha
            SenhaPasswordBox.Focus();
        }

        // Evento disparado ao clicar no botão "Confirmar"
        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            string senhaInserida = SenhaPasswordBox.Password;

            // Verifica se a senha inserida corresponde à senha do usuário logado
            if (MainWindow.UsuarioLogado != null && CriptografiaService.VerificarSenha(senhaInserida, MainWindow.UsuarioLogado.Senha))
            {
                IsConfirmed = true;
                this.Close();
            }
            else
            {
                _tentativas++;
                // Verifica se o número máximo de tentativas foi excedido
                if (_tentativas >= 3)
                {
                    MessageBox.Show("Número máximo de tentativas excedido. Ação cancelada.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    IsConfirmed = false;
                    this.Close();
                }
                else
                {
                    // Exibe mensagem de erro e limpa o campo de senha
                    MessageBox.Show("Senha incorreta! Tente novamente.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    SenhaPasswordBox.Clear();
                }
            }
        }

        // Evento disparado ao clicar no botão "Cancelar"
        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            // Cancela a operação e fecha a janela
            IsConfirmed = false;
            this.Close();
        }

        // Evento disparado ao pressionar uma tecla no campo de senha
        private void SenhaPasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Confirma a senha ao pressionar a tecla Enter
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                Confirmar_Click(sender, e);
            }
        }
    }
}

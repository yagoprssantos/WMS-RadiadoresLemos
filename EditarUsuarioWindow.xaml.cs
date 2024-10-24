using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace WMS_RadiadoresLemos_WPF
{
    public partial class EditarUsuarioWindow : Window
    {
        private Usuario usuario;

        public EditarUsuarioWindow(Usuario usuario)
        {
            InitializeComponent();
            this.usuario = usuario;

            // Preenche os campos com os valores existentes, se houver
            NomeTextBox.Text = usuario.Nome;
            EmailTextBox.Text = usuario.Email;
            PermissaoComboBox.Text = usuario.Permissao;
        }

        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            // Validações básicas
            if (string.IsNullOrWhiteSpace(NomeTextBox.Text) || string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show("Por favor, preencha todos os campos.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Atualiza o objeto usuário
            usuario.Nome = NomeTextBox.Text;
            usuario.Email = EmailTextBox.Text;
            usuario.Permissao = ((ComboBoxItem)PermissaoComboBox.SelectedItem)?.Content.ToString() ?? "Usuário";

            DialogResult = true; // Confirma a operação
            Close();
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // Cancela a operação
            Close();
        }
    }
}

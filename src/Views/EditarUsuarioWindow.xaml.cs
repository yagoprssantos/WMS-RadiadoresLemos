using System;
using System.Windows;
using System.Windows.Controls;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class EditarUsuarioWindow : Window
    {
        private Usuario usuario;

        public EditarUsuarioWindow(Usuario usuario = null)
        {
            InitializeComponent();
            this.usuario = usuario ?? new Usuario();

            // Preenche os campos com os valores do usuário existente, se fornecido
            if (usuario != null)
            {
                NomeTextBox.Text = usuario.Nome;
                EmailTextBox.Text = usuario.Email;
                PermissaoComboBox.SelectedItem = GetComboBoxItemByContent(usuario.Permissao);
            }
        }

        private void RegistrarUsuario_Click(object sender, RoutedEventArgs e)
        {
            // Validações básicas
            if (string.IsNullOrWhiteSpace(NomeTextBox.Text) ||
                string.IsNullOrWhiteSpace(EmailTextBox.Text) ||
                string.IsNullOrWhiteSpace(SenhaPasswordBox.Password) ||
                PermissaoComboBox.SelectedItem == null)
            {
                MessageBox.Show("Por favor, preencha todos os campos.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Atualiza ou define os valores do objeto usuário
            usuario.Nome = NomeTextBox.Text;
            usuario.Email = EmailTextBox.Text;
            usuario.Senha = SenhaPasswordBox.Password;
            usuario.Permissao = ((ComboBoxItem)PermissaoComboBox.SelectedItem).Content.ToString();

            DialogResult = true; // Confirma a operação
            Close();
        }

        private ComboBoxItem GetComboBoxItemByContent(string content)
        {
            foreach (ComboBoxItem item in PermissaoComboBox.Items)
            {
                if (item.Content.ToString() == content)
                    return item;
            }
            return null;
        }
    }
}

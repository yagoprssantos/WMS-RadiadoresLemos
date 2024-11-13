using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class EditarUsuarioWindow : Window
    {
        private UsuarioData usuario;
        private bool isModified = false;

        // Propriedade pública para acessar o usuário editado
        public UsuarioData Usuario => usuario;

        // Construtor que inicializa a janela com os dados do usuário ou vazio
        public EditarUsuarioWindow(UsuarioData? usuario)
        {
            InitializeComponent();

            if (usuario == null)
            {
                // Cria usuario vazio com valores padrão
                this.usuario = new UsuarioData
                {
                    Nome = string.Empty,
                    Email = string.Empty,
                    Matrícula = string.Empty,
                    Senha = string.Empty,
                    Cargo = string.Empty
                };
            }
            else
            {
                this.usuario = usuario;
            }

            PreencherCampos();
        }

        // Preenche os campos da interface com os dados do usuário
        private void PreencherCampos()
        {
            try
            {
                if (usuario != null)
                {
                    NomeTextBox.Text = usuario.Nome;
                    EmailTextBox.Text = usuario.Email;
                    MatriculaTextBox.Text = usuario.Matrícula;
                    SenhaPasswordBox.Password = usuario.Senha;
                    PermissaoComboBox.SelectedItem = GetComboBoxItemByContent(usuario.Cargo);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao preencher campos: {ex.Message}");

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message,
                                            "Erro ao preencher campos de usuário. Possíveis motivos:\n" +
                                            "- O usuário não foi encontrado;\n" +
                                            "- O usuário não foi passado corretamente para a janela de edição;\n" +
                                            "- Ocorreu um erro ao preencher os campos da janela de edição.",
                                            "- Verifique se o usuário foi encontrado no banco de dados;\n" +
                                            "- Verifique se suas informações estão corretamente preenchidas;\n" +
                                            "- Verifique conexão com o banco de dados.");
            }
        }

        // Evento disparado ao clicar no botão de salvar usuário
        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ValidarCampos())
                {
                    AtualizarUsuario();
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar usuário: {ex.Message}");

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message,
                                            "Erro ao salvar usuário. Possíveis motivos:\n" +
                                            "- O usuário não foi encontrado;\n" +
                                            "- O usuário não foi passado corretamente para a janela de edição;\n" +
                                            "- Ocorreu um erro ao salvar os campos da janela de edição.",
                                            "- Verifique se o usuário foi encontrado no banco de dados;\n" +
                                            "- Verifique se suas informações estão corretamente preenchidas;\n" +
                                            "- Verifique conexão com o banco de dados.");
            }
        }

        // Atualiza os dados do usuário com os valores dos campos
        private void AtualizarUsuario()
        {
            usuario.Nome = NomeTextBox.Text;
            usuario.Email = EmailTextBox.Text;
            usuario.Matrícula = MatriculaTextBox.Text;
            usuario.Senha = SenhaPasswordBox.Password;
            usuario.Cargo = ((ComboBoxItem)PermissaoComboBox.SelectedItem)?.Content?.ToString() ?? string.Empty;
        }

        // Evento disparado ao clicar no botão de cancelar
        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isModified && ConfirmarSaidaSemSalvar())
                {
                    return;
                }
                DialogResult = false;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao cancelar: {ex.Message}");
                Close();
            }
        }

        // Confirma se o usuário deseja sair sem salvar as alterações
        private bool ConfirmarSaidaSemSalvar()
        {
            var result = MessageBox.Show("Existem alterações não salvas. Deseja sair sem salvar?", "Confirmação", MessageBoxButton.YesNo);
            return result == MessageBoxResult.No;
        }

        // Restrições de entrada de texto nos TextBoxes
        private void MatriculaTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text, "[^0-9]+");
        }

        private void MatriculaTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            HandlePasting(e, "[^0-9]+");
        }

        private void NomeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text, "[^a-zA-Z ]+");
        }

        private void NomeTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            HandlePasting(e, "[^a-zA-Z ]+");
        }

        // Verifica se o texto é permitido com base no padrão fornecido
        private static bool IsTextAllowed(string text, string pattern)
        {
            return !Regex.IsMatch(text, pattern);
        }

        // Lida com a colagem de texto, verificando se o texto colado é permitido
        private static void HandlePasting(DataObjectPastingEventArgs e, string pattern)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextAllowed(text, pattern))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        // Evento disparado ao mudar a seleção do cargo do usuário
        private void PermissaoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PermissaoComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Content != null)
            {
                usuario.Cargo = selectedItem.Content.ToString() ?? string.Empty;
                isModified = true;
            }
        }

        // Evento disparado ao modificar qualquer campo de texto
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            isModified = true;
        }

        private void SenhaPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            isModified = true;
        }

        // Valida os campos antes de salvar
        private bool ValidarCampos()
        {
            if (string.IsNullOrWhiteSpace(NomeTextBox.Text))
            {
                MessageBox.Show("O campo Nome deve ser preenchido.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show("O campo Email deve ser preenchido.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(MatriculaTextBox.Text))
            {
                MessageBox.Show("O campo Matrícula deve ser preenchido.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(SenhaPasswordBox.Password))
            {
                MessageBox.Show("O campo Senha deve ser preenchido.");
                return false;
            }
            if (PermissaoComboBox.SelectedItem == null)
            {
                MessageBox.Show("O campo Cargo deve ser selecionado.");
                return false;
            }
            return true;
        }

        private ComboBoxItem GetComboBoxItemByContent(string content)
        {
            foreach (ComboBoxItem item in PermissaoComboBox.Items)
            {
                if (item.Content.ToString() == content)
                    return item;
            }
            return new ComboBoxItem { Content = string.Empty };
        }
    }
}

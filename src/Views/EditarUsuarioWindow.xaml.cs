using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;
using WMS_RadiadoresLemos_WPF.src.Views;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class EditarUsuarioWindow : Window
    {
        private UsuarioData usuario;
        private bool isModified = false;
        private bool isNewUser = false;
        private List<UsuarioData> usuarios;

        // Propriedade pública para acessar o usuário editado
        public UsuarioData Usuario => usuario;

        // Construtor que inicializa a janela com os dados do usuário ou vazio
        public EditarUsuarioWindow(UsuarioData? usuario)
        {
            InitializeComponent();

            if (usuario == null)
            {
                isNewUser = true;
                // Cria usuario vazio com valores padrão
                string novaMatricula;
                do
                {
                    novaMatricula = GerarMatricula("Usuário");
                } while (MatriculaExiste(novaMatricula));

                this.usuario = new UsuarioData
                {
                    Nome = string.Empty,
                    Email = string.Empty,
                    Matrícula = novaMatricula, // Gera a matrícula com base no cargo e ano atual
                    Senha = string.Empty,
                    Cargo = "Usuário",
                    Id = novaMatricula
                };
            }
            else
            {
                this.usuario = usuario;
            }

            usuarios = new List<UsuarioData>();
            PreencherCampos();

            isModified = false;
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
                    SenhaPasswordBox.Password = usuario.Senha.ToString();
                    PermissaoComboBox.SelectedItem = GetComboBoxItemByContent(usuario.Cargo);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Erro ao preencher campos: {ex.Message}");

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Erro ao preencher campos de usuário. Possíveis motivos:\n" +
                                            "- O usuário não foi encontrado;\n" +
                                            "- O usuário não foi passado corretamente para a janela de edição;\n" +
                                            "- Ocorreu um erro ao preencher os campos da janela de edição.",
                                            "- Verifique se o usuário foi encontrado no banco de dados;\n" +
                                            "- Verifique se suas informações estão corretamente preenchidas;\n" +
                                            "- Verifique conexão com o banco de dados.");
            }
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


        // Evento disparado ao clicar no botão de salvar usuário
        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var confirmarSenhaWindow = new ConfirmarSenhaWindow();
                confirmarSenhaWindow.ShowDialog();

                if (confirmarSenhaWindow.IsConfirmed)
                {
                    if (ValidarCampos())
                    {
                        AtualizarUsuario();
                        DialogResult = true;
                        Close();
                    }
                }
                else
                {
                    MessageBox.Show("Ação cancelada. Senha não confirmada.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Erro ao salvar usuário: {ex.Message}");

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Erro ao salvar usuário. Possíveis motivos:\n" +
                                            "- Dados do usuário não são válidos;\n" +
                                            "- Usuário inexistente no banco de dados;\n" +
                                            "- Banco de dados inacessível.",
                                            "- Verifique se os dados do usuário estão corretos;\n" +
                                            "- Verifique se o usuário existe no banco de dados;\n" +
                                            "- Verifique se o banco de dados está acessível.");
            }
        }

        // Atualiza os dados do usuário com os valores dos campos
        private void AtualizarUsuario()
        {
            usuario.Nome = NomeTextBox.Text;
            usuario.Email = EmailTextBox.Text;
            usuario.Matrícula = MatriculaTextBox.Text;
            usuario.Senha = SenhaPasswordBox.Password.ToString();
            usuario.Cargo = ((ComboBoxItem)PermissaoComboBox.SelectedItem)?.Content?.ToString() ?? string.Empty;

            isModified = false;
        }

        // Evento disparado ao clicar no botão de cancelar
        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isModified)
                {
                    if (ConfirmarSaidaSemSalvar())
                    {
                        return;
                    }
                }
                DialogResult = false;
                Close();
            }
            catch (Exception)
            {
                AlertaCache.AdicionarAlerta("Erro",
                                            "Edição de usuário.",
                                            "Erro ao cancelar edição de usuário. Possíveis motivos:\n" +
                                            "- Erro ao fechar janela de edição de usuário;\n" +
                                            "- Impossibilidade de fechar janela de edição de usuário.",
                                            "- Verifique se a janela de edição de usuário está aberta."); Close();
            }
        }

        // Confirma se o usuário deseja sair sem salvar as alterações
        private bool ConfirmarSaidaSemSalvar()
        {
            var result = MessageBox.Show("Existem alterações não salvas. Deseja sair sem salvar?", "Confirmação", MessageBoxButton.YesNo);
            return result == MessageBoxResult.No;
        }


        // Método para verificar se a matrícula já existe
        private bool MatriculaExiste(string matricula)
        {
            if (DadosCache.Tabelas.TryGetValue("Usuarios", out var usuarios))
            {
                return usuarios.OfType<UsuarioData>().Any(u => u.Matrícula == matricula);
            }
            return false;
        }

        // Método para gerar a matrícula do usuário com base no cargo e ano atual
        private string GerarMatricula(string cargo)
        {
            string prefixo = cargo switch
            {
                "Administrador" => "ADM", // Administrador do Sistema
                "Gerente" => "GER", // Gerente da Unidade
                "Operador" => "OPE", // Operador de Produção
                "Estagiário" => "EST", // Estagiário
                "Usuário" => "USR", // Usuário Comum
                _ => "UNK"
            };

            string ano = DateTime.Now.Year.ToString().Substring(2, 2);
            Random random = new Random();
            string posicao = random.Next(0, 100).ToString("D2");

            return $"{prefixo}{ano}{posicao}";
        }

        // Método de criação do Popup de "Sobre o cargo"
        private void MaisSobreCargo_Click(object sender, RoutedEventArgs e)
        {
            CargoPopup.IsOpen = true;
        }


        // Restrições de entrada de texto nos TextBoxes

        private void NomeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text, "[^a-zA-Z ]+");
        }

        private void NomeTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            HandlePasting(e, "[^a-zA-Z ]+");
        }

        private void SenhaPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            isModified = true;
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
                string novoCargo = selectedItem.Content.ToString() ?? string.Empty;

                // Se for um novo usuário, gera uma nova matrícula com base no cargo
                if (isNewUser)
                {
                    string novaMatricula;
                    do
                    {
                        novaMatricula = GerarMatricula(novoCargo);
                    } while (MatriculaExiste(novaMatricula));

                    usuario.Matrícula = novaMatricula; // Atualiza a matrícula com base no novo cargo e ano atual
                    MatriculaTextBox.Text = usuario.Matrícula; // Atualiza o campo de texto da matrícula
                }
                // Se for um usuário existente, altera apenas a matrícula se for um cargo diferente
                else if (usuario.Cargo != novoCargo)
                {
                    string novaMatricula;
                    do
                    {
                        novaMatricula = GerarMatricula(novoCargo);
                    } while (MatriculaExiste(novaMatricula));

                    usuario.Matrícula = novaMatricula; // Atualiza a matrícula com base no novo cargo e ano atual
                    MatriculaTextBox.Text = usuario.Matrícula; // Atualiza o campo de texto da matrícula
                }

                usuario.Cargo = novoCargo;
                isModified = true;
            }
        }

        // Evento disparado ao modificar qualquer campo de texto
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
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
    }
}

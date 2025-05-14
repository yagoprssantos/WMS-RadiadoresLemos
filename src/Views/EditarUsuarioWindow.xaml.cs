using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;
using WMS_RadiadoresLemos_WPF.Views;
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
            InicializarUsuario(usuario);
            PreencherCampos();
            isModified = false;
        }

        // Inicializa o usuário com dados existentes ou cria um novo usuário
        private void InicializarUsuario(UsuarioData? usuario)
        {
            // Se usuário for nulo, cria um novo usuário
            if (usuario == null)
            {
                isNewUser = true;
                string novaMatricula;
                do
                {
                    novaMatricula = GerarMatricula("Usuário");
                } while (MatriculaExiste(novaMatricula));

                this.usuario = new UsuarioData
                {
                    Nome = string.Empty,
                    Email = string.Empty,
                    Matricula = novaMatricula,
                    Senha = string.Empty,
                    Cargo = "Usuário",
                    Id = novaMatricula
                };
            }
            // Se usuário não for nulo, usa os dados existentes selecionados
            else
            {
                this.usuario = usuario;
            }

            usuarios = new List<UsuarioData>();
        }

        // Preenche os campos da interface com os dados do usuário
        private void PreencherCampos()
        {
            try
            {
                NomeTextBox.Text = usuario.Nome;
                EmailTextBox.Text = usuario.Email;
                MatriculaTextBox.Text = usuario.Matricula;
                PermissaoComboBox.SelectedItem = GetComboBoxItemByContent(usuario.Cargo);
            }
            catch (Exception ex)
            {
                Alerta.AdicionarAlerta("Erro",
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
                        if (isNewUser)
                        {
                            CadastrarUsuario();
                        }
                        else
                        {
                            AtualizarUsuario();
                        }
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
                Alerta.AdicionarAlerta("Erro",
                    ex.Message.ToString(),
                    "Erro ao salvar usuário. Possíveis motivos:\n" +
                    "- Dados do usuário não são válidos;\n" +
                    "- Banco de dados inacessível.",
                    "- Verifique se os dados do usuário estão corretos;\n" +
                    "- Verifique se o banco de dados está acessível.");
            }
        }

        // Cadastra um novo usuário no banco de dados
        private async void CadastrarUsuario()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db == null)
                {
                    MessageBox.Show("Erro: Banco de dados não está conectado.");
                    return;
                }

                var collection = db.GetCollection<UsuarioData>("usuarios");

                // Verifica se já existe um usuário com a mesma matrícula
                var usuarioExistente = collection.FindOne(u => u.Matricula == usuario.Matricula);
                if (usuarioExistente != null)
                {
                    MessageBox.Show("Já existe um usuário com esta matrícula.", "Usuário Existente", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                UsuarioData data = DadosDoUsuario();

                // Garante que o Id seja igual à matrícula
                data.Id = data.Matricula;

                // Tenta inserir o novo usuário
                collection.Insert(data);

                // Adiciona log
                var log = new LogData
                {
                    Data = DateTime.UtcNow,
                    Tipo = "OPERACIONAL",
                    Nivel = "Usuário",
                    Detalhes = $"Usuário cadastrado: {data.Nome}, Matrícula: {data.Matricula}",
                    Usuario = MainWindow.UsuarioLogado?.Nome ?? "Sistema"
                };
                await LogHistorico.SalvarLog(log);

                MessageBox.Show("Usuário cadastrado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao cadastrar usuário: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                Alerta.AdicionarAlerta("Erro",
                    ex.Message.ToString(),
                    "Erro ao cadastrar usuário. Possíveis motivos:\n" +
                    "- Dados do usuário não são válidos;\n" +
                    "- Banco de dados inacessível.",
                    "- Verifique se os dados do usuário estão corretos;\n" +
                    "- Verifique se o banco de dados está acessível.");
            }
        }

        // Atualiza os dados do usuário com os valores dos campos
        private async void AtualizarUsuario()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db == null)
                {
                    MessageBox.Show("Erro: Banco de dados não está conectado.");
                    return;
                }

                var collection = db.GetCollection<UsuarioData>("usuarios");

                UsuarioData data = DadosDoUsuario();

                // Garante que o Id seja igual à matrícula
                data.Id = data.Matricula;

                // Tenta atualizar o usuário
                collection.Update(data);

                // Adiciona log
                var log = new LogData
                {
                    Data = DateTime.UtcNow,
                    Tipo = "OPERACIONAL",
                    Nivel = "Usuário",
                    Detalhes = $"Usuário atualizado: {data.Nome}, Matrícula: {data.Matricula}",
                    Usuario = MainWindow.UsuarioLogado?.Nome ?? "Sistema"
                };
                await LogHistorico.SalvarLog(log);

                MessageBox.Show("Usuário atualizado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar usuário: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                Alerta.AdicionarAlerta("Erro",
                    ex.Message.ToString(),
                    "Erro ao atualizar usuário. Possíveis motivos:\n" +
                    "- Dados do usuário não são válidos;\n" +
                    "- Banco de dados inacessível.",
                    "- Verifique se os dados do usuário estão corretos;\n" +
                    "- Verifique se o banco de dados está acessível.");
            }
        }

        // Método para obter os dados do usuário a partir dos TextBoxes
        private UsuarioData DadosDoUsuario() => new()
        {
            Nome = NomeTextBox.Text.Trim(),
            Email = EmailTextBox.Text.Trim(),
            Matricula = MatriculaTextBox.Text.Trim(),
            Senha = string.IsNullOrEmpty(SenhaPasswordBox.Password) ? usuario.Senha : CriptografiaService.CriptografarSenha(SenhaPasswordBox.Password.Trim()),
            Cargo = ((ComboBoxItem)PermissaoComboBox.SelectedItem)?.Content?.ToString() ?? string.Empty,
            Id = MatriculaTextBox.Text.Trim()
        };

        // Método de criação do Popup de "Sobre o cargo"
        private void MaisSobreCargo_Click(object sender, RoutedEventArgs e)
        {
            CargoPopup.IsOpen = true;
        }

        // Evento disparado ao clicar no botão de cancelar
        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isModified)
                {
                    var result = MessageBox.Show("Existem alterações não salvas. Deseja sair sem salvar?", "Confirmação", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }
                DialogResult = false;
                Close();
            }
            catch (Exception)
            {
                Alerta.AdicionarAlerta("Erro",
                    "Edição de usuário.",
                    "Erro ao cancelar edição de usuário. Possíveis motivos:\n" +
                    "- Erro ao fechar janela de edição de usuário;\n" +
                    "- Impossibilidade de fechar janela de edição de usuário.",
                    "- Verifique se a janela de edição de usuário está aberta.");
                Close();
            }
        }

        // Matrícula
        private bool MatriculaExiste(string matricula)
        {
            if (DatabaseConnect.Database == null)
                return false;

            var collection = DatabaseConnect.Database.GetCollection<UsuarioData>("usuarios");
            return collection.Exists(u => u.Matricula == matricula);
        }
        private string GerarMatricula(string cargo)
        {
            string prefixo = cargo switch
            {
                "Administrador" => "ADM",
                "Gerente" => "GER",
                "Operador" => "OPE",
                "Estagiário" => "EST",
                "Usuário" => "USR",
                _ => "UNK"
            };

            string ano = DateTime.Now.Year.ToString().Substring(2, 2);
            Random random = new Random();
            string posicao = random.Next(0, 100).ToString("D2");

            return $"{prefixo}{ano}{posicao}";
        }

        // Tratamento de entradas
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            isModified = true;
        }
        private void NomeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text, "[^a-zA-Z ]+");
        }
        private void SenhaPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            isModified = true;
        }
        private void PermissaoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PermissaoComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Content != null)
            {
                string novoCargo = selectedItem.Content.ToString() ?? string.Empty;

                if (isNewUser || usuario.Cargo != novoCargo)
                {
                    string novaMatricula;
                    do
                    {
                        novaMatricula = GerarMatricula(novoCargo);
                    } while (MatriculaExiste(novaMatricula));

                    usuario.Matricula = novaMatricula;
                    MatriculaTextBox.Text = usuario.Matricula;
                }

                usuario.Cargo = novoCargo;
                isModified = true;
            }
        }
        private static bool IsTextAllowed(string text, string pattern)
        {
            return !Regex.IsMatch(text, pattern);
        }
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

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class EditarClienteWindow : Window
    {
        private ClienteData cliente;
        private bool isModified = false;
        private bool isNewClient = false;

        public ClienteData Cliente => cliente;

        public EditarClienteWindow(ClienteData? cliente)
        {
            InitializeComponent();
            InicializarCliente(cliente);
            PreencherCampos();
            isModified = false;
        }

        private void InicializarCliente(ClienteData? cliente)
        {
            if (cliente == null)
            {
                isNewClient = true;
                this.cliente = new ClienteData
                {
                    Email = string.Empty,
                    Telefone = string.Empty,
                    CNPJ = string.Empty,
                    Estado = string.Empty,
                    VendasRelacionadas = new List<string>(),
                    Id = string.Empty
                };
            }
            else
            {
                this.cliente = cliente;
            }
        }

        private void PreencherCampos()
        {
            try
            {
                EmailTextBox.Text = cliente.Email;
                TelefoneTextBox.Text = cliente.Telefone;
                CNPJTextBox.Text = cliente.CNPJ;
                EstadoTextBox.Text = cliente.Estado;
                
                // Atualiza o título da janela
                this.Title = isNewClient ? "Cadastrar Cliente" : "Editar Cliente";
                
                // Atualiza o texto do botão de salvar
                SalvarButton.Content = isNewClient ? "Cadastrar Cliente" : "Atualizar Cliente";
            }
            catch (Exception ex)
            {
                Alerta.AdicionarAlerta("Erro",
                    ex.Message.ToString(),
                    "Erro ao preencher campos do cliente.",
                    "- Verifique se os dados do cliente estão corretos.");
            }
        }

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
                        if (isNewClient)
                        {
                            CadastrarCliente();
                        }
                        else
                        {
                            AtualizarCliente();
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
                    "Erro ao salvar cliente.",
                    "- Verifique se os dados do cliente estão corretos.");
            }
        }

        private async void CadastrarCliente()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db == null)
                {
                    MessageBox.Show("Erro: Banco de dados não está conectado.");
                    return;
                }

                var collection = db.GetCollection<ClienteData>("clientes");

                // Verifica se já existe um cliente com o mesmo CNPJ
                var clienteExistente = collection.FindOne(c => c.CNPJ == cliente.CNPJ);
                if (clienteExistente != null)
                {
                    MessageBox.Show("Já existe um cliente com este CNPJ.", "Cliente Existente", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ClienteData data = DadosDoCliente();
                data.SetIdFromCNPJ();

                collection.Insert(data);

                var log = new LogData
                {
                    Data = DateTime.UtcNow,
                    Tipo = "OPERACIONAL",
                    Nivel = "Usuário",
                    Detalhes = $"Cliente cadastrado: {data.Email}, CNPJ: {data.CNPJ}",
                    Usuario = MainWindow.UsuarioLogado?.Nome ?? "Sistema"
                };
                await LogHistorico.SalvarLog(log);

                MessageBox.Show("Cliente cadastrado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao cadastrar cliente: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                Alerta.AdicionarAlerta("Erro",
                    ex.Message.ToString(),
                    "Erro ao cadastrar cliente.",
                    "- Verifique se os dados do cliente estão corretos.");
            }
        }

        private async void AtualizarCliente()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db == null)
                {
                    MessageBox.Show("Erro: Banco de dados não está conectado.");
                    return;
                }

                var collection = db.GetCollection<ClienteData>("clientes");

                ClienteData data = DadosDoCliente();
                data.SetIdFromCNPJ();

                collection.Update(data);

                var log = new LogData
                {
                    Data = DateTime.UtcNow,
                    Tipo = "OPERACIONAL",
                    Nivel = "Usuário",
                    Detalhes = $"Cliente atualizado: {data.Email}, CNPJ: {data.CNPJ}",
                    Usuario = MainWindow.UsuarioLogado?.Nome ?? "Sistema"
                };
                await LogHistorico.SalvarLog(log);

                MessageBox.Show("Cliente atualizado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar cliente: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                Alerta.AdicionarAlerta("Erro",
                    ex.Message.ToString(),
                    "Erro ao atualizar cliente.",
                    "- Verifique se os dados do cliente estão corretos.");
            }
        }

        private ClienteData DadosDoCliente() => new()
        {
            Email = EmailTextBox.Text.Trim(),
            Telefone = TelefoneTextBox.Text.Trim(),
            CNPJ = CNPJTextBox.Text.Trim(),
            Estado = EstadoTextBox.Text.Trim(),
            VendasRelacionadas = cliente.VendasRelacionadas,
            Id = CNPJTextBox.Text.Trim()
        };

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
                    "Edição de cliente.",
                    "Erro ao cancelar edição de cliente.",
                    "- Verifique se a janela de edição de cliente está aberta.");
                Close();
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            isModified = true;
        }

        private bool ValidarCampos()
        {
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show("O campo Email deve ser preenchido.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(TelefoneTextBox.Text))
            {
                MessageBox.Show("O campo Telefone deve ser preenchido.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(CNPJTextBox.Text))
            {
                MessageBox.Show("O campo CNPJ deve ser preenchido.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(EstadoTextBox.Text))
            {
                MessageBox.Show("O campo Estado deve ser preenchido.");
                return false;
            }
            return true;
        }
    }
}

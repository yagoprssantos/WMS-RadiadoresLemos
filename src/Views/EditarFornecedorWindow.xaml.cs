using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class EditarFornecedorWindow : Window
    {
        private FornecedorData fornecedor;
        private bool isModified = false;
        private bool isNewFornecedor = false;

        public FornecedorData Fornecedor => fornecedor;

        public EditarFornecedorWindow(FornecedorData? fornecedor)
        {
            InitializeComponent();
            InicializarFornecedor(fornecedor);
            PreencherCampos();
            isModified = false;
        }

        private void InicializarFornecedor(FornecedorData? fornecedor)
        {
            if (fornecedor == null)
            {
                isNewFornecedor = true;
                this.fornecedor = new FornecedorData
                {
                    Nome = string.Empty,
                    CNPJ = string.Empty,
                    Estado = string.Empty,
                    ComprasRelacionadas = new List<string>(),
                    Id = string.Empty
                };
            }
            else
            {
                this.fornecedor = fornecedor;
            }
        }

        private void PreencherCampos()
        {
            try
            {
                NomeTextBox.Text = fornecedor.Nome;
                CNPJTextBox.Text = fornecedor.CNPJ;
                EstadoTextBox.Text = fornecedor.Estado;
            }
            catch (Exception ex)
            {
                Alerta.AdicionarAlerta("Erro",
                    ex.Message.ToString(),
                    "Erro ao preencher campos do fornecedor.",
                    "- Verifique se os dados do fornecedor estão corretos.");
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
                        if (isNewFornecedor)
                        {
                            CadastrarFornecedor();
                        }
                        else
                        {
                            AtualizarFornecedor();
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
                    "Erro ao salvar fornecedor.",
                    "- Verifique se os dados do fornecedor estão corretos.");
            }
        }

        private async void CadastrarFornecedor()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db == null)
                {
                    MessageBox.Show("Erro: Banco de dados não está conectado.");
                    return;
                }

                var collection = db.GetCollection<FornecedorData>("fornecedores");

                // Verifica se já existe um fornecedor com o mesmo CNPJ
                var fornecedorExistente = collection.FindOne(f => f.CNPJ == fornecedor.CNPJ);
                if (fornecedorExistente != null)
                {
                    MessageBox.Show("Já existe um fornecedor com este CNPJ.", "Fornecedor Existente", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                FornecedorData data = DadosDoFornecedor();
                data.SetIdFromCNPJ();

                collection.Insert(data);

                var log = new LogData
                {
                    Data = DateTime.UtcNow,
                    Tipo = "OPERACIONAL",
                    Nivel = "Usuário",
                    Detalhes = $"Fornecedor cadastrado: {data.Nome}, CNPJ: {data.CNPJ}",
                    Usuario = MainWindow.UsuarioLogado?.Nome ?? "Sistema"
                };
                await LogHistorico.SalvarLog(log);

                MessageBox.Show("Fornecedor cadastrado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao cadastrar fornecedor: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                Alerta.AdicionarAlerta("Erro",
                    ex.Message.ToString(),
                    "Erro ao cadastrar fornecedor.",
                    "- Verifique se os dados do fornecedor estão corretos.");
            }
        }

        private async void AtualizarFornecedor()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db == null)
                {
                    MessageBox.Show("Erro: Banco de dados não está conectado.");
                    return;
                }

                var collection = db.GetCollection<FornecedorData>("fornecedores");

                FornecedorData data = DadosDoFornecedor();
                data.SetIdFromCNPJ();

                collection.Update(data);

                var log = new LogData
                {
                    Data = DateTime.UtcNow,
                    Tipo = "OPERACIONAL",
                    Nivel = "Usuário",
                    Detalhes = $"Fornecedor atualizado: {data.Nome}, CNPJ: {data.CNPJ}",
                    Usuario = MainWindow.UsuarioLogado?.Nome ?? "Sistema"
                };
                await LogHistorico.SalvarLog(log);

                MessageBox.Show("Fornecedor atualizado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar fornecedor: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                Alerta.AdicionarAlerta("Erro",
                    ex.Message.ToString(),
                    "Erro ao atualizar fornecedor.",
                    "- Verifique se os dados do fornecedor estão corretos.");
            }
        }

        private FornecedorData DadosDoFornecedor() => new()
        {
            Nome = NomeTextBox.Text.Trim(),
            CNPJ = CNPJTextBox.Text.Trim(),
            Estado = EstadoTextBox.Text.Trim(),
            ComprasRelacionadas = fornecedor.ComprasRelacionadas,
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
                    "Edição de fornecedor.",
                    "Erro ao cancelar edição de fornecedor.",
                    "- Verifique se a janela de edição de fornecedor está aberta.");
                Close();
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            isModified = true;
        }

        private bool ValidarCampos()
        {
            if (string.IsNullOrWhiteSpace(NomeTextBox.Text))
            {
                MessageBox.Show("O campo Nome deve ser preenchido.");
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

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class EditarItemWindow : Window
    {
        private readonly ItemEdicaoViewModel _item;

        public EditarItemWindow(ItemEdicaoViewModel item)
        {
            InitializeComponent();
            _item = item;
            DataContext = _item;
        }

        private void QuantidadeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Permite apenas números
            if (!int.TryParse(e.Text, out _))
            {
                e.Handled = true;
                return;
            }

            // Verifica se o número total não excede o limite
            var textBox = sender as TextBox;
            string novoTexto = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            if (int.TryParse(novoTexto, out int valor))
            {
                e.Handled = valor <= 0 || valor > 9999;
            }
        }

        private void PrecoTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            string novoTexto = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            // Permite apenas números e uma vírgula
            if (e.Text == "," && !novoTexto.Contains(","))
            {
                return;
            }

            if (!double.TryParse(novoTexto.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                e.Handled = true;
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validações
                if (_item.Quantidade <= 0)
                {
                    MessageBox.Show("A quantidade deve ser maior que zero.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_item.Preco <= 0)
                {
                    MessageBox.Show("O preço deve ser maior que zero.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 
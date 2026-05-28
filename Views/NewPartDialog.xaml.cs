using System;
using System.Windows;
using CarWorkshopWPF.Data;
using CarWorkshopWPF.Models;

namespace CarWorkshopWPF.Views
{
    public partial class NewPartDialog : Window
    {
        public NewPartDialog()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var name = NameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Podaj nazwę części.", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!decimal.TryParse(PriceTextBox.Text, out var price) || price < 0)
            {
                MessageBox.Show("Cena musi być liczbą nieujemną.", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(QuantityTextBox.Text, out var qty) || qty < 0)
            {
                MessageBox.Show("Ilość musi być liczbą nieujemną.", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var ctx = new CarWorkshopContext();
                ctx.Parts.Add(new Part { Name = name, Price = price, Quantity = qty });
                ctx.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd: {ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

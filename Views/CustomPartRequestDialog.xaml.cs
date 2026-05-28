using System;
using System.Windows;
using CarWorkshopWPF.Data;
using CarWorkshopWPF.Models;

namespace CarWorkshopWPF.Views
{
    public partial class CustomPartRequestDialog : Window
    {
        private readonly int _mechanicId;

        public CustomPartRequestDialog(int mechanicId)
        {
            InitializeComponent();
            _mechanicId = mechanicId;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var name = NameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Podaj nazwę części.", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(QuantityTextBox.Text, out int qty) || qty <= 0)
            {
                MessageBox.Show("Ilość musi być liczbą dodatnią.", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var ctx = new CarWorkshopContext();
                ctx.PartRequests.Add(new PartRequest
                {
                    MechanicId = _mechanicId,
                    PartId = null,
                    CustomPartName = name,
                    Quantity = qty,
                    Status = "Brak odpowiedzi",
                    CreatedAt = DateTime.Now
                });
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

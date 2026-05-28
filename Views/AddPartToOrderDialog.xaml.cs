using System;
using System.Linq;
using System.Windows;
using CarWorkshopWPF.Data;
using CarWorkshopWPF.Models;

namespace CarWorkshopWPF.Views
{
    public partial class AddPartToOrderDialog : Window
    {
        private readonly int _orderId;

        public AddPartToOrderDialog(int orderId)
        {
            InitializeComponent();
            _orderId = orderId;

            using var ctx = new CarWorkshopContext();
            var parts = ctx.Parts.OrderBy(p => p.Name).ToList();
            foreach (var p in parts)
            {
                PartCombo.Items.Add(new ComboItem
                {
                    Display = $"{p.Name} (Cena: {(p.Price ?? 0):F2} zł, Dostępne: {p.Quantity})",
                    Value = p.Id
                });
            }
            PartCombo.DisplayMemberPath = "Display";
            PartCombo.SelectedValuePath = "Value";
            if (PartCombo.Items.Count > 0) PartCombo.SelectedIndex = 0;
        }

        private class ComboItem
        {
            public string Display { get; set; } = "";
            public int Value { get; set; }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (PartCombo.SelectedItem is not ComboItem item)
            {
                MessageBox.Show("Wybierz część.", "Błąd",
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
                var part = ctx.Parts.Find(item.Value);
                if (part == null) return;

                if ((part.Quantity ?? 0) < qty)
                {
                    MessageBox.Show($"Niewystarczająca ilość w magazynie. Dostępne: {part.Quantity ?? 0}",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var existing = ctx.OrderParts.Find(_orderId, part.Id);
                if (existing != null)
                    existing.Quantity = (existing.Quantity ?? 0) + qty;
                else
                    ctx.OrderParts.Add(new OrderPart { OrderId = _orderId, PartId = part.Id, Quantity = qty });

                part.Quantity = (part.Quantity ?? 0) - qty;
                ctx.SaveChanges();

                MessageBox.Show($"Dodano {qty} szt. części do zlecenia.", "Sukces",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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

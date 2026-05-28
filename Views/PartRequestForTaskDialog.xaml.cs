using System;
using System.Linq;
using System.Windows;
using CarWorkshopWPF.Data;
using CarWorkshopWPF.Models;

namespace CarWorkshopWPF.Views
{
    public partial class PartRequestForTaskDialog : Window
    {
        private readonly int _mechanicId;
        private readonly int _taskId;

        public PartRequestForTaskDialog(int mechanicId, int taskId)
        {
            InitializeComponent();
            _mechanicId = mechanicId;
            _taskId = taskId;

            using var ctx = new CarWorkshopContext();
            var parts = ctx.Parts.OrderBy(p => p.Name).ToList();
            PartCombo.Items.Add(new ComboItem { Display = "🛑 BRAK W MAGAZYNIE - Trzeba zamówić", Value = -1 });
            foreach (var p in parts)
                PartCombo.Items.Add(new ComboItem
                {
                    Display = $"{p.Name} (Dostępne: {p.Quantity}, Cena: {(p.Price ?? 0):F2} zł)",
                    Value = p.Id
                });
            PartCombo.DisplayMemberPath = "Display";
            PartCombo.SelectedValuePath = "Value";
            PartCombo.SelectedIndex = 0;
        }

        private class ComboItem
        {
            public string Display { get; set; } = "";
            public int Value { get; set; }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(QuantityTextBox.Text, out int qty) || qty <= 0)
            {
                MessageBox.Show("Ilość musi być liczbą dodatnią.", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (PartCombo.SelectedItem is not ComboItem item)
            {
                MessageBox.Show("Wybierz część lub wpisz nazwę niestandardową.", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var ctx = new CarWorkshopContext();
                var req = new PartRequest
                {
                    MechanicId = _mechanicId,
                    RepairTaskId = _taskId,
                    Quantity = qty,
                    Status = "Brak odpowiedzi",
                    CreatedAt = DateTime.Now
                };

                if (item.Value == -1)
                {
                    var customName = CustomNameTextBox.Text.Trim();
                    if (string.IsNullOrWhiteSpace(customName))
                    {
                        MessageBox.Show("Podaj nazwę części niestandardowej.", "Błąd",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    req.CustomPartName = customName;
                    req.PartId = null;
                }
                else
                {
                    req.PartId = item.Value;
                    req.CustomPartName = null;
                }

                ctx.PartRequests.Add(req);
                ctx.SaveChanges();

                MessageBox.Show("Zgłoszenie wysłane do magazyniera.", "Sukces",
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

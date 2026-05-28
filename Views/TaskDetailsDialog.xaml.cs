using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CarWorkshopWPF.Data;
using CarWorkshopWPF.Models;
using Microsoft.EntityFrameworkCore;

namespace CarWorkshopWPF.Views
{
    public partial class TaskDetailsDialog : Window
    {
        private readonly int _orderId;
        private readonly int _mechanicId;
        private readonly Dictionary<int, CheckBox> _completedChecks = new();
        private readonly Dictionary<int, CheckBox> _needsPartsChecks = new();

        public TaskDetailsDialog(int orderId, int mechanicId)
        {
            InitializeComponent();
            _orderId = orderId;
            _mechanicId = mechanicId;
            LoadOrderDetails();
        }

        private void LoadOrderDetails()
        {
            using var ctx = new CarWorkshopContext();
            var order = ctx.RepairOrders
                .Include(o => o.Vehicle).ThenInclude(v => v!.Customer)
                .Include(o => o.RepairTasks)
                .FirstOrDefault(o => o.Id == _orderId);

            if (order == null)
            {
                MessageBox.Show("Nie znaleziono zlecenia.", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            string vehicle = order.Vehicle != null
                ? $"{order.Vehicle.Make} {order.Vehicle.Model} ({order.Vehicle.LicensePlate})"
                : "Nieznany";
            string client = order.Vehicle?.Customer != null
                ? $"{order.Vehicle.Customer.FirstName} {order.Vehicle.Customer.LastName}"
                : "Nieznany";

            HeaderText.Text = $"🚗 {vehicle}";
            InfoText.Text = $"Klient: {client}\nStatus: {order.Status}\nUtworzono: {order.CreatedAt:dd.MM.yyyy HH:mm}";

            TasksPanel.Children.Clear();
            _completedChecks.Clear();
            _needsPartsChecks.Clear();

            if (!order.RepairTasks.Any())
            {
                TasksPanel.Children.Add(new TextBlock
                {
                    Text = "Brak zadań w tym zleceniu.",
                    FontStyle = FontStyles.Italic,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(20)
                });
                return;
            }

            int idx = 1;
            foreach (var task in order.RepairTasks)
            {
                var border = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 0, 8),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    BorderThickness = new Thickness(1)
                };

                var stack = new StackPanel();

                var titleRow = new Grid();
                titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var title = new TextBlock { Text = $"Zadanie {idx++}", FontWeight = FontWeights.Bold, FontSize = 14 };
                var price = new TextBlock { Text = $"{(task.Price ?? 0):F2} zł", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(39, 174, 96)) };
                Grid.SetColumn(price, 1);
                titleRow.Children.Add(title);
                titleRow.Children.Add(price);
                stack.Children.Add(titleRow);

                stack.Children.Add(new TextBlock { Text = task.Description, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 8) });

                var cbRow = new StackPanel { Orientation = Orientation.Horizontal };
                var cbCompleted = new CheckBox { Content = "✓ Wykonane", IsChecked = task.IsCompleted, Margin = new Thickness(0, 0, 20, 0), FontWeight = FontWeights.Bold };
                var cbNeedsParts = new CheckBox { Content = "🔧 Brakuje części", IsChecked = task.NeedsParts, FontWeight = FontWeights.Bold };
                cbRow.Children.Add(cbCompleted);
                cbRow.Children.Add(cbNeedsParts);
                stack.Children.Add(cbRow);

                _completedChecks[task.Id] = cbCompleted;
                _needsPartsChecks[task.Id] = cbNeedsParts;

                cbCompleted.Checked += (s, e) => cbNeedsParts.IsChecked = false;
                cbNeedsParts.Checked += (s, e) =>
                {
                    cbCompleted.IsChecked = false;
                    AskForPartRequest(task);
                };

                border.Child = stack;
                TasksPanel.Children.Add(border);
            }
        }

        private void AskForPartRequest(RepairTask task)
        {
            using var ctx = new CarWorkshopContext();
            // Sprawdź czy już istnieje zgłoszenie
            bool exists = ctx.PartRequests.Any(r => r.MechanicId == _mechanicId && r.RepairTaskId == task.Id);
            if (exists)
            {
                MessageBox.Show("Zgłoszenie dla tego zadania już zostało wysłane.", "Informacja",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Czy chcesz wysłać zgłoszenie do magazynu o części dla:\n\n{task.Description}",
                "Zgłoszenie do magazynu",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                _needsPartsChecks[task.Id].IsChecked = false;
                return;
            }

            var dlg = new PartRequestForTaskDialog(_mechanicId, task.Id);
            dlg.Owner = this;
            if (dlg.ShowDialog() != true)
                _needsPartsChecks[task.Id].IsChecked = false;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var ctx = new CarWorkshopContext();
                var order = ctx.RepairOrders.Include(o => o.RepairTasks)
                    .FirstOrDefault(o => o.Id == _orderId);
                if (order == null) return;

                foreach (var t in order.RepairTasks)
                {
                    if (_completedChecks.TryGetValue(t.Id, out var c)) t.IsCompleted = c.IsChecked == true;
                    if (_needsPartsChecks.TryGetValue(t.Id, out var n)) t.NeedsParts = n.IsChecked == true;
                }

                // Auto-status
                if (order.RepairTasks.Any())
                {
                    bool anyNeedsParts = order.RepairTasks.Any(t => t.NeedsParts);
                    int completedCount = order.RepairTasks.Count(t => t.IsCompleted);
                    int total = order.RepairTasks.Count;

                    string newStatus;
                    if (anyNeedsParts) newStatus = "Czeka na części";
                    else if (completedCount == total)
                    {
                        newStatus = "Gotowe do odbioru";
                        order.CompletedAt = DateTime.Now;
                    }
                    else if (completedCount > 0) newStatus = "W trakcie";
                    else newStatus = order.Status == "W trakcie" ? "W trakcie" : "Nierozpoczęte";

                    order.Status = newStatus;
                }

                ctx.SaveChanges();
                MessageBox.Show($"Zmiany zapisane.\nStatus: {order.Status}", "Sukces",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadOrderDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisu: {ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddPartButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new AddPartToOrderDialog(_orderId);
            dlg.Owner = this;
            dlg.ShowDialog();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}

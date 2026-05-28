using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CarWorkshopWPF.Data;
using CarWorkshopWPF.Models;
using Microsoft.EntityFrameworkCore;

namespace CarWorkshopWPF.Views
{
    public partial class RecepcjaWindow : Window
    {
        private readonly User _user;
        private readonly DispatcherTimer _clockTimer;
        private readonly Dictionary<string, int> _mechanicsMap = new();

        public RecepcjaWindow(User user)
        {
            InitializeComponent();
            _user = user;
            labelUser.Text = $"👤 {_user.Login}  |  RECEPCJA";

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => UpdateTime();
            _clockTimer.Start();
            UpdateTime();
            Closed += (s, e) => _clockTimer.Stop();

            LoadMechanics();
            ShowDashboard();
        }

        private void UpdateTime()
        {
            var now = DateTime.Now;
            DataLabel.Text = $"📅 {now:dd.MM.yyyy}";
            TimeLabel.Text = $"🕐 {now:HH:mm:ss}";
        }

        private void SetActiveButton(Button active)
        {
            foreach (var btn in new[] { buttonMain, buttonReadyCars, buttonNewOrder, buttonOrders })
            {
                btn.Style = btn == active
                    ? (Style)FindResource("NavButtonActive")
                    : (Style)FindResource("NavButton");
            }
        }

        private void buttonMain_Click(object sender, RoutedEventArgs e) => ShowDashboard();
        private void buttonReadyCars_Click(object sender, RoutedEventArgs e) => ShowReadyCars();
        private void buttonNewOrder_Click(object sender, RoutedEventArgs e) => ShowNewOrder();
        private void buttonOrders_Click(object sender, RoutedEventArgs e) => ShowOrders();

        private void ShowDashboard()
        {
            stackedWidget.SelectedIndex = 0;
            SetActiveButton(buttonMain);
            SetupDashboard();
        }

        private void ShowReadyCars()
        {
            stackedWidget.SelectedIndex = 1;
            SetActiveButton(buttonReadyCars);
            LoadReadyCars();
        }

        private void ShowNewOrder()
        {
            stackedWidget.SelectedIndex = 2;
            SetActiveButton(buttonNewOrder);
            LoadMechanics();
        }

        private void ShowOrders()
        {
            stackedWidget.SelectedIndex = 3;
            SetActiveButton(buttonOrders);
            LoadOrders();
        }

        private void SetupDashboard()
        {
            var firstName = _user.FirstName ?? _user.Login;
            var lastName = _user.LastName ?? "";
            WelcomeLabel.Text = $"Witaj, {firstName}!";
            FullNameLabel.Text = $"👤 {firstName} {lastName}";
            RoleLabel.Text = $"🔑 Stanowisko: RECEPCJA";
            PhoneLabel.Text = $"📞 Telefon: {_user.Phone ?? "Brak"}";
            EmailLabel.Text = $"✉ Email: {_user.Email ?? "Brak"}";

            try
            {
                using var ctx = new CarWorkshopContext();
                int total = ctx.RepairOrders.Count();
                int notStarted = ctx.RepairOrders.Count(r => r.Status == "Nierozpoczęte");
                int inProgress = ctx.RepairOrders.Count(r => r.Status == "W trakcie");
                int ready = ctx.RepairOrders.Count(r => r.Status == "Gotowe do odbioru");

                totalOrdersLabel.Text = $"Wszystkich zleceń: {total}";
                notStartedLabel.Text = $"⏳ Nierozpoczęte: {notStarted}";
                inProgressLabel.Text = $"🔧 W trakcie: {inProgress}";
                readyLabel.Text = $"✅ Gotowe do odbioru: {ready}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMechanics()
        {
            try
            {
                using var ctx = new CarWorkshopContext();
                var mechanics = ctx.Users.Where(u => u.Role == "mechanic").ToList();
                comboMechanic.Items.Clear();
                _mechanicsMap.Clear();
                foreach (var m in mechanics)
                {
                    var display = $"{m.FirstName ?? m.Login} {m.LastName ?? ""}".Trim();
                    comboMechanic.Items.Add(display);
                    _mechanicsMap[display] = m.Id;
                }
                if (comboMechanic.Items.Count > 0) comboMechanic.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd ładowania mechaników: {ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === Auta gotowe ===

        private void LoadReadyCars()
        {
            try
            {
                using var ctx = new CarWorkshopContext();
                var rows = ctx.RepairOrders
                    .Include(o => o.Vehicle).ThenInclude(v => v!.Customer)
                    .Include(o => o.Mechanic)
                    .Include(o => o.RepairTasks)
                    .Include(o => o.OrderParts).ThenInclude(op => op.Part)
                    .Where(o => o.Status == "Gotowe do odbioru")
                    .ToList()
                    .Select(o =>
                    {
                        decimal tasksTotal = o.RepairTasks.Sum(t => t.Price ?? 0);
                        decimal partsTotal = o.OrderParts.Sum(op => (op.Part?.Price ?? 0) * (op.Quantity ?? 0));
                        return new
                        {
                            o.Id,
                            Client = o.Vehicle?.Customer != null
                                ? $"{o.Vehicle.Customer.FirstName} {o.Vehicle.Customer.LastName}" : "",
                            Make = o.Vehicle?.Make ?? "",
                            Model = o.Vehicle?.Model ?? "",
                            Plate = o.Vehicle?.LicensePlate ?? "",
                            Mechanic = o.Mechanic != null ? $"{o.Mechanic.FirstName} {o.Mechanic.LastName}" : "",
                            Total = $"{tasksTotal + partsTotal:F2}",
                            CompletedStr = o.CompletedAt?.ToString("yyyy-MM-dd HH:mm") ?? ""
                        };
                    })
                    .ToList();
                tableReadyCars.ItemsSource = rows;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void refreshReadyCars_Click(object sender, RoutedEventArgs e) => LoadReadyCars();

        private int? GetSelectedReadyId()
        {
            if (tableReadyCars.SelectedItem == null) return null;
            return tableReadyCars.SelectedItem.GetType().GetProperty("Id")?.GetValue(tableReadyCars.SelectedItem) as int?;
        }

        private void showReadySummary_Click(object sender, RoutedEventArgs e)
        {
            var id = GetSelectedReadyId();
            if (id == null) { MessageBox.Show("Wybierz zlecenie.", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            ShowSummary(id.Value);
        }

        private void markPickedUp_Click(object sender, RoutedEventArgs e)
        {
            var id = GetSelectedReadyId();
            if (id == null) { MessageBox.Show("Wybierz zlecenie.", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            var confirm = MessageBox.Show("Czy klient odebrał pojazd i zapłacił?", "Potwierdzenie",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using var ctx = new CarWorkshopContext();
                var order = ctx.RepairOrders.Find(id.Value);
                if (order != null)
                {
                    order.Status = "Odebrane";
                    ctx.SaveChanges();
                    LoadReadyCars();
                    SetupDashboard();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd: {ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === Nowe zlecenie ===

        private void btnSubmitOrder_Click(object sender, RoutedEventArgs e)
        {
            var firstName = inputFirstName.Text.Trim();
            var lastName = inputLastName.Text.Trim();
            var make = inputMake.Text.Trim();
            var model = inputModel.Text.Trim();
            var plate = inputPlate.Text.Trim();
            var vin = inputVin.Text.Trim();

            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            {
                MessageBox.Show("Podaj imię i nazwisko klienta.", "Uwaga",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(make) || string.IsNullOrEmpty(model))
            {
                MessageBox.Show("Podaj markę i model pojazdu.", "Uwaga",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(plate))
            {
                MessageBox.Show("Podaj numer rejestracyjny.", "Uwaga",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var mechanicDisplay = comboMechanic.SelectedItem as string ?? "";
            if (!_mechanicsMap.ContainsKey(mechanicDisplay))
            {
                MessageBox.Show("Wybierz mechanika.", "Uwaga",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int? year = null;
            if (!string.IsNullOrEmpty(inputYear.Text))
            {
                if (!int.TryParse(inputYear.Text, out var y))
                {
                    MessageBox.Show("Rok produkcji musi być liczbą.", "Uwaga",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                year = y;
            }

            // Zadania
            var tasksDlg = new AddTasksDialog();
            tasksDlg.Owner = this;
            if (tasksDlg.ShowDialog() != true || tasksDlg.Tasks.Count == 0)
            {
                MessageBox.Show("Musisz dodać przynajmniej jedno zadanie.", "Uwaga",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var ctx = new CarWorkshopContext();

                var customer = ctx.Customers.FirstOrDefault(c =>
                    c.FirstName == firstName && c.LastName == lastName);
                if (customer == null)
                {
                    customer = new Customer
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        Phone = string.IsNullOrEmpty(inputClientPhone.Text) ? null : inputClientPhone.Text.Trim(),
                        Email = string.IsNullOrEmpty(inputClientEmail.Text) ? null : inputClientEmail.Text.Trim(),
                        CreatedAt = DateTime.Now
                    };
                    ctx.Customers.Add(customer);
                    ctx.SaveChanges();
                }

                Vehicle? vehicle = null;
                if (!string.IsNullOrEmpty(vin))
                    vehicle = ctx.Vehicles.FirstOrDefault(v => v.Vin == vin);
                if (vehicle == null && !string.IsNullOrEmpty(plate))
                    vehicle = ctx.Vehicles.FirstOrDefault(v => v.LicensePlate == plate);
                if (vehicle == null)
                {
                    vehicle = new Vehicle
                    {
                        CustomerId = customer.Id,
                        Make = make,
                        Model = model,
                        Year = year,
                        LicensePlate = plate,
                        Vin = string.IsNullOrEmpty(vin) ? null : vin
                    };
                    ctx.Vehicles.Add(vehicle);
                    ctx.SaveChanges();
                }

                var order = new RepairOrder
                {
                    VehicleId = vehicle.Id,
                    MechanicId = _mechanicsMap[mechanicDisplay],
                    Status = "Nierozpoczęte",
                    Description = "",
                    CreatedAt = DateTime.Now
                };
                ctx.RepairOrders.Add(order);
                ctx.SaveChanges();

                foreach (var t in tasksDlg.Tasks)
                {
                    ctx.RepairTasks.Add(new RepairTask
                    {
                        RepairOrderId = order.Id,
                        ServiceTypeId = t.ServiceTypeId,
                        Description = t.Description,
                        Price = t.Price,
                        IsCompleted = false,
                        NeedsParts = false,
                        CreatedAt = DateTime.Now
                    });
                }
                ctx.SaveChanges();

                MessageBox.Show(
                    $"Zlecenie dodane!\n\nKlient: {firstName} {lastName}\nPojazd: {make} {model} ({plate})\nLiczba zadań: {tasksDlg.Tasks.Count}",
                    "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);

                ClearForm();
                SetupDashboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nie udało się dodać zlecenia:\n{ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClearForm_Click(object sender, RoutedEventArgs e) => ClearForm();

        private void ClearForm()
        {
            inputFirstName.Clear();
            inputLastName.Clear();
            inputClientPhone.Clear();
            inputClientEmail.Clear();
            inputMake.Clear();
            inputModel.Clear();
            inputYear.Clear();
            inputPlate.Clear();
            inputVin.Clear();
        }

        // === Lista zleceń ===

        private void LoadOrders()
        {
            try
            {
                using var ctx = new CarWorkshopContext();
                IQueryable<RepairOrder> q = ctx.RepairOrders
                    .Include(o => o.Vehicle).ThenInclude(v => v!.Customer)
                    .Include(o => o.Mechanic)
                    .Include(o => o.RepairTasks)
                    .Include(o => o.OrderParts).ThenInclude(op => op.Part);

                var statusText = (ordersStatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Wszystkie";
                if (statusText != "Wszystkie")
                    q = q.Where(o => o.Status == statusText);

                var rows = q.ToList().Select(o =>
                {
                    decimal tasksTotal = o.RepairTasks.Sum(t => t.Price ?? 0);
                    decimal partsTotal = o.OrderParts.Sum(op => (op.Part?.Price ?? 0) * (op.Quantity ?? 0));
                    int completed = o.RepairTasks.Count(t => t.IsCompleted);
                    return new
                    {
                        o.Id,
                        Client = o.Vehicle?.Customer != null
                            ? $"{o.Vehicle.Customer.FirstName} {o.Vehicle.Customer.LastName}" : "",
                        Vehicle = o.Vehicle != null ? $"{o.Vehicle.Make} {o.Vehicle.Model} ({o.Vehicle.LicensePlate})" : "",
                        Mechanic = o.Mechanic != null ? $"{o.Mechanic.FirstName} {o.Mechanic.LastName}" : "",
                        Status = o.Status,
                        Tasks = $"{completed}/{o.RepairTasks.Count}",
                        Total = $"{tasksTotal + partsTotal:F2}",
                        CreatedStr = o.CreatedAt?.ToString("yyyy-MM-dd HH:mm") ?? ""
                    };
                }).ToList();
                tableOrders.ItemsSource = rows;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ordersStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) LoadOrders();
        }

        private void refreshOrders_Click(object sender, RoutedEventArgs e) => LoadOrders();

        private int? GetSelectedOrderId()
        {
            if (tableOrders.SelectedItem == null) return null;
            return tableOrders.SelectedItem.GetType().GetProperty("Id")?.GetValue(tableOrders.SelectedItem) as int?;
        }

        private void showOrderSummary_Click(object sender, RoutedEventArgs e)
        {
            var id = GetSelectedOrderId();
            if (id == null) { MessageBox.Show("Wybierz zlecenie.", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            ShowSummary(id.Value);
        }

        private void deleteOrder_Click(object sender, RoutedEventArgs e)
        {
            var id = GetSelectedOrderId();
            if (id == null) { MessageBox.Show("Wybierz zlecenie.", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            var confirm = MessageBox.Show("Czy na pewno chcesz usunąć to zlecenie?", "Potwierdzenie",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using var ctx = new CarWorkshopContext();
                // Usuń powiązane (taski, części)
                var tasks = ctx.RepairTasks.Where(t => t.RepairOrderId == id).ToList();
                ctx.RepairTasks.RemoveRange(tasks);
                var parts = ctx.OrderParts.Where(op => op.OrderId == id).ToList();
                ctx.OrderParts.RemoveRange(parts);
                var order = ctx.RepairOrders.Find(id.Value);
                if (order != null) ctx.RepairOrders.Remove(order);
                ctx.SaveChanges();
                LoadOrders();
                SetupDashboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nie udało się usunąć: {ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowSummary(int orderId)
        {
            try
            {
                using var ctx = new CarWorkshopContext();
                var o = ctx.RepairOrders
                    .Include(x => x.Vehicle).ThenInclude(v => v!.Customer)
                    .Include(x => x.Mechanic)
                    .Include(x => x.RepairTasks)
                    .Include(x => x.OrderParts).ThenInclude(op => op.Part)
                    .FirstOrDefault(x => x.Id == orderId);
                if (o == null) return;

                string client = o.Vehicle?.Customer != null
                    ? $"{o.Vehicle.Customer.FirstName} {o.Vehicle.Customer.LastName}" : "";
                string vehicle = o.Vehicle != null
                    ? $"{o.Vehicle.Make} {o.Vehicle.Model} ({o.Vehicle.LicensePlate})" : "";
                string mechanic = o.Mechanic != null ? $"{o.Mechanic.FirstName} {o.Mechanic.LastName}" : "";

                decimal tasksTotal = 0;
                var taskLines = new List<string>();
                foreach (var t in o.RepairTasks)
                {
                    decimal price = t.Price ?? 0;
                    tasksTotal += price;
                    string icon = t.IsCompleted ? "✓" : "○";
                    taskLines.Add($"  {icon} {t.Description}: {price:F2} zł");
                }

                decimal partsTotal = 0;
                var partLines = new List<string>();
                foreach (var op in o.OrderParts)
                {
                    if (op.Part == null) continue;
                    decimal price = op.Part.Price ?? 0;
                    decimal lineTotal = price * (op.Quantity ?? 0);
                    partsTotal += lineTotal;
                    partLines.Add($"  • {op.Part.Name}: {op.Quantity} szt. × {price:F2} zł = {lineTotal:F2} zł");
                }

                decimal total = tasksTotal + partsTotal;

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("═══════════════════════════════════════════");
                sb.AppendLine("         PODSUMOWANIE ZLECENIA");
                sb.AppendLine("═══════════════════════════════════════════\n");
                sb.AppendLine("📋 INFORMACJE PODSTAWOWE:");
                sb.AppendLine($"  Klient: {client}");
                sb.AppendLine($"  Pojazd: {vehicle}");
                sb.AppendLine($"  Mechanik: {mechanic}");
                sb.AppendLine($"  Status: {o.Status}\n");
                sb.AppendLine("💰 KOSZTY:");
                sb.AppendLine("\n  Zadania naprawcze:");
                sb.AppendLine(taskLines.Count > 0 ? string.Join("\n", taskLines) : "  (brak zadań)");
                sb.AppendLine($"\n  Zadania razem: {tasksTotal:F2} zł");
                sb.AppendLine("\n  Części zamienne:");
                sb.AppendLine(partLines.Count > 0 ? string.Join("\n", partLines) : "  (brak użytych części)");
                sb.AppendLine($"\n  Części razem: {partsTotal:F2} zł\n");
                sb.AppendLine("═════════════════════════════════════════");
                sb.AppendLine($"  SUMA DO ZAPŁATY: {total:F2} zł");
                sb.AppendLine("═════════════════════════════════════════");

                MessageBox.Show(sb.ToString(), "Podsumowanie zlecenia",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd: {ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === Inne ===

        private void editProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new UpdateProfileDialog(_user);
            dlg.Owner = this;
            if (dlg.ShowDialog() == true) SetupDashboard();
        }

        private void buttonLogout_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            Close();
        }
    }
}

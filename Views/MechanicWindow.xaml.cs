using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CarWorkshopWPF.Data;
using CarWorkshopWPF.Models;
using Microsoft.EntityFrameworkCore;

namespace CarWorkshopWPF.Views
{
    public partial class MechanicWindow : Window
    {
        private readonly User _user;
        private readonly DispatcherTimer _clockTimer;

        public MechanicWindow(User user)
        {
            InitializeComponent();
            _user = user;
            labelUser.Text = $"👤 {_user.Login}  |  MECHANIK";

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => UpdateTime();
            _clockTimer.Start();
            UpdateTime();
            Closed += (s, e) => _clockTimer.Stop();

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
            foreach (var btn in new[] { buttonMain, buttonCars, buttonParts, buttonPartRequests })
            {
                btn.Style = btn == active
                    ? (Style)FindResource("NavButtonActive")
                    : (Style)FindResource("NavButton");
            }
        }

        private void buttonMain_Click(object sender, RoutedEventArgs e) => ShowDashboard();
        private void buttonCars_Click(object sender, RoutedEventArgs e) => ShowCars();
        private void buttonParts_Click(object sender, RoutedEventArgs e) => ShowParts();
        private void buttonPartRequests_Click(object sender, RoutedEventArgs e) => ShowRequests();

        private void ShowDashboard()
        {
            stackedWidget.SelectedIndex = 0;
            SetActiveButton(buttonMain);
            SetupDashboard();
        }

        private void ShowCars()
        {
            stackedWidget.SelectedIndex = 1;
            SetActiveButton(buttonCars);
            LoadCars();
        }

        private void ShowParts()
        {
            stackedWidget.SelectedIndex = 2;
            SetActiveButton(buttonParts);
            LoadParts();
        }

        private void ShowRequests()
        {
            stackedWidget.SelectedIndex = 3;
            SetActiveButton(buttonPartRequests);
            LoadRequests();
        }

        private void SetupDashboard()
        {
            var firstName = _user.FirstName ?? _user.Login;
            var lastName = _user.LastName ?? "";
            WelcomeLabel.Text = $"Witaj, {firstName}! ✋";
            FullNameLabel.Text = $"👤 {firstName} {lastName}";
            RoleLabel.Text = $"🔑 Stanowisko: MECHANIK";
            PhoneLabel.Text = $"📞 Telefon: {_user.Phone ?? "Brak"}";
            EmailLabel.Text = $"✉ Email: {_user.Email ?? "Brak"}";

            try
            {
                using var ctx = new CarWorkshopContext();
                int notStarted = ctx.RepairOrders.Count(r => r.MechanicId == _user.Id && r.Status == "Nierozpoczęte");
                int inProgress = ctx.RepairOrders.Count(r => r.MechanicId == _user.Id && r.Status == "W trakcie");
                int ready = ctx.RepairOrders.Count(r => r.MechanicId == _user.Id && r.Status == "Gotowe do odbioru");

                notStartedLabel.Text = $"⏳ Nierozpoczęte: {notStarted}";
                inProgressLabel.Text = $"🔧 W trakcie: {inProgress}";
                readyLabel.Text = $"✅ Gotowe do odbioru: {ready}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCars()
        {
            try
            {
                using var ctx = new CarWorkshopContext();
                IQueryable<RepairOrder> q = ctx.RepairOrders
                    .Include(r => r.Vehicle).ThenInclude(v => v!.Customer)
                    .Include(r => r.RepairTasks)
                    .Include(r => r.OrderParts).ThenInclude(op => op.Part)
                    .Where(r => r.MechanicId == _user.Id);

                var statusText = (statusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Wszystkie";
                if (statusText != "Wszystkie")
                    q = q.Where(r => r.Status == statusText);

                var rows = q.ToList().Select(r =>
                {
                    decimal tasksTotal = r.RepairTasks.Sum(t => t.Price ?? 0);
                    decimal partsTotal = r.OrderParts.Sum(op => (op.Part?.Price ?? 0) * (op.Quantity ?? 0));
                    int completed = r.RepairTasks.Count(t => t.IsCompleted);
                    return new
                    {
                        r.Id,
                        Make = r.Vehicle?.Make ?? "",
                        Model = r.Vehicle?.Model ?? "",
                        Plate = r.Vehicle?.LicensePlate ?? "",
                        Client = r.Vehicle?.Customer != null
                            ? $"{r.Vehicle.Customer.FirstName} {r.Vehicle.Customer.LastName}"
                            : "",
                        r.Status,
                        Tasks = $"{completed}/{r.RepairTasks.Count}",
                        Total = $"{tasksTotal + partsTotal:F2}",
                        OrderId = r.Id
                    };
                }).ToList();
                tableCars.ItemsSource = rows;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void statusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) LoadCars();
        }

        private void refreshCars_Click(object sender, RoutedEventArgs e) => LoadCars();

        private int? GetSelectedCarOrderId()
        {
            if (tableCars.SelectedItem == null) return null;
            var prop = tableCars.SelectedItem.GetType().GetProperty("Id");
            return prop?.GetValue(tableCars.SelectedItem) as int?;
        }

        private void viewTasks_Click(object sender, RoutedEventArgs e)
        {
            var id = GetSelectedCarOrderId();
            if (id == null)
            {
                MessageBox.Show("Wybierz zlecenie z listy.", "Uwaga",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var dlg = new TaskDetailsDialog(id.Value, _user.Id);
            dlg.Owner = this;
            dlg.ShowDialog();
            LoadCars();
        }

        private void changeStatus_Click(object sender, RoutedEventArgs e)
        {
            var id = GetSelectedCarOrderId();
            if (id == null)
            {
                MessageBox.Show("Wybierz zlecenie z listy.", "Uwaga",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var statuses = new[] { "Nierozpoczęte", "W trakcie", "Czeka na części", "Gotowe do odbioru" };
            var dlg = new ChooseStatusDialog(statuses);
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    using var ctx = new CarWorkshopContext();
                    var order = ctx.RepairOrders.Find(id.Value);
                    if (order != null)
                    {
                        order.Status = dlg.Selected!;
                        if (dlg.Selected == "Gotowe do odbioru" && order.CompletedAt == null)
                            order.CompletedAt = DateTime.Now;
                        ctx.SaveChanges();
                        LoadCars();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd: {ex.Message}", "Błąd",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // === Magazyn ===

        private void LoadParts()
        {
            try
            {
                using var ctx = new CarWorkshopContext();
                var search = partsSearchInput.Text?.Trim().ToLower() ?? "";
                var parts = ctx.Parts.AsEnumerable()
                    .Where(p => string.IsNullOrEmpty(search) || (p.Name?.ToLower().Contains(search) ?? false))
                    .OrderBy(p => p.Name)
                    .Select(p => new
                    {
                        p.Id, p.Name,
                        Price = $"{(p.Price ?? 0):F2}",
                        Quantity = p.Quantity ?? 0
                    })
                    .ToList();
                tableParts.ItemsSource = parts;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void partsSearchInput_TextChanged(object sender, TextChangedEventArgs e) => LoadParts();
        private void refreshParts_Click(object sender, RoutedEventArgs e)
        {
            partsSearchInput.Text = "";
            LoadParts();
        }

        private void requestCustomPart_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CustomPartRequestDialog(_user.Id);
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                MessageBox.Show("Zgłoszenie wysłane do magazyniera.", "Sukces",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadRequests();
            }
        }

        // === Moje zgłoszenia ===

        private void LoadRequests()
        {
            try
            {
                using var ctx = new CarWorkshopContext();
                var rows = ctx.PartRequests
                    .Include(r => r.Part)
                    .Where(r => r.MechanicId == _user.Id)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToList()
                    .Select(r => new
                    {
                        r.Id,
                        PartName = r.Part != null ? r.Part.Name : (r.CustomPartName ?? "Nieznana"),
                        Quantity = r.Quantity ?? 0,
                        Type = r.Part != null ? "✅ W magazynie" : "🛑 Do zamówienia",
                        Status = r.Status ?? "Brak odpowiedzi",
                        CreatedStr = r.CreatedAt?.ToString("yyyy-MM-dd HH:mm") ?? ""
                    })
                    .ToList();
                tableRequests.ItemsSource = rows;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void refreshRequests_Click(object sender, RoutedEventArgs e) => LoadRequests();

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

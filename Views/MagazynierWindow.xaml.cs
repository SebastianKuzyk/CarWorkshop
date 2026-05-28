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
    public partial class MagazynierWindow : Window
    {
        private readonly User _user;
        private readonly DispatcherTimer _clockTimer;

        public MagazynierWindow(User user)
        {
            InitializeComponent();
            _user = user;
            labelUser.Text = $"👤 {_user.Login}  |  MAGAZYNIER";

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
            foreach (var btn in new[] { buttonMain, buttonParts, buttonRequests })
            {
                btn.Style = btn == active
                    ? (Style)FindResource("NavButtonActive")
                    : (Style)FindResource("NavButton");
            }
        }

        private void buttonMain_Click(object sender, RoutedEventArgs e) => ShowDashboard();
        private void buttonParts_Click(object sender, RoutedEventArgs e) => ShowParts();
        private void buttonRequests_Click(object sender, RoutedEventArgs e) => ShowRequests();

        private void ShowDashboard()
        {
            stackedWidget.SelectedIndex = 0;
            SetActiveButton(buttonMain);
            SetupDashboard();
        }

        private void ShowParts()
        {
            stackedWidget.SelectedIndex = 1;
            SetActiveButton(buttonParts);
            LoadParts();
        }

        private void ShowRequests()
        {
            stackedWidget.SelectedIndex = 2;
            SetActiveButton(buttonRequests);
            LoadRequests();
        }

        private void SetupDashboard()
        {
            var firstName = _user.FirstName ?? _user.Login;
            var lastName = _user.LastName ?? "";
            WelcomeLabel.Text = $"Witaj, {firstName}! ✋";
            FullNameLabel.Text = $"👤 {firstName} {lastName}";
            RoleLabel.Text = $"🔑 Stanowisko: MAGAZYNIER";
            PhoneLabel.Text = $"📞 Telefon: {_user.Phone ?? "Brak"}";
            EmailLabel.Text = $"✉ Email: {_user.Email ?? "Brak"}";

            try
            {
                using var ctx = new CarWorkshopContext();
                int total = ctx.Parts.Count();
                int low = ctx.Parts.Count(p => (p.Quantity ?? 0) < 10);
                int newReqs = ctx.PartRequests.Count(r => r.Status == "Brak odpowiedzi");

                totalPartsLabel.Text = $"📦 Różnych części w bazie: {total}";
                lowStockLabel.Text = $"⚠ Części na wyczerpaniu (<10): {low}";
                newRequestsLabel.Text = $"🎟 Nowe zgłoszenia: {newReqs}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === Magazyn ===

        private void LoadParts()
        {
            try
            {
                using var ctx = new CarWorkshopContext();
                var search = partsSearchInput.Text?.Trim().ToLower() ?? "";
                var rows = ctx.Parts.AsEnumerable()
                    .Where(p => string.IsNullOrEmpty(search) || (p.Name?.ToLower().Contains(search) ?? false))
                    .OrderBy(p => p.Name)
                    .Select(p => new
                    {
                        p.Id, p.Name,
                        Price = $"{(p.Price ?? 0):F2}",
                        Quantity = p.Quantity ?? 0
                    })
                    .ToList();
                tableParts.ItemsSource = rows;
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

        private int? GetSelectedPartId()
        {
            if (tableParts.SelectedItem == null) return null;
            return tableParts.SelectedItem.GetType().GetProperty("Id")?.GetValue(tableParts.SelectedItem) as int?;
        }

        private void receiveDelivery_Click(object sender, RoutedEventArgs e)
        {
            var id = GetSelectedPartId();
            if (id == null) { MessageBox.Show("Wybierz część z listy.", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            var dlg = new InputNumberDialog("Odbiór dostawy", "Ile sztuk dojechało?", 1);
            dlg.Owner = this;
            if (dlg.ShowDialog() != true) return;

            try
            {
                using var ctx = new CarWorkshopContext();
                var part = ctx.Parts.Find(id.Value);
                if (part != null)
                {
                    part.Quantity = (part.Quantity ?? 0) + dlg.Value;
                    ctx.SaveChanges();
                    MessageBox.Show($"Dodano {dlg.Value} szt. do {part.Name}. Nowy stan: {part.Quantity}",
                        "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadParts();
                    SetupDashboard();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd: {ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void addNewPart_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new NewPartDialog();
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                LoadParts();
                SetupDashboard();
            }
        }

        // === Zgłoszenia ===

        private void LoadRequests()
        {
            try
            {
                using var ctx = new CarWorkshopContext();
                IQueryable<PartRequest> q = ctx.PartRequests
                    .Include(r => r.Mechanic)
                    .Include(r => r.Part);

                var statusText = (filterRequestStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Wszystkie";
                if (statusText != "Wszystkie")
                    q = q.Where(r => r.Status == statusText);

                var rows = q.OrderByDescending(r => r.CreatedAt).ToList()
                    .Select(r => new
                    {
                        r.Id,
                        MechanicName = r.Mechanic != null
                            ? $"{r.Mechanic.FirstName} {r.Mechanic.LastName}"
                            : "Nieznany",
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

        private void filterRequestStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) LoadRequests();
        }

        private void refreshRequests_Click(object sender, RoutedEventArgs e) => LoadRequests();

        private void changeRequestStatus_Click(object sender, RoutedEventArgs e)
        {
            if (tableRequests.SelectedItem == null)
            {
                MessageBox.Show("Wybierz zgłoszenie z listy.", "Uwaga",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int? id = tableRequests.SelectedItem.GetType().GetProperty("Id")?.GetValue(tableRequests.SelectedItem) as int?;
            if (id == null) return;

            var statuses = new[] { "Brak odpowiedzi", "Zamówiona", "Gotowa do odbioru" };
            var dlg = new ChooseStatusDialog(statuses);
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    using var ctx = new CarWorkshopContext();
                    var req = ctx.PartRequests.Find(id.Value);
                    if (req != null)
                    {
                        req.Status = dlg.Selected!;
                        ctx.SaveChanges();
                        LoadRequests();
                        SetupDashboard();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd: {ex.Message}", "Błąd",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

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

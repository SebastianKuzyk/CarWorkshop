using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CarWorkshopWPF.Data;
using CarWorkshopWPF.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace CarWorkshopWPF.Views
{
    public partial class AdminWindow : Window
    {
        private readonly User _user;
        private readonly DispatcherTimer _clockTimer;

        public AdminWindow(User user)
        {
            InitializeComponent();
            _user = user;

            labelUser.Text = $"👤 {_user.Login}  |  {_user.Role.ToUpper()}";

            // Zegar
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
            foreach (var btn in new[] { buttonMain, buttonUsers, buttonClients, buttonRepairs })
            {
                btn.Style = btn == active
                    ? (Style)FindResource("NavButtonActive")
                    : (Style)FindResource("NavButton");
            }
        }

        // === Nawigacja ===

        private void buttonMain_Click(object sender, RoutedEventArgs e) => ShowDashboard();
        private void buttonUsers_Click(object sender, RoutedEventArgs e) => ShowUsers();
        private void buttonClients_Click(object sender, RoutedEventArgs e) => ShowClients();
        private void buttonRepairs_Click(object sender, RoutedEventArgs e) => ShowRepairs();

        private void ShowDashboard()
        {
            stackedWidget.SelectedIndex = 0;
            SetActiveButton(buttonMain);
            SetupDashboard();
        }

        private void ShowUsers()
        {
            stackedWidget.SelectedIndex = 1;
            SetActiveButton(buttonUsers);
            LoadWorkers();
        }

        private void ShowClients()
        {
            stackedWidget.SelectedIndex = 2;
            SetActiveButton(buttonClients);
            LoadClients();
        }

        private void ShowRepairs()
        {
            stackedWidget.SelectedIndex = 3;
            SetActiveButton(buttonRepairs);
            LoadRepairs();
        }

        // === Dashboard ===

        private void SetupDashboard()
        {
            var firstName = _user.FirstName ?? _user.Login;
            var lastName = _user.LastName ?? "";
            var phone = _user.Phone ?? "Brak";
            var email = _user.Email ?? "Brak";
            var role = _user.Role.ToUpper();

            WelcomeLabel.Text = $"Witaj, {firstName}! ✋";
            FullNameLabel.Text = $"👤 {firstName} {lastName}";
            RoleLabel.Text = $"🔑 Stanowisko: {role}";
            PhoneLabel.Text = $"📞 Telefon: {phone}";
            EmailLabel.Text = $"✉ Email: {email}";

            try
            {
                using var ctx = new CarWorkshopContext();
                int notStarted = ctx.RepairOrders.Count(r => r.Status == "Nierozpoczęte");
                int inProgress = ctx.RepairOrders.Count(r => r.Status == "W trakcie");
                int waitingParts = ctx.RepairOrders.Count(r => r.Status == "Czeka na części");
                int pickedUp = ctx.RepairOrders.Count(r => r.Status == "Odebrane");

                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                int completedToday = ctx.RepairOrders
                    .Count(r => (r.Status == "Gotowe do odbioru" || r.Status == "Odebrane")
                                && r.CompletedAt >= today && r.CompletedAt < tomorrow);

                notStartedLabel.Text = $"⏳ Czeka na rozpoczęcie: {notStarted}";
                inProgressLabel.Text = $"🔧 W trakcie naprawy: {inProgress}";
                waitingPartsLabel.Text = $"📦 Czeka na części: {waitingParts}";
                completedTodayLabel.Text = $"🏁 Skończone dzisiaj: {completedToday}";
                pickedUpLabel.Text = $"🤝 Odebrane przez klientów: {pickedUp}";

                UpdateFinancialStats(ctx, today, tomorrow);
                DrawCharts(ctx, today, tomorrow);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd ładowania dashboardu:\n{ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateFinancialStats(CarWorkshopContext ctx, DateTime today, DateTime tomorrow)
        {
            var completedOrders = ctx.RepairOrders
                .Include(o => o.RepairTasks)
                .Include(o => o.OrderParts).ThenInclude(op => op.Part)
                .Where(o => o.CompletedAt >= today && o.CompletedAt < tomorrow
                            && (o.Status == "Gotowe do odbioru" || o.Status == "Odebrane"))
                .ToList();

            decimal income = 0;
            decimal partsCost = 0;
            foreach (var o in completedOrders)
            {
                income += o.RepairTasks.Sum(t => t.Price ?? 0);
                partsCost += o.OrderParts.Sum(op => (op.Part?.Price ?? 0) * (op.Quantity ?? 0));
            }
            var net = income - partsCost;

            incomeLabel.Text = $"💰 Dochód z napraw: {income:F2} zł";
            partsCostLabel.Text = $"📦 Wydatki na części: {partsCost:F2} zł";
            netProfitLabel.Text = $"💵 Zysk netto: {net:F2} zł";
        }

        private void DrawCharts(CarWorkshopContext ctx, DateTime today, DateTime tomorrow)
        {
            // Wykres 1 - aktywność dzisiaj
            int arrived = ctx.RepairOrders.Count(o => o.CreatedAt >= today && o.CreatedAt < tomorrow);
            int fixedToday = ctx.RepairOrders
                .Count(o => o.CompletedAt >= today && o.CompletedAt < tomorrow
                            && (o.Status == "Gotowe do odbioru" || o.Status == "Odebrane"));
            int pickedUpToday = ctx.RepairOrders
                .Count(o => o.CompletedAt >= today && o.CompletedAt < tomorrow && o.Status == "Odebrane");

            ChartActivity.Series = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Values = new[] { arrived, fixedToday, pickedUpToday },
                    Fill = new SolidColorPaint(SKColors.DodgerBlue)
                }
            };
            ChartActivity.XAxes = new[]
            {
                new Axis { Labels = new[] { "Przyjęte", "Skończone", "Wydane" }, LabelsRotation = 0 }
            };

            // Wykres 2 - auta na warsztacie (kołowy)
            int s1 = ctx.RepairOrders.Count(o => o.Status == "Nierozpoczęte");
            int s2 = ctx.RepairOrders.Count(o => o.Status == "W trakcie");
            int s3 = ctx.RepairOrders.Count(o => o.Status == "Czeka na części");
            int s4 = ctx.RepairOrders.Count(o => o.Status == "Gotowe do odbioru");

            ChartCarsInWorkshop.Series = new ISeries[]
            {
                new PieSeries<int> { Values = new[] { s1 }, Name = "Nierozpoczęte", Fill = new SolidColorPaint(SKColors.Tomato) },
                new PieSeries<int> { Values = new[] { s2 }, Name = "W trakcie", Fill = new SolidColorPaint(SKColors.Orange) },
                new PieSeries<int> { Values = new[] { s3 }, Name = "Czeka na części", Fill = new SolidColorPaint(SKColors.Gray) },
                new PieSeries<int> { Values = new[] { s4 }, Name = "Gotowe do odbioru", Fill = new SolidColorPaint(SKColors.SeaGreen) }
            };

            // Wykres 3 - finanse dzisiaj
            var completedOrders = ctx.RepairOrders
                .Include(o => o.RepairTasks)
                .Include(o => o.OrderParts).ThenInclude(op => op.Part)
                .Where(o => o.CompletedAt >= today && o.CompletedAt < tomorrow
                            && (o.Status == "Gotowe do odbioru" || o.Status == "Odebrane"))
                .ToList();

            decimal income = 0, partsCost = 0;
            foreach (var o in completedOrders)
            {
                income += o.RepairTasks.Sum(t => t.Price ?? 0);
                partsCost += o.OrderParts.Sum(op => (op.Part?.Price ?? 0) * (op.Quantity ?? 0));
            }
            var net = income - partsCost;

            ChartFinance.Series = new ISeries[]
            {
                new ColumnSeries<decimal>
                {
                    Values = new[] { income, partsCost, net },
                    Fill = new SolidColorPaint(SKColors.MediumSeaGreen)
                }
            };
            ChartFinance.XAxes = new[]
            {
                new Axis { Labels = new[] { "Dochód", "Wydatki", "Zysk" }, LabelsRotation = 0 }
            };
        }

        // === Pracownicy ===

        private void LoadWorkers()
        {
            try
            {
                using var ctx = new CarWorkshopContext();
                tableWorkers.ItemsSource = ctx.Users.OrderBy(u => u.Login).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd ładowania pracowników:\n{ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddWorker_Click(object sender, RoutedEventArgs e)
        {
            var login = inputWorkerLogin.Text.Trim();
            var pwd = inputWorkerPass.Text.Trim();
            var firstName = inputWorkerName.Text.Trim();
            var lastName = inputWorkerLName.Text.Trim();
            var phone = inputWorkerNumber.Text.Trim();
            var email = inputWorkerEmail.Text.Trim();
            var role = (inputWorkerRole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "mechanic";

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(pwd))
            {
                MessageBox.Show("Login i hasło są wymagane!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(phone) && (!phone.All(char.IsDigit) || phone.Length != 9))
            {
                MessageBox.Show("Numer telefonu musi zawierać dokładnie 9 cyfr!", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(email) && !email.Contains("@"))
            {
                MessageBox.Show("Email musi zawierać znak @!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var ctx = new CarWorkshopContext();
                if (ctx.Users.Any(u => u.Login == login))
                {
                    MessageBox.Show("Taki login już istnieje!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newUser = new User
                {
                    Login = login,
                    Password = pwd,
                    Role = role,
                    FirstName = string.IsNullOrEmpty(firstName) ? null : firstName,
                    LastName = string.IsNullOrEmpty(lastName) ? null : lastName,
                    Phone = string.IsNullOrEmpty(phone) ? null : phone,
                    Email = string.IsNullOrEmpty(email) ? null : email
                };
                ctx.Users.Add(newUser);
                ctx.SaveChanges();

                MessageBox.Show($"Zarejestrowano pracownika: {login} ({role})", "Sukces",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                inputWorkerLogin.Clear();
                inputWorkerPass.Clear();
                inputWorkerName.Clear();
                inputWorkerLName.Clear();
                inputWorkerNumber.Clear();
                inputWorkerEmail.Clear();
                LoadWorkers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDeleteWorker_Click(object sender, RoutedEventArgs e)
        {
            if (tableWorkers.SelectedItem is not User selected)
            {
                MessageBox.Show("Wybierz pracownika z listy do usunięcia.", "Uwaga",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (selected.Id == _user.Id)
            {
                MessageBox.Show("Nie możesz usunąć sam siebie!", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Czy na pewno usunąć konto {selected.Login}?", "Usuwanie",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using var ctx = new CarWorkshopContext();
                var u = ctx.Users.Find(selected.Id);
                if (u != null)
                {
                    ctx.Users.Remove(u);
                    ctx.SaveChanges();
                    LoadWorkers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nie udało się usunąć. Użytkownik posiada powiązania.\n{ex.Message}",
                    "Błąd bazy", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === Klienci ===

        private void LoadClients()
        {
            try
            {
                using var ctx = new CarWorkshopContext();
                tableClients.ItemsSource = ctx.Customers.OrderBy(c => c.LastName).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd ładowania klientów:\n{ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRefreshClients_Click(object sender, RoutedEventArgs e) => LoadClients();

        // === Naprawy ===

        private void LoadRepairs()
        {
            try
            {
                using var ctx = new CarWorkshopContext();
                var repairs = ctx.RepairOrders
                    .Include(r => r.Vehicle).ThenInclude(v => v!.Customer)
                    .Include(r => r.Mechanic)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToList()
                    .Select(r => new
                    {
                        r.Id,
                        VehicleStr = r.Vehicle != null ? $"#{r.Vehicle.Id} {r.Vehicle.Make} {r.Vehicle.Model}" : "Brak",
                        ClientStr = r.Vehicle?.Customer != null
                            ? $"{r.Vehicle.Customer.FirstName} {r.Vehicle.Customer.LastName}"
                            : "Brak",
                        MechanicStr = r.Mechanic != null
                            ? $"{r.Mechanic.FirstName} {r.Mechanic.LastName}"
                            : "Brak",
                        r.Status,
                        CreatedStr = r.CreatedAt?.ToString("yyyy-MM-dd HH:mm") ?? ""
                    })
                    .ToList();
                tableRepairs.ItemsSource = repairs;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd ładowania napraw:\n{ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRefreshRepairs_Click(object sender, RoutedEventArgs e) => LoadRepairs();

        // === Inne ===

        private void editProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new UpdateProfileDialog(_user);
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                SetupDashboard();
                LoadWorkers();
            }
        }

        private void buttonLogout_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            Close();
        }
    }
}

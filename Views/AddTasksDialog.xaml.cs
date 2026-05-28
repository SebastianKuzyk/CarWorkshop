using System.Collections.Generic;
using System.Linq;
using System.Windows;
using CarWorkshopWPF.Data;

namespace CarWorkshopWPF.Views
{
    public partial class AddTasksDialog : Window
    {
        public class TaskEntry
        {
            public string Description { get; set; } = "";
            public decimal Price { get; set; }
            public int? ServiceTypeId { get; set; }
        }

        private class ServiceItem
        {
            public string Name { get; set; } = "";
            public int Id { get; set; }
            public decimal Price { get; set; }
        }

        public List<TaskEntry> Tasks { get; } = new();

        public AddTasksDialog()
        {
            InitializeComponent();

            using var ctx = new CarWorkshopContext();
            var services = ctx.ServiceTypes.OrderBy(s => s.Name).ToList();

            ServiceCombo.Items.Add(new ServiceItem { Name = "-- wybierz usługę --", Id = 0, Price = 0 });
            foreach (var s in services)
                ServiceCombo.Items.Add(new ServiceItem { Name = s.Name, Id = s.Id, Price = s.DefaultPrice ?? 0 });
            ServiceCombo.DisplayMemberPath = "Name";
            ServiceCombo.SelectedIndex = 0;
        }

        private void ServiceCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ServiceCombo.SelectedItem is ServiceItem si && si.Id > 0)
                PriceTextBox.Text = si.Price.ToString("F2");
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (ServiceCombo.SelectedItem is not ServiceItem si || si.Id == 0)
            {
                MessageBox.Show("Wybierz typ usługi.", "Uwaga",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!decimal.TryParse(PriceTextBox.Text, out var price) || price < 0)
            {
                MessageBox.Show("Cena musi być liczbą nieujemną.", "Uwaga",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var extra = ExtraInfoTextBox.Text.Trim();
            string desc = string.IsNullOrEmpty(extra) ? si.Name : $"{si.Name} ({extra})";

            Tasks.Add(new TaskEntry { Description = desc, Price = price, ServiceTypeId = si.Id });
            TasksList.Items.Add($"• {desc} - {price:F2} zł");

            ServiceCombo.SelectedIndex = 0;
            ExtraInfoTextBox.Clear();
            PriceTextBox.Text = "100";
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            int idx = TasksList.SelectedIndex;
            if (idx >= 0)
            {
                TasksList.Items.RemoveAt(idx);
                Tasks.RemoveAt(idx);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (Tasks.Count == 0)
            {
                MessageBox.Show("Dodaj przynajmniej jedno zadanie.", "Uwaga",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

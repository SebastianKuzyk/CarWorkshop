using System.Windows;

namespace CarWorkshopWPF.Views
{
    public partial class ChooseStatusDialog : Window
    {
        public string? Selected { get; private set; }

        public ChooseStatusDialog(string[] statuses)
        {
            InitializeComponent();
            foreach (var s in statuses)
                StatusComboBox.Items.Add(s);
            if (StatusComboBox.Items.Count > 0)
                StatusComboBox.SelectedIndex = 0;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Selected = StatusComboBox.SelectedItem?.ToString();
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

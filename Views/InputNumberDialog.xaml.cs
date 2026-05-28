using System.Windows;

namespace CarWorkshopWPF.Views
{
    public partial class InputNumberDialog : Window
    {
        public int Value { get; private set; }

        public InputNumberDialog(string title, string prompt, int defaultValue = 0)
        {
            InitializeComponent();
            Title = title;
            PromptText.Text = prompt;
            ValueTextBox.Text = defaultValue.ToString();
            Loaded += (s, e) => { ValueTextBox.Focus(); ValueTextBox.SelectAll(); };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(ValueTextBox.Text, out int v) || v <= 0)
            {
                MessageBox.Show("Wartość musi być liczbą całkowitą większą od zera.", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Value = v;
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

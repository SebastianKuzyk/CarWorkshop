using System;
using System.Windows;
using CarWorkshopWPF.Data;
using CarWorkshopWPF.Models;

namespace CarWorkshopWPF.Views
{
    public partial class UpdateProfileDialog : Window
    {
        private readonly User _user;

        public UpdateProfileDialog(User user)
        {
            InitializeComponent();
            _user = user;
            LoginTextBox.Text = _user.Login;
            PhoneTextBox.Text = _user.Phone ?? "";
            EmailTextBox.Text = _user.Email ?? "";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var ctx = new CarWorkshopContext();
                var u = ctx.Users.Find(_user.Id);
                if (u == null)
                {
                    MessageBox.Show("Nie znaleziono użytkownika.", "Błąd",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                u.Login = LoginTextBox.Text.Trim();
                u.Phone = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim();
                u.Email = string.IsNullOrWhiteSpace(EmailTextBox.Text) ? null : EmailTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(PasswordTextBox.Text))
                    u.Password = PasswordTextBox.Text;

                ctx.SaveChanges();

                // Aktualizuj kopię w pamięci
                _user.Login = u.Login;
                _user.Phone = u.Phone;
                _user.Email = u.Email;
                if (!string.IsNullOrEmpty(PasswordTextBox.Text))
                    _user.Password = u.Password;

                MessageBox.Show("Profil zaktualizowany.", "Sukces",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisu:\n{ex.Message}", "Błąd",
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

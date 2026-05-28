using System.Windows;
using System.Windows.Media;
using CarWorkshopWPF.Services;

namespace CarWorkshopWPF.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text;
            string password = PasswordBox.Password;

            try
            {
                var user = LoginService.AuthenticateUser(login, password);

                if (user != null)
                {
                    MessageLabel.Foreground = Brushes.Green;
                    MessageLabel.Text = $"Zalogowano jako: {user.Role}";
                    OpenPanelForRole(user);
                }
                else
                {
                    MessageLabel.Foreground = Brushes.Red;
                    MessageLabel.Text = "Błędny login lub hasło";
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Błąd połączenia z bazą:\n{ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenPanelForRole(Models.User user)
        {
            Window? panel = user.Role switch
            {
                "admin" => new AdminWindow(user),
                "mechanic" => new MechanicWindow(user),
                "recepcja" => new RecepcjaWindow(user),
                "magazynier" => new MagazynierWindow(user),
                _ => null
            };

            if (panel != null)
            {
                panel.Show();
                this.Close();
            }
        }
    }
}

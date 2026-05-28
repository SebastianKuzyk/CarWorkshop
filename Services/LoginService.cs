using System.Linq;
using CarWorkshopWPF.Data;
using CarWorkshopWPF.Models;

namespace CarWorkshopWPF.Services
{
    public class LoginService
    {
        public static User? AuthenticateUser(string login, string password)
        {
            using var context = new CarWorkshopContext();
            return context.Users.FirstOrDefault(u => u.Login == login && u.Password == password);
        }
    }
}

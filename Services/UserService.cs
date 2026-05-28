using System.Collections.Generic;
using System.Linq;
using CarWorkshopWPF.Data;
using CarWorkshopWPF.Models;

namespace CarWorkshopWPF.Services
{
    public class UserService
    {
        public static List<User> GetAllUsers()
        {
            using var context = new CarWorkshopContext();
            return context.Users.OrderBy(u => u.Login).ToList();
        }

        public static User? GetUserById(int id)
        {
            using var context = new CarWorkshopContext();
            return context.Users.FirstOrDefault(u => u.Id == id);
        }

        public static void AddUser(User user)
        {
            using var context = new CarWorkshopContext();
            context.Users.Add(user);
            context.SaveChanges();
        }

        public static void DeleteUser(int id)
        {
            using var context = new CarWorkshopContext();
            var user = context.Users.Find(id);
            if (user != null)
            {
                context.Users.Remove(user);
                context.SaveChanges();
            }
        }
    }
}

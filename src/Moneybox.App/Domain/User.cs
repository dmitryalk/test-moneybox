using System;

namespace Moneybox.App
{
    public class User
    {
        public Guid Id { get; init; }

        public string Name { get; private set; }

        public string Email { get; private set; }

        public static User Create(string name, string email)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                Name = name,
                Email = email
            };
        }
    }
}

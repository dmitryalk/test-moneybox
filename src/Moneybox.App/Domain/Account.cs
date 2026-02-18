using System;

namespace Moneybox.App
{
    public class Account
    {
        public const decimal PayInLimit = 4000m;
        public const decimal LowFunds = 500m;

        public Guid Id { get; init; }

        public User User { get; private set; }

        public decimal Balance { get; private set; }

        public decimal Withdrawn { get; private set; }

        public decimal PaidIn { get; private set; }

        private Account() { }

        public static Account Create(User user)
        {
            return new Account
            {
                Id = Guid.NewGuid(),
                User = user,
                Balance = 0m,
                Withdrawn = 0m,
                PaidIn = 0m
            };
        }

        public Account SetBalance(decimal balance, decimal withdrawn = 0, decimal paidIn = 0)
        {
            Balance = balance;
            Withdrawn = withdrawn;
            PaidIn = paidIn;
            return this;
        }

        public Account Deposit(decimal amount)
        {
            if (PaidIn + amount > PayInLimit)
            {
                throw Errors.PayInLimitReached;
            }
            Balance += amount;
            PaidIn += amount;
            return this;
        }

        public Account Withdraw(decimal amount)
        {
            if (Balance < amount)
            {
                throw Errors.InsufficientFunds;
            }
            Balance -= amount;
            Withdrawn -= amount;
            return this;
        }

        public static class Errors
        {
            public static InvalidOperationException InsufficientFunds => new InvalidOperationException("Insufficient funds to make transfer");
            public static InvalidOperationException PayInLimitReached => new InvalidOperationException("Account pay in limit reached");
        }
    }
}

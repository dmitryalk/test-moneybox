using Moneybox.App.DataAccess;
using Moneybox.App.Domain.Services;
using Moneybox.App.Features;
using Moq;

namespace Moneybox.App.Tests;

[TestClass]
public sealed class WithdrawMoneyTests
{
    private Mock<IAccountRepository> _mockAccountRepository;
    private Mock<INotificationService> _mockNotificationService;
    private WithdrawMoney _withdrawMoney;
    private User _user;

    public WithdrawMoneyTests()
    {
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockNotificationService = new Mock<INotificationService>();
        _withdrawMoney = new WithdrawMoney(_mockAccountRepository.Object, _mockNotificationService.Object);
        _user = User.Create("Test User", "test@test.com");
    }

    [TestInitialize]
    public void Setup()
    {
       
    }

    [TestMethod]
    public void Execute_SuccessfulWithdrawal_UpdatesAccount()
    {
        // Arrange
        var amount = 100m;

        var account = Account
            .Create(_user)
            .SetBalance(1000m, -200m, 0m);

        _mockAccountRepository.Setup(x => x.GetAccountById(account.Id)).Returns(account);

        // Act
        _withdrawMoney.Execute(account.Id, amount);

        // Assert
        Assert.AreEqual(900m, account.Balance);
        Assert.AreEqual(-300m, account.Withdrawn);
        _mockAccountRepository.Verify(x => x.Update(account), Times.Once);
    }

    [TestMethod]
    public void Execute_InsufficientFunds_ThrowsException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = 1000.01m;

        var account = Account
            .Create(_user)
            .SetBalance(1000m);

        _mockAccountRepository.Setup(x => x.GetAccountById(accountId)).Returns(account);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _withdrawMoney.Execute(accountId, amount));
        Assert.AreEqual(Account.Errors.InsufficientFunds.Message, exception.Message);
        _mockAccountRepository.Verify(x => x.Update(account), Times.Never);
    }


    [TestMethod]
    public void Execute_BalanceBelow500AfterWithdrawal_NotifiesFundsLow()
    {
        // Arrange
        var amount = 600m;

        var account = Account
            .Create(_user)
            .SetBalance(1000m);

        _mockAccountRepository.Setup(x => x.GetAccountById(account.Id)).Returns(account);

        // Act
        _withdrawMoney.Execute(account.Id, amount);

        // Assert
        Assert.AreEqual(400m, account.Balance);
        _mockNotificationService.Verify(x => x.NotifyFundsLow(_user.Email), Times.Once);
        _mockAccountRepository.Verify(x => x.Update(account), Times.Once);
    }

    [TestMethod]
    public void Execute_BalanceAbove500AfterWithdrawal_DoesNotNotifyFundsLow()
    {
        // Arrange
        var amount = 100m;

        var account = Account
            .Create(_user)
            .SetBalance(1000m);

        _mockAccountRepository.Setup(x => x.GetAccountById(account.Id)).Returns(account);

        // Act
        _withdrawMoney.Execute(account.Id, amount);

        // Assert
        Assert.AreEqual(900m, account.Balance);
        _mockNotificationService.Verify(x => x.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockAccountRepository.Verify(x => x.Update(account), Times.Once);
    }

    [TestMethod]
    public void Execute_BalanceExactly500AfterWithdrawal_DoesNotNotifyFundsLow()
    {
        // Arrange
        var amount = 500m;

        var account = Account
            .Create(_user)
            .SetBalance(1000m);

        _mockAccountRepository.Setup(x => x.GetAccountById(account.Id)).Returns(account);

        // Act
        _withdrawMoney.Execute(account.Id, amount);

        // Assert
        Assert.AreEqual(500m, account.Balance);
        _mockNotificationService.Verify(x => x.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockAccountRepository.Verify(x => x.Update(account), Times.Once);
    }

    [TestMethod]
    public void Execute_BalanceExactly499AfterWithdrawal_NotifiesFundsLow()
    {
        // Arrange
        var amount = 501m;

        var account = Account
            .Create(_user)
            .SetBalance(1000m);

        _mockAccountRepository.Setup(x => x.GetAccountById(account.Id)).Returns(account);

        // Act
        _withdrawMoney.Execute(account.Id, amount);

        // Assert
        Assert.AreEqual(499m, account.Balance);
        _mockNotificationService.Verify(x => x.NotifyFundsLow(_user.Email), Times.Once);
        _mockAccountRepository.Verify(x => x.Update(account), Times.Once);
    }

    [TestMethod]
    public void Execute_WithdrawEntireBalance_UpdatesAccountAndNotifies()
    {
        // Arrange
        var amount = 1000m;

        var account = Account
            .Create(_user)
            .SetBalance(1000m);

        _mockAccountRepository.Setup(x => x.GetAccountById(account.Id)).Returns(account);

        // Act
        _withdrawMoney.Execute(account.Id, amount);

        // Assert
        Assert.AreEqual(0m, account.Balance);
        Assert.AreEqual(-1000m, account.Withdrawn);
        _mockNotificationService.Verify(x => x.NotifyFundsLow(_user.Email), Times.Once);
        _mockAccountRepository.Verify(x => x.Update(account), Times.Once);
    }

    [TestMethod]
    public void Execute_ZeroAmount_UpdatesAccountWithoutNotifications()
    {
        // Arrange
        var amount = 0m;

        var account = Account
            .Create(_user)
            .SetBalance(1000m, -100m, 0m);

        _mockAccountRepository.Setup(x => x.GetAccountById(account.Id)).Returns(account);

        // Act
        _withdrawMoney.Execute(account.Id, amount);

        // Assert
        Assert.AreEqual(1000m, account.Balance);
        Assert.AreEqual(-100m, account.Withdrawn);
        _mockNotificationService.Verify(x => x.NotifyFundsLow(It.IsAny<string>()), Times.Never);
        _mockAccountRepository.Verify(x => x.Update(account), Times.Once);
    }

    [TestMethod]
    public void Execute_AccountAlreadyHasWithdrawals_AccumulatesWithdrawnAmount()
    {
        // Arrange
        var amount = 200m;

        var account = Account
            .Create(_user)
            .SetBalance(1000m, -300m, 0m);
       
        _mockAccountRepository.Setup(x => x.GetAccountById(account.Id)).Returns(account);

        // Act
        _withdrawMoney.Execute(account.Id, amount);

        // Assert
        Assert.AreEqual(800m, account.Balance);
        Assert.AreEqual(-500m, account.Withdrawn);
        _mockAccountRepository.Verify(x => x.Update(account), Times.Once);
    }
}

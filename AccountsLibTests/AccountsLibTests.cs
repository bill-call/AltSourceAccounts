using System.Diagnostics.CodeAnalysis;
using System.Security.Authentication;
using AccountsLib;
using Xunit;

namespace AccountsLibTests
{
  [SuppressMessage("ReSharper", "NotAccessedVariable")]
  [SuppressMessage("ReSharper", "UnusedVariable")]
  public class Accounts
  {
    private const string DefaultAdminUsername = "admin";

    [Fact]
    public void FactDefaultAdminCredentialsPermitLogin()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        Assert.True(session.IsValid);
      }
    }

    [Fact]
    public void FactNonExistantUsernameWithValidPasswordForValidUserPreventsLoginWithAuthenticationException()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      var ex = Assert.Throws<AuthenticationException>(
        () => Logic.Login(repository, "foo")
      );
    }

    [Fact]
    public void FactNullUsernameWithPasswordForValidUserPreventsLoginWithAuthenticationException()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      var ex = Assert.Throws<AuthenticationException>(
        () => Logic.Login(repository, "foo")
      );
    }

    [Fact]
    public void FactDefaultAdminUserHasNoClient()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        Assert.True(session.IsValid);
        Assert.Equal(Repository.THE_NULL_ID, session.User.ClientId);
      }
    }

    [Fact]
    public void FactDefaultAdminUserIsMarkedAsAdmin()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        Assert.True(session.IsValid);
        Assert.True(session.User.IsAdmin);
      }
    }

    [Fact]
    public void FactLoggedOnAdminUserIsMarkedAsAuthenticated()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        Assert.True(session.IsValid);
        Assert.True(session.User.IsAdmin);
      }
    }

    [Fact]
    public void FactAttemptToCreateAccountWithInvalidClientIdFailsWithInvalidRequestException()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        var localSession = session;

        var ex = Assert.Throws<InvalidRequestException>(
          () => Logic.CreateAccount(localSession, "C00001")
        );
      }
    }

    [Fact]
    public void FactAttemptToCreateAccountIfRequestingUserIsAuthenticatedAdminUserSucceeds()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        var account = Logic.CreateAccount(session, "foo", "Foo", "Bar");
      }
    }

    [Fact]
    public void FactUsersCreatedAsASideEffectOfAccountCreationAreNotAdminUsers()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        var newAccount = Logic.CreateAccount(session, "foo", "Foo", "Bar");
      }

      using (var nonAdminSession = Logic.Login(repository, "foo"))
      {
        Assert.False(nonAdminSession.User.IsAdmin);
      }
    }

    [Fact]
    public void FactAttemptToCreateAccountIfRequestingUserIsAuthenticatedNonAdminUserFailsWithAuthorizationException()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        var newAccount = Logic.CreateAccount(session, "foo", "Foo", "Bar");
      }

      using (var nonAdminSession = Logic.Login(repository, "foo"))
      {
        var localNonAdminSession = nonAdminSession;

        var ex = Assert.Throws<AuthorizationException>(
          () => Logic.CreateAccount(localNonAdminSession, "fee", "Fo", "Fum")
        );
      }
    }

    [Fact]
    public void FactCreatingNewAccountAutomaticallyCreatesUserIfOneDoesNotExist1()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        Assert.True(session.IsValid);
        Assert.True(session.User.IsAdmin);
      }
    }

    [Fact]
    public void FactCreatingNewAccountAutomaticallyCreatesUserIfOneDoesNotExist2()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        Assert.True(session.IsValid);
        Assert.True(session.User.IsAdmin);
      }
    }

    [Fact]
    public void FactGettingAnAccountBalanceWithTheAutomaticalyCreatedOwningUserSucceeds()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      Account newAccount;

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        newAccount = Logic.CreateAccount(session, "foo", "Foo", "Bar");
      }

      using (var owningUserSession = Logic.Login(repository, "foo"))
      {
        var newAccountBalance = Logic.GetBalance(owningUserSession, newAccount.AccountNumber);

        Assert.Equal(0.0, newAccountBalance);
      }
    }

    [Fact]
    public void FactGettingAnAccountTransactionHistoryWithTheAutomaticalyCreatedOwningUserSucceeds()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      Account newAccount;

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        newAccount = Logic.CreateAccount(session, "foo", "Foo", "Bar");
      }

      using (var owningUserSession = Logic.Login(repository, "foo"))
      {
        var newAccountTransactionHistory = Logic.GetTransactionHistory(owningUserSession,
                                                                       newAccount.AccountNumber);

        Assert.Empty(newAccountTransactionHistory);
      }
    }

    [Fact]
    public void FactANewAccountHasABalanceOfZero()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        var newAccount = Logic.CreateAccount(session, "foo", "Foo", "Bar");
        var newAccountBalance = Logic.GetBalance(session, newAccount.AccountNumber);

        Assert.Equal(0.0, newAccountBalance);
      }
    }

    [Fact]
    public void FactANewAccountHasNoTransactions()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        var newAccount = Logic.CreateAccount(session, "foo", "Foo", "Bar");

        var newAccountTransactionHistory = Logic.GetTransactionHistory(session,
                                                                       newAccount.AccountNumber);

        Assert.Empty(newAccountTransactionHistory);
      }
    }

    [Fact]
    public void FactGettingAnAccountBalanceWithAnInvalidSessionFailsWithAnInvalidSessionException()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      Account newAccount;
      Session session;

      using (session = Logic.Login(repository, DefaultAdminUsername))
      {
        newAccount = Logic.CreateAccount(session, "foo", "Foo", "Bar");
      }

      Assert.False(session.IsValid);

      var ex = Assert.Throws<InvalidSessionException>(
        () => Logic.GetBalance(session, newAccount.AccountNumber)
      );
    }

    [Fact]
    public void FactGettingAnAccountBalanceWithAnUnauthorizedNonAdminUserFailsWithAnAuthorizationException()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      Account newAccount01;

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        newAccount01 = Logic.CreateAccount(session, "foo", "Foo", "Bar");

        var newAccount02 = Logic.CreateAccount(session, "fee", "Fo", "Fum");
      }

      using (var unauthorizedUserSession = Logic.Login(repository, "fee"))
      {
        var localUnauthorizedUserSession = unauthorizedUserSession;

        var ex = Assert.Throws<AuthorizationException>(
          () => Logic.GetBalance(localUnauthorizedUserSession, newAccount01.AccountNumber)
        );
      }
    }

    [Fact]
    public void FactCreditingAnAccountAsAnAdminCorrectlyUpdatesTheAccountBalance()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        var account = Logic.CreateAccount(session, "foo", "Foo", "Bar");
        var oldBalance = Logic.GetBalance(session, account.AccountNumber);

        Logic.ExecuteTransaction(session,
                                 account.AccountNumber,
                                 ETransactionType.Credit,
                                 100.0);

        var newBalance = Logic.GetBalance(session, account.AccountNumber);
        var variance = (newBalance - oldBalance);

        Assert.Equal(100.0, variance);
      }
    }

    [Fact]
    public void FactCreditingAnAccountAsAnAuthenticatedAndAuthorizedNonAdminUserUpdatesTheAccountBalance()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      Account account;

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        account = Logic.CreateAccount(session, "foo", "Foo", "Bar");
      }

      using (var session = Logic.Login(repository, "foo"))
      {
        var oldBalance = Logic.GetBalance(session, account.AccountNumber);

        Logic.ExecuteTransaction(session,
                                 account.AccountNumber,
                                 ETransactionType.Credit,
                                 100.0);

        var newBalance = Logic.GetBalance(session, account.AccountNumber);
        var variance = (newBalance - oldBalance);

        Assert.Equal(100.0, variance);
      }
    }

    [Fact]
    public void FactCreditingAnAccountAsAnAuthenticatedButUnauthorizedNonAdminUserFailsWithAnAuthorizationException()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      Account newAccount01;

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        newAccount01 = Logic.CreateAccount(session, "foo", "Foo", "Bar");

        var newAccount02 = Logic.CreateAccount(session, "fee", "Fo", "Fum");
        var oldBalance = Logic.GetBalance(session, newAccount01.AccountNumber);
      }

      using (var unauthorizedUserSession = Logic.Login(repository, "fee"))
      {
        var localUnathorizedUserSession = unauthorizedUserSession;

        var ex = Assert.Throws<AuthorizationException>(() => 
          Logic.ExecuteTransaction(localUnathorizedUserSession,
                                   newAccount01.AccountNumber,
                                   ETransactionType.Credit,
                                   100.0)
        );
      }
    }

    [Fact]
    public void FactAttemptingToDebitAnAccountWithAnAmountGreaterThanItsCurrentBalanceThrowsAnOverdraftException()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        var newAccount01 = Logic.CreateAccount(session, "foo", "Foo", "Bar");
        var balance = Logic.GetBalance(session, newAccount01.AccountNumber);

        Assert.Equal(0.0, balance);

        var ex = Assert.Throws<OverdraftException>(() =>
          Logic.ExecuteTransaction(session,
                                   newAccount01.AccountNumber,
                                   ETransactionType.Debit,
                                   100.0)
        );
      }
    }

    [Fact]
    public void FactDirectlyCreatingANewUserWillResultInAUserWithANullClientAssociation()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      User user;

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        user = Logic.CreateUser(session, "foo");
      }

      Assert.Equal(user.ClientId, Repository.THE_NULL_ID);
    }

    [Fact]
    public void FactAttemptingToCreateANewUserWithAUsernameThatIsAlreadyInUseFailsWithAnInvalidRequestException()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        var userFoo1 = Logic.CreateUser(session, "foo");

        var ex = Assert.Throws<InvalidRequestException>(() =>
            Logic.CreateUser(session, "foo")
        );
      }
    }

    [Fact]
    public void FactRequestingTheBalanceOfAnAccountThatDoesNotExistWillFailWithAnInvalidRequestException()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        var ex = Assert.Throws<InvalidRequestException>(() =>
          Logic.GetBalance(session, "badacct")
        );
      }
    }

    [Fact]
    public void FactAttemptingToCreateAnAccountWithANonAdminUserWillFailWithAnAuthorizationException()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      Client fooClient;

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        var nonAdminUserFoo = Logic.CreateUser(session, "foo");

        fooClient = Logic.CreateClient(session, "foo", "Foo", "Bar");
      }

      using (var session = Logic.Login(repository, "foo"))
      {
        var ex = Assert.Throws<AuthorizationException>(() =>
          Logic.CreateAccount(session, fooClient.ClientId)
        );
      }
    }

    [Fact]
    public void FactAttemptingToAutoLinkANewAccountToAUserThatAlreadyExistsWillFailWithAnInvalidRequestException()
    {
      var repository = Repository.Make(DefaultAdminUsername);

      using (var session = Logic.Login(repository, DefaultAdminUsername))
      {
        var userFoo = Logic.CreateUser(session, "foo");

        var ex = Assert.Throws<InvalidRequestException>(() =>
          Logic.CreateAccount(session, "foo", "Foo", "Bar")
        );
      }
    }
  }
}

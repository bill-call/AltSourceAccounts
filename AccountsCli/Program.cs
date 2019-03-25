using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using AccountsCli.Util;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;

namespace AccountsCli
{
  public class Program
  {
    private static string _currentUsername;
    private static string _currentPassword;
    private static string _currentFullName;

    private static Actions _actions;

    //==============================================================================================
  
    /// <summary>
    /// Main program entry point.  No command line options.
    /// </summary>
    /// <returns></returns>
    private static async Task<int> Main()
    {
      var configuration = GetConfiguration();
      var idn = configuration["Idn"];
      var idnAuthority = configuration["IdnAuthority"];
      var accountsApi = configuration["AccountsApi"];

      _actions = new Actions(idnAuthority, accountsApi, idn);

      var doContinue = true;

      while (doContinue)
      {
        Console.Write("$ ");

        var app = ConfigureCommandLineProcessor();
        var cmd = (Console.ReadLine() ?? string.Empty);

        var cmdTokens = cmd.Tokenize()
                           .Select(t => t.Trim())
                           .Where(t => (!String.IsNullOrEmpty(t)))
                           .ToArray();

        try
        {
          doContinue = (app.Execute(cmdTokens) >= 0);
        }
        catch (CommandParsingException ex)
        {
          Console.WriteLine($"{ex.Message}");
        }
        catch (HttpRequestException ex)
        {
          Console.WriteLine($"{ex.Message}");
        }
        catch (Exception ex)
        {
          Console.WriteLine($"{ex.Message}");
        }

        Console.WriteLine();
      }

      await ForceLogout();

      return 0;
    }

    //==============================================================================================

    /// <summary>
    /// Return true if there is currently a logged-in user.
    /// </summary>
    /// <returns></returns>
    private static bool IsLoggedOnUser()
    {
      return (_currentUsername != null);
    }

    //==============================================================================================

    /// <summary>
    /// If a user is logged in, log them out.
    /// </summary>
    /// <returns></returns>
    private static async Task ForceLogout()
    {
      if (IsLoggedOnUser())
      {
        await Logout();
      }
    }

    //==============================================================================================
 
    /// <summary>
    /// Build the configuration object from the appsettings.json file.
    /// </summary>
    /// <returns></returns>
    private static IConfiguration GetConfiguration()
    {
      var projectRoot = Directory.GetCurrentDirectory();
      var configuration = new ConfigurationBuilder().AddJsonFile($"{projectRoot}\\appsettings.json").Build();

      return configuration;
    }

    //==============================================================================================

    private static async Task<(string, string)> UserCredentialsCallback()
    {
      IDictionary<string, string> userInfo;

      _currentUsername = null;
      _currentPassword = null;
      _currentFullName = null;

      Console.WriteLine("You are not logged in.  Please provide your login credentials:");

      (_currentUsername, _currentPassword, userInfo) = await AdHocLogin();

      _currentFullName = (userInfo.TryGetValue("name", out var name) ? name : "nameless user");

      Console.WriteLine($"Logged in as {_currentUsername}.  Welcome, {_currentFullName}!");

      return (_currentUsername, _currentPassword);
    }

    //=================================================================================================

    private static async Task<(string, string, IDictionary<string, string>)> AdHocLogin()
    {
      const int maxRetryLimit = 3;

      var retryCount = 0;

      string username = null;
      string password = null;
      IDictionary<string, string> userInfo = null;

      while (retryCount++ < maxRetryLimit)
      {
        try
        {
          username = Prompt.GetString("Username:");
          password = (SecureStringToString(Prompt.GetPasswordAsSecureString("Password:")) ?? String.Empty);
          userInfo = await _actions.Login(username, password);

          break;
        }
        catch (LoginException ex)
        {
          Console.WriteLine(ex.Message);
        }
      }

      if (retryCount >= maxRetryLimit)
      {
        throw new LoginException("Login failed; maximum number of retries exceeded.");
      }

      return (username, password, userInfo);
    }

    //=================================================================================================

    private static string SecureStringToString(SecureString value)
    {
      var valuePtr = IntPtr.Zero;

      try
      {
        valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);

        return Marshal.PtrToStringUni(valuePtr);
      }
      finally
      {
        Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
      }
    }

    //=================================================================================================

    private static async Task Login(string username, string password)
    {
      await ForceLogout();

      var userInfo = await _actions.Login(username, password);

      _currentUsername = username;
      _currentPassword = password;
      _currentFullName = (userInfo.TryGetValue("name", out var name) ? name : "nameless user");

      Console.WriteLine($"Logged in as {_currentUsername}.  Welcome, {_currentFullName}!");
    }

    //==============================================================================================

    private static async Task Logout()
    {
      // ReSharper disable once UnusedVariable
      var isSuccess = await _actions.Logout();

      try
      {
        Console.WriteLine($"{_currentUsername} logged out.  Goodbye, {_currentFullName}.");
      }
      finally
      {
        _currentUsername = null;
        _currentPassword = null;
        _currentFullName = null;
      }
    }

    //==============================================================================================

    private static async Task UserCreate(string username,
                                         string password,
                                         string firstName,
                                         string lastName,
                                         bool doCreateClient)
    {
      await _actions.CreateUser(username, 
                               password, 
                               firstName,
                               lastName,
                               false,
                               UserCredentialsCallback);

      Console.WriteLine($"New user {username} created.");

      if (doCreateClient)
      {
        var newClientId = await _actions.CreateClient(username,
                                                     firstName,
                                                     lastName, 
                                                     UserCredentialsCallback);

        Console.WriteLine($"New client {newClientId} created and linked to user {username}.");
      }
    }

    //==============================================================================================

    private static async Task AdminUserCreate(string username,
                                              string password,
                                              string firstName,
                                              string lastName)
    {
      await _actions.CreateUser(username,
                               password,
                               firstName,
                               lastName,
                               true,
                               UserCredentialsCallback);

      Console.WriteLine($"New admin user {username} created.");
    }

    //==============================================================================================

    private static async Task AccountCreate(string username)
    {
      var accountNo = await _actions.CreateAccount(username, UserCredentialsCallback);

      Console.WriteLine($"New account {accountNo} created. Account owner is {username}.");
    }

    //==============================================================================================

    private static async Task AccountCredit(string accountNumber, double amount, string memo)
    {
      var balance = await _actions.CreditAccount(accountNumber, amount, memo, UserCredentialsCallback);

      Console.WriteLine($"Account {accountNumber} credited with ${amount}.  New Balance is {balance:C2}.");
    }

    //==============================================================================================

    private static async Task AccountDebit(string accountNumber, double amount, string memo)
    {
      var balance = await _actions.DebitAccount( accountNumber, amount, memo, UserCredentialsCallback);

      Console.WriteLine($"Account {accountNumber} debited by ${amount}.  New Balance is {balance:C2}.");
    }

    //==============================================================================================

    private static async Task AccountGetBalance(string accountNumber)
    {
      var balance = await _actions.GetAccountBalance(accountNumber, UserCredentialsCallback);

      Console.WriteLine($"Balance for account {accountNumber} is {balance:C2}.");
    }

    //==============================================================================================

    private static async Task AccountGetHistory(string accountNumber)
    {
      var history = await _actions.GetAccountHistory(accountNumber, UserCredentialsCallback);

      // TODO: return transactions, not double; iterate and emit.

      Console.WriteLine($"Lifetime transaction history for account {accountNumber}:");
      Console.WriteLine();
      Console.Write(history);
    }

    //==============================================================================================

    private static CommandLineApplication ConfigureCommandLineProcessor()
    {
      var app = BuildApplicationRoot();

      app.AddSubcommand(BuildByeCommand());
      app.AddSubcommand(BuildCreateCommand());
      app.AddSubcommand(BuildAccountCommand());
      app.AddSubcommand(BuildLoginCommand());
      app.AddSubcommand(BuildLogoutCommand());

      return app;
    }

    //==============================================================================================

    private static CommandLineApplication BuildApplicationRoot()
    {
      var app = new CommandLineApplication
      {
        Name = "AccountsCli",

        ExtendedHelpText = string.Join(
          ' ',
          "\nThis application was written as a component of the AltSourceAccounts application.",
          "It is part of the job application interview for Bill Call."
        )
      };

      app.HelpOption("-?|-h|--help");

      return app;
    }

    //==============================================================================================

    private static CommandLineApplication BuildByeCommand()
    {
      var cmdBye = new CommandLineApplication().Command("bye", bye =>
      {
        bye.Description = "Exit the command loop.";
        bye.HelpOption("-?|-h|--help");

        bye.OnExecute(async () =>
        {
          await ForceLogout();

          return (-1);
        });
      });

      return cmdBye;
    }

    //==============================================================================================

    private static CommandLineApplication BuildCreateCommand()
    {
      var cmdCreate = new CommandLineApplication().Command("create", create =>
      {
        create.Description = "Create a user, admin user, or account.  Enter \"create -?\" for details.";
        create.HelpOption("-?|-h|--help");
      });

      //--------------------------------------------------------------------------------------------

      cmdCreate.AddSubcommand(new CommandLineApplication().Command("user", user =>
      {
        user.Description = "Create a user.";
        user.HelpOption("-?|-h|--help");

        var argUsername = user.Argument<string>("username", "Username").IsRequired();
        var argPassword = user.Argument<string>("password", "Password").IsRequired();
        var argFirstName = user.Argument<string>("firstname", "First Name").IsRequired();
        var argLastName = user.Argument<string>("lastname", "Last Name").IsRequired();

        var optionDoNotCreateClient = user.Option<bool>("-nc|--noclient",
                                                        "Do not automatically create a Client and bind it to the new User.",
                                                        CommandOptionType.NoValue);

        var doCreateClient = (!optionDoNotCreateClient.ParsedValue);

        user.OnExecute(async () =>
        {
          Console.WriteLine("create user");

          await UserCreate(argUsername.Value,
                           argPassword.Value,
                           argFirstName.Value,
                           argLastName.Value,
                           doCreateClient);
        });
      }));

      //--------------------------------------------------------------------------------------------

      cmdCreate.AddSubcommand(new CommandLineApplication().Command("admin", admin =>
      {
        admin.Description = "Create an admin user.";
        admin.HelpOption("-?|-h|--help");

        var argUsername = admin.Argument<string>("username", "Username").IsRequired();
        var argPassword = admin.Argument<string>("password", "Password").IsRequired();
        var argFirstName = admin.Argument<string>("firstname", "First Name").IsRequired();
        var argLastName = admin.Argument<string>("lastname", "Last Name").IsRequired();

        admin.OnExecute(async () =>
        {
          Console.WriteLine("create admin");

          await AdminUserCreate(argUsername.Value, argPassword.Value, argFirstName.Value, argLastName.Value);
        });
      }));

      //--------------------------------------------------------------------------------------------

      cmdCreate.AddSubcommand(new CommandLineApplication().Command("account", account =>
      {
        account.Description = "Create an account.";
        account.HelpOption("-?|-h|--help");

        var argUsername = account.Argument<string>("client", "Client ID, in the form Cnnnnn (a capital 'C', followed by five digits); e.g.: C12345").IsRequired();

        account.OnExecute(async () =>
        {
          Console.WriteLine("create account");

          await AccountCreate(argUsername.Value);
        });
      }));

      return cmdCreate;
    }

    //==============================================================================================

    private static CommandLineApplication BuildAccountCommand()
    {
      const string accountNumberArgName = "account";
      const string accountNumberArgDesc = "Six-Character Account Number, in the form Annnnn (a capital 'A', followed by five digits); e.g.: 'A12345'";
      const string amountArgName = "amount";
      const string memoOptionTemplate = "--memo|-m";
      const string memoOptionDesc = "An optional notation of up to 64 characters, describing this transaction.";

      var cmdAccount = new CommandLineApplication().Command("account", account =>
      {
        account.Description = "Credit or debit an account; get an account balance or history.  Enter \"account -?\" for details.";
        account.HelpOption("-?|-h|--help");
      });

      //--------------------------------------------------------------------------------------------

      cmdAccount.AddSubcommand(new CommandLineApplication().Command("credit", credit =>
      {
        credit.Description = "Credit an account.";
        credit.HelpOption("-?|-h|--help");

        var argAccountNumber = credit.Argument<string>(accountNumberArgName, accountNumberArgDesc).IsRequired();
        var argAmount = credit.Argument<double>(amountArgName, "Amount of credit").IsRequired();

        var optionMemo = credit.Option<string>(memoOptionTemplate,
                                               memoOptionDesc,
                                               CommandOptionType.SingleValue, 
                                               false);

        credit.OnExecute(async () =>
        {
          Console.WriteLine($"credit account {argAccountNumber.Value} with ${argAmount.Value}");

          var amount = double.Parse(argAmount.Value);

          await AccountCredit(argAccountNumber.Value, amount, optionMemo.Value());
        });
      }));

      //--------------------------------------------------------------------------------------------

      cmdAccount.AddSubcommand(new CommandLineApplication().Command("debit", debit =>
      {
        debit.Description = "Debit an account.";
        debit.HelpOption("-?|-h|--help");

        var argAccountNumber = debit.Argument<string>(accountNumberArgName, accountNumberArgDesc).IsRequired();
        var argAmount = debit.Argument<double>(amountArgName, "Amount of credit").IsRequired();

        var optionMemo = debit.Option<string>(memoOptionTemplate,
                                              memoOptionDesc,
                                              CommandOptionType.SingleValue,
                                              false);

        debit.OnExecute(async () =>
        {
          Console.WriteLine($"debit account {argAccountNumber.Value} by ${argAmount.Value}");

          var amount = double.Parse(argAmount.Value);

          await AccountDebit(argAccountNumber.Value, amount, optionMemo.Value());
        });
      }));

      //--------------------------------------------------------------------------------------------

      cmdAccount.AddSubcommand(new CommandLineApplication().Command("balance", balance =>
      {
        balance.Description = "Get an account balance.";
        balance.HelpOption("-?|-h|--help");

        var argAccountNumber = balance.Argument<string>(accountNumberArgName, accountNumberArgDesc).IsRequired();

        balance.OnExecute(async () =>
        {
          Console.WriteLine($"account balance for account {argAccountNumber.Value}");

          await AccountGetBalance(argAccountNumber.Value);
        });
      }));

      //--------------------------------------------------------------------------------------------

      cmdAccount.AddSubcommand(new CommandLineApplication().Command("history", history =>
      {
        history.Description = "Credit an account transaction history.";
        history.HelpOption("-?|-h|--help");

        var argAccountNumber = history.Argument<string>(accountNumberArgName, accountNumberArgDesc).IsRequired();

        history.OnExecute(async () =>
        {
          Console.WriteLine($"account history for account {argAccountNumber.Value}");

          await AccountGetHistory(argAccountNumber.Value);
        });
      }));

      return cmdAccount;
    }

    //==============================================================================================

    private static CommandLineApplication BuildLoginCommand()
    {
      var cmdLogin = new CommandLineApplication().Command("login", login =>
      {
        login.Description = "Log in with username/password.  Enter \"login -?\" for details.";
        login.HelpOption("-?|-h|--help");

        var argUsername = login.Argument<string>("username", "Username").IsRequired();
        var argPassword = login.Argument<string>("password", "password").IsRequired();

        login.OnExecute(async () =>
        {
          await Login(argUsername.Value, argPassword.Value);
        });
      });

      return cmdLogin;
    }

    //==============================================================================================

    private static CommandLineApplication BuildLogoutCommand()
    {
      var cmdLogout = new CommandLineApplication().Command("logout", logout =>
      {
        logout.Description = "Log out.";
        logout.HelpOption("-?|-h|--help");

        logout.OnExecute(async () =>
        {
          Console.WriteLine("Logout");

          await Logout();
        });
      });

      return cmdLogout;
    }
  }
}

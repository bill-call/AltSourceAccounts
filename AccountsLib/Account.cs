using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using Oid = System.UInt32;

// TODO: Unused. Remove?
namespace AccountsLib
{
  public enum ETransactionStatus
  {
    Invalid,
    Pending,
    Successful,
    Failed
  }

  //==============================================================================================

  /// <summary>
  /// Enumerates the available permissions that may be represented by an <see cref="Authorization"/>.
  /// Defined as bitwise flags for compact grouping.
  /// </summary>
  [Flags]
  public enum EAuthorizations
  {
    Invalid = 0x00,
    Create  = 0x01,
    Review  = 0x02,
    Update  = 0x04,
    Delete  = 0x08,
    Credit  = 0x10,
    Debit   = 0x20
  }

  //==============================================================================================

  // TODO: Unused. Remove?
  public enum ETransactionFailureReason
  {
    Invalid,
    Overdraft,
    AuthenticationFailure,
    AuthorizationFailure
  }

  //==============================================================================================

  /// <summary>
  /// Enumerates the set of transactions that may be applied to an <see cref="Account"/>.
  /// </summary>
  public enum ETransactionType
  {
    Invalid,
    Credit,
    Debit
  }

  //==============================================================================================

  /// <summary>
  /// Base class for all persistent objects in the system.  Gives each derived class
  /// an OID (Object Identifier), which is guaranteed to be globally unique with a
  /// given <see cref="DbContext"/>.
  /// </summary>
  public class Entity
  {
    public Oid Id { get; protected set; }

    public Entity(Oid id)
    {
      Id = id;
    }

    public Entity(Entity entity)
    {
      Id = entity.Id;
    }
  }

  //==============================================================================================

  /// <summary>
  /// User != Human.  A User is just an entity with certain rights in the system.
  /// </summary>
  public class User : Entity
  {
    public string Username { get; protected set; }
    public Oid ClientId { get; internal set; }
    public bool IsAdmin { get; internal set; }

    public User(Oid id, string username, bool isAdmin = false) : base(id)
    {
      Username = username;
      ClientId = Repository.THE_NULL_ID;
      IsAdmin = isAdmin;
    }

    public User(Oid id, string username, Oid clientId) : base(id)
    {
      Username = username;
      ClientId = clientId;
      IsAdmin = false;
    }

    public User(User user) : base(user)
    {
      Username = user.Username;
      ClientId = user.ClientId;
      IsAdmin = user.IsAdmin;
    }
  }

  //==============================================================================================

  /// <summary>
  /// Clients are distinct from Users. Some Users are linked to Clients.  Clients own Accounts, not Users.
  /// An authenticated User may have specific Authorizations to the Accounts of specific Clients (among other Entities).
  /// </summary>
  public class Client : Entity
  {
    public string ClientId { get; protected set; }
    public string FirstName { get; protected set; }
    public string LastName { get; protected set; }

    public Client(Oid id, string clientId, string firstName, string lastName) : base(id)
    {
      ClientId = clientId;
      FirstName = firstName;
      LastName = lastName;
    }

    public Client(Client client) : base(client)
    {
      ClientId = client.ClientId;
      FirstName = client.FirstName;
      LastName = client.LastName;
    } 
  }

  //==============================================================================================

  /// <summary>
  /// Base class for transaction history. Critically, contains no reference to an Account.
  /// </summary>
  public class AnonymousTransaction : Entity
  {
    public ETransactionType TransactionType { get; protected set; }
    public double Amount { get; protected set; }
    public DateTime Timestamp { get; protected set; }
    public string Memo { get; protected set; }

    public AnonymousTransaction(Oid id, DateTime timestamp, ETransactionType transactionType, double amount, string memo) : base(id)
    {
      TransactionType = transactionType;
      Amount = amount;
      Timestamp = timestamp;
      Memo = memo;
    }

    public AnonymousTransaction(AnonymousTransaction transaction)
           : this(transaction.Id, 
                  transaction.Timestamp,
                  transaction.TransactionType,
                  transaction.Amount,
                  transaction.Memo)
    {
    }
  }

  //==============================================================================================

  /// <summary>
  /// Same as <see cref="AnonymousTransaction"/>, but is linked to an Account.
  /// </summary>
  public class Transaction : AnonymousTransaction
  {
    public Oid AccountId { get; protected set; }

    public Transaction(Oid id, DateTime timestamp, Oid accountId, ETransactionType transactionType, double amount, string memo) 
      : base(id, timestamp, transactionType, amount, memo)
    {
      AccountId = accountId;
    }

    public Transaction(Transaction transaction)
           : this(transaction.Id,
                  transaction.Timestamp,
                  transaction.AccountId,
                  transaction.TransactionType,
                  transaction.Amount,
                  transaction.Memo)
    {
    }
  }

  //==============================================================================================

  public class AnonymousTransactionHistory : List<AnonymousTransaction>
  {
  }

  //==============================================================================================

  /// <summary>
  /// Account records contain the information that identifies the actual owner of an account. This
  /// is distinct from a User, as any user with the right grants could access an account; that does
  /// not make the User in question the owner of the Account.
  /// </summary>
  ///
  /// <example>
  /// Example: Bob Smith is the legal owner of account A00001. This is reflected in the Account record.
  ///          Bob Smith is also a User (bsmith) in the system. That user has certain rights to
  ///          Account A00001.  Bob's wife, Alice, is also a User (asmith). Account A0001 is a joint
  ///          account, so asmith has the same rights to A00002 as bsmith, but this does not make
  ///          Alice Smith the legal owner of the Account.
  /// </example>
  public class Account : Entity
  {
    public DateTime CreatedOn { get; protected set; }
    public Oid CreatedBy { get; protected set; }
    public string AccountNumber { get; protected set; }
    public Oid ClientId { get; protected set; }

    public Account(Oid id, string accountNumber, Oid clientId, Oid creatorId) : base(id)
    {
      CreatedOn = DateTime.UtcNow;
      CreatedBy = creatorId;
      AccountNumber = accountNumber;
      ClientId = clientId;
    }

    public Account(Account account) : base(account)
    {
      CreatedOn = account.CreatedOn;
      CreatedBy = account.CreatedBy;
      AccountNumber = account.AccountNumber;
      ClientId = account.ClientId;
    }
  }

  //==============================================================================================

  /// <summary>
  /// Base class of <see cref="AccountSnapshot"/>.  Critically, contains no reference to an Account.
  /// </summary>
  public class AnonymousAccountSnapshot
  {
    [Required] public DateTime Timestamp { get; protected set; }
    [Required] public IEnumerable<AnonymousTransaction> History { get; protected set; }

    public double Balance;

    protected AnonymousAccountSnapshot() {}

    //----------------------------------------------------------------------------------------------

    protected static AnonymousAccountSnapshot Make(IEnumerable<AnonymousTransaction> transactions)
    {
      return Make(new AnonymousAccountSnapshot(), transactions);
    }

    //----------------------------------------------------------------------------------------------

    public static AnonymousAccountSnapshot Make(AnonymousAccountSnapshot snapshot,
                                                IEnumerable<AnonymousTransaction> transactions)
    {
      // Make certain it's really an AnonymousTransaction, and not some derived class.
      snapshot.History = transactions.Select(t => new AnonymousTransaction(t))  
                                     .OrderBy(a => a.Timestamp)
                                     .ToList();

      snapshot.Balance = snapshot.History.Where(t => (   (t.TransactionType != ETransactionType.Credit)
                                                      || (t.TransactionType != ETransactionType.Debit)))
                                         .Sum(t => (t.TransactionType == ETransactionType.Credit) ? t.Amount : (-t.Amount));

      snapshot.Timestamp = ((snapshot.History.Any()) ? snapshot.History.Last().Timestamp : DateTime.UtcNow);

      return snapshot;
    }
  }

  //==============================================================================================

  /// <summary>
  /// Same as <see cref="AnonymousAccountSnapshot"/>, but contains a reference to the Account whose
  /// state it represents.
  /// </summary>
  public class AccountSnapshot : AnonymousAccountSnapshot
  {
    public Oid AccountId { get; private set; }

    protected AccountSnapshot(Oid accountId)
    {
      AccountId = accountId;
    }

    public static AccountSnapshot Make(IEnumerable<Transaction> transactions, Account account)
    {
      var snapshot = new AccountSnapshot(account.Id);

      AnonymousAccountSnapshot.Make(snapshot, transactions);

      return snapshot;
    }
  }

  //==============================================================================================

  /// <summary>
  /// A simple record indicating what rights (authorizations) a given user has to a given
  /// entity in the system. Currently, only used to define a User's rights to an Account.
  /// </summary>
  internal class Authorization
  {
    public Oid UserId { get; protected set; }
    public Oid EntityId { get; protected set; }
    public EAuthorizations Authorizations {get; protected set; }

    public Authorization(Oid userId, Oid entityId, EAuthorizations authorizations)
    {
      UserId = userId;
      EntityId = entityId;
      Authorizations = authorizations;
    }
  }

  //==============================================================================================

  /// <summary>
  /// An object that contains the context for the current API call.
  /// </summary>
  public sealed class Session : IDisposable
  {
    private Session(Repository repository, User user)
    {
      User = new User(user);
      Repository = repository;
    }

    internal static Session Make(Repository repository, User user)
    {
      if (repository == null)
      {
        throw new InvalidRequestException("Bad Request");
      }

      if (user == null)
      {
        throw new InvalidRequestException("Bad Request");
      }

      var repositoryUser = repository.GetUserByUsername(user.Username);

      if (repositoryUser.Id != user.Id)
      {
        throw new InvalidRequestException("Bad Request");
      }

      return new Session(repository, user);
    }

    public User User { get; internal set; }
    public Repository Repository { get; internal set; }

    internal bool IsValidated { get; private set; }

    internal Session Validate()
    {
      IsValidated = true;

      return this;
    }

    internal Session Invalidate()
    {
      IsValidated = false;

      return this;
    }

    public bool IsValid
    {
      get
      {
        var isValid = (   IsValidated
                       && (User != null) 
                       && (Repository != null)
                       && Repository.AssertIsUserKnown(User.Username));

        return isValid;
      }
    }

    public void Dispose()
    {
      Logic.Logout(this);
    }
  }

  //==============================================================================================

  public class AccountsLibException : Exception
  {
    public AccountsLibException(string message) : base(message)
    {
    }
  }
  //==============================================================================================

  public class TransactionException : AccountsLibException
  {
    public TransactionException(string message) : base(message)
    {
    }
  }

  //==============================================================================================

  public class OverdraftException : TransactionException
  {
    public OverdraftException(string accountNumber) 
      : base($"Debit against account '{accountNumber}' failed due to insufficient funds")
    {
    }
  }

  //==============================================================================================

  public class AuthorizationException : TransactionException
  {
    public AuthorizationException(string message) : base(message)
    {
    }
  }

  public class InvalidRequestException : AccountsLibException  {
    public InvalidRequestException(string message) : base(message)
    {
    }
  }

  public class InvalidSessionException : AccountsLibException
  {
    public InvalidSessionException(string message) : base(message)
    {
    }
  }

  //==============================================================================================
  //==============================================================================================

  /// <summary>
  /// This is the actual "database".  All of the "tables" and "indices" are defined here.
  /// </summary>
  internal class DbContext
  {
    public Dictionary<Oid, Account> Accounts { get; private set; }
    public Dictionary<Oid, Transaction> Transactions { get; private set; }
    public Dictionary<Oid, User> Users { get; private set; }
    public Dictionary<Oid, Client> Clients { get; private set; }
    public Dictionary<Oid, Authorization> Authorizations { get; private set; }

    public Dictionary<string, Oid> IndexUserByUsername { get; private set; }
    public Dictionary<Oid, Oid> IndexUserByClientOid { get; private set; }
    public Dictionary<string, Oid> IndexAccountByAccountNumber { get; private set; }
    public Dictionary<string, Oid> IndexClientByClientId { get; private set; }

    public DbContext()
    {
      Accounts = new Dictionary<Oid, Account>();
      Transactions = new Dictionary<Oid, Transaction>();
      Users = new Dictionary<Oid, User>();
      Clients = new Dictionary<Oid, Client>();
      Authorizations = new Dictionary<uint, Authorization>();

      IndexUserByUsername= new Dictionary<string, Oid>();
      IndexUserByClientOid = new Dictionary<Oid, Oid>();
      IndexAccountByAccountNumber = new Dictionary<string, Oid>();
      IndexClientByClientId = new Dictionary<string, Oid>();
    }
  }

  //==============================================================================================
  //==============================================================================================

  /// <summary>
  /// Wraps a <see cref="DbContext"/> and defines the legal operations upon it. Contains no
  /// security or business logic; just the logic for accessing and manipulating the individual
  /// tables/indices of a DbContext.
  /// </summary>
  public class Repository
  {
    //----------------------------------------------------------------------------------------------
    //----------------------------------------------------------------------------------------------

    public sealed class ReadLock : IDisposable
    {
      private ReaderWriterLockSlim Padlock { get; }

      public ReadLock(ReaderWriterLockSlim padlock)
      {
        Padlock = padlock;
        
        Padlock.EnterReadLock();
      }

      public void Dispose()
      {
        Padlock.ExitReadLock();
      }
    }

    //----------------------------------------------------------------------------------------------
    //----------------------------------------------------------------------------------------------

    public sealed class WriteLock : IDisposable
    {
      private ReaderWriterLockSlim Padlock { get; }

      public WriteLock(ReaderWriterLockSlim padlock)
      {
        Padlock = padlock;

        Padlock.EnterWriteLock();
      }

      public void Dispose()
      {
        Padlock.ExitWriteLock();
      }
    }

    //----------------------------------------------------------------------------------------------

    public const uint THE_NULL_ID = 0;

    public Oid CurrentOid;
    public uint CurrentAccountNumber;

    internal DbContext Context { get; private set; }


    //----------------------------------------------------------------------------------------------
    // The lock object used to manage concurrent access to the parent repository.
    // Uses the "Supports Recursion" Recursion Policy.  This slower, but safer.
    //----------------------------------------------------------------------------------------------

    protected ReaderWriterLockSlim Lock { get; }

    //----------------------------------------------------------------------------------------------

    public ReadLock Read()
    {
      return new ReadLock(Lock);
    }
    //----------------------------------------------------------------------------------------------

    public WriteLock Write()
    {
      return new WriteLock(Lock);
    }

    //----------------------------------------------------------------------------------------------

    private Repository()
    {
      Context = new DbContext();
      Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
    }

    //----------------------------------------------------------------------------------------------

    public static Repository Make()
    {
      var repository = new Repository();

      repository.AddUser("alice", true);
      repository.AddUser("bob", true);

      return repository;
    }

    //----------------------------------------------------------------------------------------------

    public static Repository Make(string testUsername)
    {
      var repository = new Repository();

      repository.AddUser(testUsername, true);

      return repository;
    }

    //----------------------------------------------------------------------------------------------

    protected Oid GetNextOid()
    {
      return ++CurrentOid;
    }

    protected string GetNextAccountNumber()
    {
      return $"A{++CurrentAccountNumber:D5}";
    }
    protected string GetNextClientId ()
    {
      return $"C{++CurrentAccountNumber:D5}";
    }

    //----------------------------------------------------------------------------------------------

    public bool AssertIsUserKnown(string username)
    {
      var user = GetUserByUsername(username);

      if (user == null)
      {
        throw new AuthorizationException($"User '{username}' is not known");
      }

      return true;
    }

    //----------------------------------------------------------------------------------------------

    public bool AssertIsUserAuthorized(string username, Oid entityId, EAuthorizations authorizations)
    {
      var user = GetUserByUsername(username);

      if (!(user.IsAdmin || IsAuthorized(user.Id, entityId, authorizations)))
      {
        throw new AuthorizationException($"User '{user.Username}' is not authorized for this operation.");
      }

      return true;
    }

    //----------------------------------------------------------------------------------------------

    public bool AssertIsAdminUser(string username)
    {
      var user = GetUserByUsername(username);

      if (!user.IsAdmin)
      {
        throw new AuthorizationException($"User '{user.Username}' is not authorized for administrative operations");
      }

      return true;
    }

    //----------------------------------------------------------------------------------------------

    internal User AddUser(string username, bool isAdmin = false)
    {
      var user = new User(GetNextOid(), username, isAdmin);

      Context.Users.Add(user.Id, user);
      Context.IndexUserByUsername.Add(user.Username, user.Id);

      return user;
    }

    internal User AddUser(string username, Oid clientId)
    {
      var user = new User(GetNextOid(), username, clientId);

      Context.Users.Add(user.Id, user);
      Context.IndexUserByUsername.Add(user.Username, user.Id);

      if (clientId != THE_NULL_ID)
      {
        Context.IndexUserByClientOid.Add(clientId, user.Id);
      }

      return user;
    }

    internal User GetUserByOid(Oid oid)
    {
      Context.Users.TryGetValue(oid, out var user);

      return user;
    }

    internal User GetUserByUsername(string username)
    {
      User user = null;

      if (Context.IndexUserByUsername.TryGetValue(username, out var userOid))
      {
        Context.Users.TryGetValue(userOid, out user);
      }

      return user;
    }

    internal User GetUserByClientOid(Oid clientId)
    {
      User user = null;

      if (Context.IndexUserByClientOid.TryGetValue(clientId, out var userOid))
      {
        Context.Users.TryGetValue(userOid, out user);
      }

      return user;
    }

    internal User SetUserClientOid(Oid userId, Oid clientId)
    {
      var user = GetUserByOid(userId);

      if (user != null)
      {
        user.ClientId = clientId;
        Context.IndexUserByClientOid.Add(clientId, user.Id);
      }

      return user;
    }

    //----------------------------------------------------------------------------------------------

    internal Client AddClient(Oid userId, string firstName, string lastName)
    {
      var user = GetUserByOid(userId);
      var client = new Client(GetNextOid(), GetNextClientId(), firstName, lastName);

      Context.Clients.Add(client.Id, client);
      Context.IndexClientByClientId.Add(client.ClientId, client.Id);

      user.ClientId = client.Id;

      return client;
    }

    internal Client GetClientByOid(Oid clientOid)
    {
      Context.Clients.TryGetValue(clientOid, out var client);

      return client;
    }

    internal Client GetClientByClientId(string clientId)
    {
      Client client = null;

      if (Context.IndexClientByClientId.TryGetValue(clientId, out var clientOid))
      {
        client = GetClientByOid(clientOid);
      }

      return client;
    }

    //----------------------------------------------------------------------------------------------

    internal Account AddAccount(Oid client, Oid creatorId)
    {
      var account = new Account(GetNextOid(), GetNextAccountNumber(), client, creatorId);

      Context.Accounts.Add(account.Id, account);
      Context.IndexAccountByAccountNumber.Add(account.AccountNumber, account.Id);

      return account;
    }

    internal Account GetAccountByOid(Oid accountOid)
    {
      Context.Accounts.TryGetValue(accountOid, out var account);

      return account;
    }

    internal Account GetAccountByAccountNumber(string accountNumber)
    {
      Account account = null;

      if (Context.IndexAccountByAccountNumber.TryGetValue(accountNumber, out var accountOid))
      {
        account = GetAccountByOid(accountOid);
      }

      return account;
    }

    internal Dictionary<string, Account> GetAccountsByClientOid(Oid clientOid)
    {
      var accounts = Context.Accounts.Values.Where(a => (a.ClientId == clientOid))
                                            .ToDictionary(a => a.AccountNumber, a => a);

      return accounts;
    }

    //----------------------------------------------------------------------------------------------

    internal Transaction AddTransaction(Oid accountId, ETransactionType transactionType, double amount, string memo = null)
    {
      var transaction = new Transaction(GetNextOid(), DateTime.UtcNow, accountId, transactionType, amount, memo);

      Context.Transactions.Add(transaction.Id, transaction);

      return transaction;
    }

    internal Transaction[] GetTransactionSnapshot(Oid accountId)
    {
      var snapshot = Context.Transactions.Values.Where(t => (t.AccountId == accountId))
                                                .Select(t => new Transaction(t))  // Make a copy!!!
                                                .OrderBy(a => a.Timestamp)
                                                .ToArray();

      return snapshot;
    }
    
    //----------------------------------------------------------------------------------------------

    internal Authorization AddAuthorization(Oid userId, Oid entityId, EAuthorizations authorizations)
    {
      var authorization = new Authorization(userId, entityId, authorizations);

      Context.Authorizations.Add(authorization.UserId, authorization);

      return authorization;
    }

    internal bool IsAuthorized(Oid userId, Oid entityId, EAuthorizations authorizations)
    {
      var isAuthorized = Context.Authorizations.Values.Any(a => (   (a.UserId == userId)
                                                                 && (a.EntityId == entityId)
                                                                 && ((a.Authorizations & authorizations) == authorizations)));

      return isAuthorized;
    }
  }

  //================================================================================================
  //================================================================================================

  /// <summary>
  /// Uses a <see cref="Session"/> to manipulate a <see cref="Repository"/> wrapping a <see cref="DbContext"/>.
  /// All business and security logic is found here.
  /// </summary>
  public static class Logic
  {
    private const string TheInvalidStringValue = "<invalid>";


    //==============================================================================================

    public static bool AssertIsValidSession(Session session)
    {
      if (session == null)
      {
        throw new InvalidSessionException("Invalid session");
      }

      if (!(session.IsValid))
      {
        throw new InvalidSessionException("Invalid session");
      }

      return true;
    }
    
    //==============================================================================================

    private static readonly Dictionary<ETransactionType, string> TransactionMessagePatterns
      = new Dictionary<ETransactionType, string>()
      {
        {
          ETransactionType.Credit,
          "Transaction not authorized: User '{0:s}' attempted to credit account {1} by {2:c}"
        },
        {
          ETransactionType.Debit,
          "Transaction not authorized: User '{0}' attempted to debit account '{1}' by {2:c}"
        }
      };

    //----------------------------------------------------------------------------------------------

    private static readonly Dictionary<ETransactionType, EAuthorizations> RequiredAuthorizationsByTransactionType
      = new Dictionary<ETransactionType, EAuthorizations>()

      {
        {ETransactionType.Credit, EAuthorizations.Credit},
        {ETransactionType.Debit, EAuthorizations.Debit}
      };

    //----------------------------------------------------------------------------------------------

    private static string GetTransactionAuthorizationExceptionMessage(string username,
                                                                      string accountNumber,
                                                                      ETransactionType transactionType,
                                                                      double amount)
    {
      string message;

      if (TransactionMessagePatterns.TryGetValue(transactionType, out string messagePattern))
      {
        message = string.Format(messagePattern,
                                username,
                                accountNumber,
                                amount);
      }
      else
      {
        message = "Transaction failed";
      }

      return message;
    }

    //----------------------------------------------------------------------------------------------

    private static EAuthorizations GetRequiredAuthorizationsByTransactionType(ETransactionType transactionType)
    {
      if (RequiredAuthorizationsByTransactionType.TryGetValue(transactionType, out var authorizations))
      {
        return authorizations;
      }

      return EAuthorizations.Update;
    }

    //----------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------

    public static Session Login(Repository repository, string username)
    {
      User user;

      using (repository.Read())
      {
        user = repository.GetUserByUsername(username);

        if (user == null)
        {
          throw new AuthenticationException();
        }
      }

      return Session.Make(repository, new User(user)).Validate();
    }

    //----------------------------------------------------------------------------------------------

    public static void Logout(Session session)
    {
      AssertIsValidSession(session);

      using (session.Repository.Read())
      {
        // No matter what happens, this session is now invalid.
        session.Invalidate();  

        var user = session.Repository.GetUserByUsername(session.User.Username);

        if (user == null)
        {
          throw new AuthenticationException("Logout: unknown user '{username}'");
        }
      }
    }

    //----------------------------------------------------------------------------------------------
    // Add a new User.
    //----------------------------------------------------------------------------------------------

    public static User CreateUser(Session session, string newUsername)
    {
      AssertIsValidSession(session);

      User newUser;

      var repository = session.Repository;

      using (repository.Write())
      {
        session.Repository.AssertIsAdminUser(session.User.Username);

        var existingUser = repository.GetUserByUsername(newUsername);

        if (existingUser != null)
        {
          throw new InvalidRequestException("Bad Request");
        }

        newUser = repository.AddUser(newUsername, Repository.THE_NULL_ID);
      }

      return new User(newUser);
    }

    //----------------------------------------------------------------------------------------------
    // Add a new Client and bind it to an existing User.
    //----------------------------------------------------------------------------------------------

    public static Client CreateClient(Session session,
                                      string ownerUsername,
                                      string clientFirstName,
                                      string clientLastName)
    {
      AssertIsValidSession(session);

      Client newClient;

      var repository = session.Repository;

      using (repository.Write())
      {
        repository.AssertIsAdminUser(session.User.Username);

        var ownerUser = repository.GetUserByUsername(ownerUsername);

        if (ownerUser == null)
        {
          throw new InvalidRequestException("Bad Request");
        }

        newClient = repository.AddClient(ownerUser.Id, clientFirstName, clientLastName);

        if (repository.SetUserClientOid(ownerUser.Id, newClient.Id) == null)
        {
          throw new InvalidRequestException("Bad Request");
        }

        // TODO: Consider adding an authorization for the User to this Client, rather than the existing hard bind.
      }

      return new Client(newClient);
    }

    //----------------------------------------------------------------------------------------------
    // Add a new Account to an existing Client.
    //----------------------------------------------------------------------------------------------

    public static Account CreateAccount(Session session, string clientId)
    {
      AssertIsValidSession(session);

      Account account;

      var repository = session.Repository;

      using (repository.Write())
      {
        repository.AssertIsAdminUser(session.User.Username);

        var client = repository.GetClientByClientId(clientId);

        if (client == null)
        {
          throw new InvalidRequestException("Bad Request");
        }

        account = repository.AddAccount(client.Id, session.User.Id);

        var owningUser = repository.GetUserByClientOid(client.Id);

        // ReSharper disable once UnusedVariable
        var authorization = repository.AddAuthorization(owningUser.Id,
                                                       account.Id,
                                                       (  EAuthorizations.Review
                                                        | EAuthorizations.Credit
                                                        | EAuthorizations.Debit));
      }

      return new Account(account);
    }

    //----------------------------------------------------------------------------------------------
    // Create a new Account, Client, and User in one operation.
    //----------------------------------------------------------------------------------------------

    // TODO: Deprecated
    public static Account CreateAccount(Session session,
                                        string ownerUsername,
                                        string clientFirstName,
                                        string clientLastName)
    {
      AssertIsValidSession(session);

      Account account;

      var repository = session.Repository;

      using (repository.Write())
      {
        repository.AssertIsAdminUser(session.User.Username);

        var ownerUser = repository.GetUserByUsername(ownerUsername);

        if (ownerUser != null)
        {
          throw new InvalidRequestException("Bad Request");
        }

        ownerUser = repository.AddUser(ownerUsername, Repository.THE_NULL_ID);

        var client = repository.AddClient(ownerUser.Id, clientFirstName, clientLastName);

        if (repository.SetUserClientOid(ownerUser.Id, client.Id) == null)
        {
          throw new InvalidRequestException("Bad Request");
        }

        account = repository.AddAccount(ownerUser.ClientId, session.User.Id);

        // ReSharper disable once UnusedVariable
        var authorization = repository.AddAuthorization(ownerUser.Id,
                                                        account.Id,
                                                        (  EAuthorizations.Review
                                                         | EAuthorizations.Credit
                                                         | EAuthorizations.Debit));

      }

      return new Account(account);
    }

    //----------------------------------------------------------------------------------------------

    public static void ExecuteTransaction(Session session,
                                          string accountNumber,
                                          ETransactionType transactionType,
                                          double amount,
                                          string memo = null)
    {
      var repository = session.Repository;
      var requestingUserName = (session.User.Username ?? TheInvalidStringValue);

      using (repository.Write())
      {
        var requestingUsername = session.User.Username;
        var account = repository.GetAccountByAccountNumber(accountNumber);

        if (account == null)
        {
          var message = GetTransactionAuthorizationExceptionMessage(requestingUserName, TheInvalidStringValue,
            transactionType, amount);

          throw new AuthorizationException(message);
        }

        var client = repository.GetClientByOid(account.ClientId);

        if (client == null)
        {
          var message = GetTransactionAuthorizationExceptionMessage(requestingUserName, account.AccountNumber,
            transactionType, amount);

          throw new AuthorizationException(message);
        }

        var requestingUser = repository.GetUserByUsername(requestingUsername);

        if (requestingUser == null)
        {
          var message = GetTransactionAuthorizationExceptionMessage(requestingUserName, account.AccountNumber,
            transactionType, amount);

          throw new AuthorizationException(message);
        }

        var requiredAuthorization = GetRequiredAuthorizationsByTransactionType(transactionType);

        if (!(requestingUser.IsAdmin || repository.IsAuthorized(requestingUser.Id, account.Id, requiredAuthorization)))
        {
          var message = GetTransactionAuthorizationExceptionMessage(requestingUser.Username, account.AccountNumber,
            transactionType, amount);

          throw new AuthorizationException(message);
        }

        if (transactionType == ETransactionType.Debit)
        {
          var currentBalance = GetBalance(session, account.AccountNumber);

          if (currentBalance < amount)
          {
            throw new OverdraftException(account.AccountNumber);
          }
        }

        repository.AddTransaction(account.Id, transactionType, Math.Abs(amount), memo);
      }
    }

    //----------------------------------------------------------------------------------------------

    private static AccountSnapshot GetAccountSnapshot(Session session, Oid accountOid)
    {
      AssertIsValidSession(session);

      var repository = session.Repository;

      AccountSnapshot snapshot = null;

      using (repository.Read())
      {
        var account = repository.GetAccountByOid(accountOid);

        if (account != null)
        {
          var transactions = repository.GetTransactionSnapshot(accountOid);

          snapshot = AccountSnapshot.Make(transactions, account);
        }
      }

      return snapshot;
    }

    //----------------------------------------------------------------------------------------------

    public static double GetBalance(Session session, string accountNumber)
    {
      AssertIsValidSession(session);

      var repository = session.Repository;
      var requestingUser = session.User;

      AccountSnapshot snapshot;

      using (repository.Read())
      {
        var account = repository.GetAccountByAccountNumber(accountNumber);

        if (account == null)
        {
          throw new InvalidRequestException("Bad request");
        }

        repository.AssertIsUserAuthorized(requestingUser.Username, account.Id, EAuthorizations.Review);

        snapshot = GetAccountSnapshot(session, account.Id);
      }

      return snapshot.Balance;
    }

    //----------------------------------------------------------------------------------------------

    public static IEnumerable<AnonymousTransaction> GetTransactionHistory(Session session,
                                                                          string accountNumber)
    {
      var repository = session.Repository;
      var requestingUser = session.User;

      AccountSnapshot snapshot;

      using (repository.Read())
      {
        var account = repository.GetAccountByAccountNumber(accountNumber);

        repository.AssertIsUserAuthorized(requestingUser.Username, account.Id, EAuthorizations.Review);

        snapshot = GetAccountSnapshot(session, account.Id);
      }

      return snapshot.History;
    }
  }
}

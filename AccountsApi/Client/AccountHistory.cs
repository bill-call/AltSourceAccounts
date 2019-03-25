using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccountsLib;

namespace AccountsApi.Client
{
  /// <summary>
  /// Represents an account's transaction history outside of the Accounts API.
  /// </summary>
  public class AccountHistory
  {
    public double StartingBalance { get; set; }
    public double FinalBalance { get; set; }
    public List<Transaction> History { get; set; }

    /// <summary>
    /// Default string dump of a transaction history.  Override if you want something different.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
      var builder = new StringBuilder();

      builder.AppendLine($"{"Starting Balance:", -17} {StartingBalance, 16:C2}")
             .AppendLine($"{"Ending Balance:", -17} {FinalBalance, 16:C2}")
             .AppendLine()
             .AppendLine("Date                   Action             Amount           Balance Memo")
             .AppendLine("-----------------------------------------------------------------------");

      var runningBalance = StartingBalance;

      foreach (var transaction in History)
      {
        runningBalance += (  (transaction.Action == ETransactionType.Credit)
                           ? transaction.Amount
                           : (-transaction.Amount));

        builder.AppendLine(transaction.ToString(runningBalance));
      }

      return builder.ToString();
    }
  }
}

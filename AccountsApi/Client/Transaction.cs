using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountsLib;

namespace AccountsApi.Client
{
  public class Transaction
  {
    public Transaction() {}

    public Transaction(AnonymousTransaction t)
    {
      Timestamp = t.Timestamp;
      Action = (ETransactionType)Enum.Parse(typeof(ETransactionType), t.TransactionType.ToString());
      Amount = t.Amount;
      Memo = t.Memo;
    }

    public DateTime Timestamp { get; set; }
    public ETransactionType Action { get; set; }
    public double Amount { get; set; }
    public string Memo { get; set; }

    public string ToString(double runningBalance)
    {
      var amount = ((Action == ETransactionType.Credit) ? $"{Amount, 16:C2} " : $"{-Amount, 17:C2}");
      var balance = ((runningBalance >= 0) ? $"{runningBalance, 16:C2} " : $"{-runningBalance, 17:C2}");

      return $"[{Timestamp}] {Action, -6} {amount} {balance} {Memo ?? String.Empty}";
    }
  }
}

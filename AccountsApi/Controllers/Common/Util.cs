using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using AccountsLib;
using Common;
using IdentityModel;

namespace AccountsApi.Controllers.Common
{
  internal static class Util
  {
    public static string Quotify(string input)
    {
      const string quotes = "\"'";
      const string empty = "\"\"";

      var output = empty;

      if ((input != null) && (input.Length > 1))
      {
        output = input;

        var isQuoted = ((input[0] == input[input.Length - 1]) && quotes.Contains(input[0]));

        if (!isQuoted)
        {
          output = $"\"{input}\"";
        }
      }

      return output;
    }

    /// <summary>
    /// Removes outer single or double quotes from string.
    /// </summary>
    /// <param name="input">The string to be de-quoted.</param>
    /// <returns>
    /// Returns <c>input</c>, minus the bounding quotes (if any). If <c>input</c>
    /// was <c>null</c>, returns <c>String.Empty().</c>
    /// </returns>
    public static string Dequotify(string input)
    {
      var output = ((input == null) ? string.Empty : input.Trim().Trim('\'', '"'));

      return output;
    }

    /// <summary>
    /// Truncates a double with an arbitrary precision to just two decimal places.
    /// Always rounds down.
    /// </summary>
    /// <param name="value">The value to be truncated.</param>
    /// <returns></returns>
    public static double TruncateMoneyValue(double value)
    {
      return (Math.Floor(Math.Abs(value) * 100.0) / 100.0);
    }
  }
}
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common
{
  /// <summary>
  /// The validators in this class are used to guard inputs in key logic, particularly logic that
  /// persists data. A key element in protection from various XSS attacks.
  /// </summary>
  public static class UserInputValidation
  {
    private const string UsernameHelpText = @"A valid user name has between 1 and 20 characters, and must contain only letters, digits and the special character '_'";
    private const string PasswordHelpText = @"A valid password has between 1 and 20 characters, and must contain only letters, digits and the special characters '_' and '$'";
    private const string NameHelpText = @"A first or last name has between 1 and 20 characters, and must contain only letters.  It must also start with an uppercase letter.";
    private const string AmountHelpText = @"A valid credit/debit amount must be a positive number between 0.00 and 99,999.99, inclusive.";
    private const string AccountNumberHelpText = @"A valid account number is in the form 'A00000' (a capital letter 'A', followed by five digits).";
    private const string ClientIdHelpText = @"A valid account number is in the form 'C00000' (a capital letter 'C', followed by five digits).";
    private const string MemoHelpText = @"A valid memo must be no more than 64 characters in length and contain only numbers, letters, spaces, and the characters  . , ! ? - _ & $ # @ * : ' ~.";
    private const string AddressHelpText = @"The provided input is not a legal address block.";

    private static readonly Regex UsernameRegex = new Regex(@"^[a-zA-Z0-9_]{1,20}$", RegexOptions.Compiled);
    private static readonly Regex PasswordRegex = new Regex(@"^[a-zA-Z0-9_$]{1,20}$", RegexOptions.Compiled);
    private static readonly Regex NameRegex = new Regex(@"^[A-Z][a-zA-Z]{1,20}$", RegexOptions.Compiled);
    private static readonly Regex AmountRegex = new Regex(@"^\d{1,5}(.\d{0,2}){0,1}$", RegexOptions.Compiled);
    private static readonly Regex AccountNumberRegex = new Regex(@"^A\d{5}$", RegexOptions.Compiled);
    private static readonly Regex ClientIdRegex = new Regex(@"^C\d{5}$", RegexOptions.Compiled);
    private static readonly Regex MemoRegex = new Regex(@"^[\w .,!?-_&$#@*:~]{0,64}$", RegexOptions.Compiled);

    public delegate void Validator(string input);

    /// <summary>
    /// Determines whether or not <c>input</c> is a legal <c>username</c>.
    /// Throws <see cref="UserInputValidationException"/> if it not.
    /// </summary>
    /// <param name="input">The string to be validated.</param>
    /// <exception cref="UserInputValidationException"></exception>
    public static void ValidateUsername(string input)
    {
      Validate(input, UsernameRegex, UsernameHelpText);
    }

    /// <summary>
    /// Determines whether or not <c>input</c> is a legal <c>password</c>.
    /// Throws <see cref="UserInputValidationException"/> if it not.
    /// </summary>
    /// <param name="input">The string to be validated.</param>
    /// <exception cref="UserInputValidationException"></exception>
    public static void ValidatePassword(string input)
    {
      Validate(input, PasswordRegex, PasswordHelpText);
    }

    /// <summary>
    /// Determines whether or not <c>input</c> is a legal <c>Name</c> (first/last) or (given/family).
    /// Throws <see cref="UserInputValidationException"/> if it not.
    /// </summary>
    /// <param name="input">The string to be validated.</param>
    /// <exception cref="UserInputValidationException"></exception>
    public static void ValidateName(string input)
    {
      Validate(input, NameRegex, NameHelpText);
    }
    
    /// <summary>
    /// Determines whether or not <c>input</c> is a legal <c>amount</c>.
    /// Throws <see cref="UserInputValidationException"/> if it not.
    /// </summary>
    /// <param name="input">The string to be validated.</param>
    /// <exception cref="UserInputValidationException"></exception>
    public static void ValidateAmount(double input)
    {
      ValidateAmount(input.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Determines whether or not <c>input</c> is a legal <c>amount</c>.
    /// Throws <see cref="UserInputValidationException"/> if it not.
    /// </summary>
    /// <param name="input">The string to be validated.</param>
    /// <exception cref="UserInputValidationException"></exception>
    public static void ValidateAmount(string input)
    {
      Validate(input, AmountRegex, AmountHelpText);
    }

    /// <summary>
    /// Determines whether or not <c>input</c> is a legal <c>Account Number</c>.
    /// Throws <see cref="UserInputValidationException"/> if it not.
    /// </summary>
    /// <param name="input">The string to be validated.</param>
    /// <exception cref="UserInputValidationException"></exception>
    public static void ValidateAccountNumber(string input)
    {
      Validate(input, AccountNumberRegex, AccountNumberHelpText);
    }

    /// <summary>
    /// Determines whether or not <c>input</c> is a legal <c>Client ID</c>.
    /// Throws <see cref="UserInputValidationException"/> if it not.
    /// </summary>
    /// <param name="input">The string to be validated.</param>
    /// <exception cref="UserInputValidationException"></exception>
    public static void ValidateClientId(string input)
    {
      Validate(input, ClientIdRegex, ClientIdHelpText);
    }

    /// <summary>
    /// Determines whether or not <c>input</c> is a legal <c>JSON</c> address block.
    /// Throws <see cref="UserInputValidationException"/> if it not.
    /// </summary>
    /// <param name="input">The string to be validated.</param>
    /// <exception cref="UserInputValidationException"></exception>
    public static void ValidateAddress(string input)
    {
      if (!(String.IsNullOrEmpty(input) || IsValidJson(input)))
      {
        throw new UserInputValidationException(AddressHelpText);
      }
    }

    /// <summary>
    /// Determines whether or not <c>input</c> is a legal <c>memo</c> string.
    /// Throws <see cref="UserInputValidationException"/> if it not.
    /// </summary>
    /// <param name="input">The string to be validated.</param>
    /// <exception cref="UserInputValidationException"></exception>
    public static void ValidateMemo(string input)
    {
      Validate(input, MemoRegex, MemoHelpText);
    }

    /// <summary>
    /// Tests to see if <c>input</c> matches some Regular Expression.  Throws <see cref="UserInputValidationException"/> if it does not./>
    /// </summary>
    /// <param name="input">A string to be tested against <c>pattern</c>.</param>
    /// <param name="pattern">A Regular Expression to test <c>input</c> against.</param>
    /// <param name="helpText">A string to be appended to the message text associated with failed validations.</param>
    /// <returns>void</returns>
    /// <exception cref="UserInputValidationException"></exception>
    private static void Validate(string input, Regex pattern, string helpText)
    {
      Debug.Assert(pattern != null, nameof(pattern) + " != null");

      input = (input ?? String.Empty);

      if (!pattern.IsMatch(input))
      {
        throw new UserInputValidationException($"The provided input is not legal. {helpText}.");
      }
    }

    /// <summary>
    /// Like <see cref="Validate(string, Regex, string)"/>, but returns <c>false</c> if <c>input</c> fails to match <c>pattern</c>,
    /// instead of throwing an exception.
    /// </summary>
    /// <param name="input">A string to be tested against <c>pattern</c>.</param>
    /// <param name="pattern">A Regular Expression to test <c>input</c> against.</param>
    /// <returns>Returns <c>true></c>, if <c>input</c> matches <c>pattern</c>. Otherwise; <c>false</c>.</returns>
    private static bool IsValid(string input, Regex pattern)
    {
        Debug.Assert(pattern != null, nameof(pattern) + " != null");

        input = (input ?? String.Empty);

        var isValid = pattern.IsMatch(input);

        return isValid;
    }

    /// <summary>
    /// Attempts to determine whether or not <c>input</c> is valid JSON.
    /// </summary>
    /// <param name="input">The string to test.</param>
    /// <returns>If <c>input</c> is valid JSON, returns <c>true</c>; <c>false</c>, otherwise.</returns>
    private static bool IsValidJson(string input)
    {
      input = input.Trim();

      if (   (input.StartsWith("{") && input.EndsWith("}")) //For object
          || (input.StartsWith("[") && input.EndsWith("]"))) //For array
      {
        try
        {
          // ReSharper disable once UnusedVariable
          var obj = JToken.Parse(input);

          return true;
        }
        catch (JsonReaderException jex)
        {
          //Exception in parsing json
          Console.WriteLine(jex.Message);

          return false;
        }
        catch (Exception ex) //some other exception
        {
          Console.WriteLine(ex.ToString());

          return false;
        }
      }
      else
      {
        return false;
      }
    }
  }
}

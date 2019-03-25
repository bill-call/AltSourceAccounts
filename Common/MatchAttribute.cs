using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

namespace Common
{
  /// <summary>Specifies that a data field value in ASP.NET Dynamic Data must match the specified regular expression.</summary>
  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
  public class MatchAttribute : ValidationAttribute
  {
    /// <summary>Initializes a new instance of the <see cref="T:System.ComponentModel.DataAnnotations.RegularExpressionAttribute"></see> class.</summary>
    /// <param name="pattern">The regular expression that is used to validate the data field value.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="pattern">pattern</paramref> is null.</exception>
    public MatchAttribute(string pattern)
    {
     Pattern = pattern;
    }

    /// <summary>Gets or set the amount of time in milliseconds to execute a single matching operation before the operation times out.</summary>
    /// <returns>The amount of time in milliseconds to execute a single matching operation.</returns>
    public int MatchTimeoutInMilliseconds { get; set; }

    /// <summary>Gets the regular expression pattern.</summary>
    /// <returns>The pattern to match.</returns>
    public string Pattern { get; }

    /// <summary>Formats the error message to display if the regular expression validation fails.</summary>
    /// <param name="name">The name of the field that caused the validation failure.</param>
    /// <returns>The formatted error message.</returns>
    public override string FormatErrorMessage(string name)
    {
      return $"Baaaad {name}";
    }

    /// <summary>Checks whether the value entered by the user matches the regular expression pattern.</summary>
    /// <param name="value">The data field value to validate.</param>
    /// <returns>true if validation is successful; otherwise, false.</returns>
    /// <exception cref="T:System.ComponentModel.DataAnnotations.ValidationException">The data field value did not match the regular expression pattern.</exception>
    public override bool IsValid(object value)
    {
      var isMatch = Regex.IsMatch(value.ToString(), Pattern);

      return isMatch;
    }
  }
}

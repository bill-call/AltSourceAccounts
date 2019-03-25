using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AccountsCli.Util
{
  public static class CommandTokenizer
  {
    static readonly Regex Pattern = new Regex(@"((\s*""(?<token>[^""]*)(""|$)\s*)|(\s*(?<token>[^\s""]+)\s*))*",
                                              (RegexOptions.Compiled | RegexOptions.ExplicitCapture));

    public static IEnumerable<string> Tokenize(this string input)
    {
      var match = Pattern.Match(input);
      var matches = (match.Success ? match.Groups["token"].Captures.Select(c => c.Value) : new string[0]);

      return (matches ?? default(string[]));
    }
  }
}

using System;
using System.Collections.Generic;
using System.Text;
using McMaster.Extensions.CommandLineUtils;

namespace AccountsCli.Util
{
  public static class CmdLineExtensions
  {
    public static bool HasValue(this CommandArgument arg)
    {
      return (!String.IsNullOrEmpty(arg?.Value));
    }
  }
}

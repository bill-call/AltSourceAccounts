using System;

namespace AccountsCli.Util
{
  public class LoginException : Exception
  {
     public LoginException(string message) : base(message) { }
  }
}
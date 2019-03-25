using System;

namespace Common
{
  public class UserInputValidationException : Exception
  {
    public UserInputValidationException(string message) : base(message)
    {
    }
  }
}

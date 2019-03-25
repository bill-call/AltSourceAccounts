using System;

namespace Common
{
  public class AltSourceNewClientDto
  {
    public AltSourceNewClientDto() { }

    public AltSourceNewClientDto(string username, string firstName, string lastName)
    {
      Username = username;
      FirstName = firstName;
      LastName = lastName;
    }

    public string Username { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
  }
}
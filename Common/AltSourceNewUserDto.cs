using System;

namespace Common
{
  public class AltSourceNewUserDto
  {
    public AltSourceNewUserDto() {}

    public AltSourceNewUserDto(string username,
                               string password,
                               string firstName,
                               string lastName,
                               bool isAdmin)
    {
      Username = username;
      Password = password;
      FirstName = firstName;
      LastName = lastName;
      Address = String.Empty;
      IsAdmin = isAdmin;
    }

    public string Username { get; set; }
    public string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Address { get; set; }
    public bool IsAdmin { get; set; }
  }
}
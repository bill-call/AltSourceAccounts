namespace Common
{
  public class AccessTokenPair
  {
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }

    public AccessTokenPair(AccessTokenPair pair) : this(pair.AccessToken, pair.RefreshToken) {}

    public AccessTokenPair(string accessToken, string refreshToken)
    {
      AccessToken = accessToken;
      RefreshToken = refreshToken;
    }

    public bool IsNull()
    {
      return ((AccessToken == null) && (RefreshToken == null));
    }
  }
}
namespace Common
{
  /// <summary>
  /// An simple, synchronous, implementation of <see cref="ITokenManager"/>, suitable for
  /// single-threaded applications. Explicitly *not* thread-safe.
  /// </summary>
  /// 
  public sealed class DefaultTokenManager : ITokenManager
  {
  public static readonly AccessTokenPair TheNullAccessTokenPair = new AccessTokenPair(null, null);

    private AccessTokenPair _internalAccessTokenPair = TheNullAccessTokenPair;

    //----------------------------------------------------------------------------------------------
    // Note that this implementation can be made trivially (if not necessarily efficiently)
    // thread safe by the application of appropriate memory barriers/fences to this
    // property.
    //----------------------------------------------------------------------------------------------

    private AccessTokenPair AccessTokens
    {
      get => new AccessTokenPair(_internalAccessTokenPair);
      set => _internalAccessTokenPair = new AccessTokenPair(value);
    }

    //----------------------------------------------------------------------------------------------

    public AccessTokenPair GetAccessTokens()
    {
      return AccessTokens;
    }

    //----------------------------------------------------------------------------------------------

    public void SetAccessTokens(AccessTokenPair accessTokens)
    {
      AccessTokens = accessTokens;
    }

    //----------------------------------------------------------------------------------------------

    public void SetAccessTokens(string accessToken, string refreshToken)
    {
      AccessTokens = new AccessTokenPair(accessToken, refreshToken);
    }

    //----------------------------------------------------------------------------------------------

    public void ClearAccessTokens()
    {
      AccessTokens = TheNullAccessTokenPair;
    }

    //----------------------------------------------------------------------------------------------

    public void Dispose()
    {
    }
  }
}

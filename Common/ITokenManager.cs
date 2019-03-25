using System;

namespace Common
{
  /// <summary>
  /// A simple interface defining a service capable of storing Access and Refresh Tokens.
  /// This service should be implemented to safely store these tokens, possibly in
  /// a remote security service. Note that the interface is defined such that
  /// thread-safe and/or asynchronous implementations are possible, but not required.
  /// </summary>
  public interface ITokenManager : IDisposable
  {
    AccessTokenPair GetAccessTokens();

    void SetAccessTokens(AccessTokenPair tokens);
    void SetAccessTokens(string accessToken, string refreshToken);

    void ClearAccessTokens();
  }
}

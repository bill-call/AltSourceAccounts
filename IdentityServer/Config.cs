// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityServer4.Models;
using System.Collections.Generic;
using Common;
using IdentityModel;
using IdentityServer4;
using Microsoft.Extensions.Configuration;

namespace IdentityServer
{
  public static class Config
  {
    public static IEnumerable<IdentityResource> GetIdentityResources()
    {
      return new IdentityResource[]
      {
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),

        new IdentityResource("foo333", "the foo IR", new [] {"s1", "s2" })
        {
          Required = true
        }
      };
    }

    public static IEnumerable<ApiResource> GetApis()
    {
      return new List<ApiResource>
      {
        new ApiResource(ApplicationScopes.ACCOUNTS_API, "AltSource Accounts API", new List<string> {"role", "profile",})
        {
          Name = ApplicationScopes.ACCOUNTS_API,
          DisplayName = "AltSource Accounts API",

          UserClaims =
          {
            JwtClaimTypes.Profile,
            JwtClaimTypes.PreferredUserName,
            JwtClaimTypes.GivenName,
            JwtClaimTypes.FamilyName,
            ApplicationClaimTypes.ROLE,
            ApplicationClaimTypes.ACCOUNTS_USERNAME
          }
        },

        new ApiResource(ApplicationScopes.USER_MANAGEMENT_API, "AltSource User Management API", new List<string> {"role", "profile"})
      };
    }

    public static IEnumerable<Client> GetClients(IConfiguration configuration)
    {
      var accountsCliUrl = configuration["IdentityServer:Clients:AccountsCli:Url"];
      var accountsWebUrl = configuration["IdentityServer:Clients:AccountsWeb:Url"];
      var accountsCliClientSecret = configuration["IdentityServer:Clients:AccountsCli:ClientSecret"];
      var accountsWebClientSecret = configuration["IdentityServer:Clients:AccountsWeb:ClientSecret"];

      return new List<Client>
      {
        // AccountsCli
        new Client
        {
          ClientId = "AccountsCli",

          // No interactive user, use the client-id/secret for authentication
          AllowedGrantTypes = GrantTypes.ClientCredentials,

          // Secret for authentication
          ClientSecrets =
          {
            new Secret(accountsCliClientSecret.Sha256())
          },

          // scopes that client has access to
          AllowedScopes =
          {
            ApplicationScopes.ACCOUNTS_API,
            ApplicationScopes.USER_MANAGEMENT_API
          }
        },

        // AccountsCli.ro
        // Resource owner password grant client
        new Client
        {
          ClientId = "AccountsCli.ro",

          // Will send credentials from client to resource provider (service/api); credentials known to client.  Decidedly insecure.
          AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,

          AllowOfflineAccess = true,
          SlidingRefreshTokenLifetime = (10 * 60),
          AbsoluteRefreshTokenLifetime = (30 * 60),
          RefreshTokenUsage = TokenUsage.OneTimeOnly,
          UpdateAccessTokenClaimsOnRefresh = true,

          ClientSecrets =
          {
            new Secret(accountsCliClientSecret.Sha256())
          },

          AllowedScopes =
          {
            IdentityServerConstants.StandardScopes.OpenId,
            IdentityServerConstants.StandardScopes.Profile,
            ApplicationScopes.ACCOUNTS_API,
            ApplicationScopes.USER_MANAGEMENT_API
          }
        },

        // OpenID Connect Hybrid flow client (MVC)
        new Client
        {
          ClientId = "AccountsWeb",
          ClientName = "AltSource Accounts Web Client",
          AllowedGrantTypes = GrantTypes.Hybrid,
          AllowOfflineAccess = true,

         // SlidingRefreshTokenLifetime = (10 * 60),
         // AbsoluteRefreshTokenLifetime = (30 * 60),
         // RefreshTokenUsage = TokenUsage.OneTimeOnly,
         // UpdateAccessTokenClaimsOnRefresh = true,

          ClientSecrets =
          {
            new Secret(accountsWebClientSecret.Sha256())
          },

          // Where to redirect to after login
          RedirectUris = { $"{accountsWebUrl}/signin-oidc" },

          // Where to redirect to after logout
          PostLogoutRedirectUris = {  $"{accountsWebUrl}/signout-callback-oidc" },

          // CORS (Cross-Origin Resource Sharing)
          AllowedCorsOrigins = { accountsWebUrl },

          AllowedScopes = new List<string>
          {
            IdentityServerConstants.StandardScopes.OpenId,
            IdentityServerConstants.StandardScopes.Profile,
            ApplicationScopes.ACCOUNTS_API,
            ApplicationScopes.USER_MANAGEMENT_API
          },
        }
      };
    }
  }
}
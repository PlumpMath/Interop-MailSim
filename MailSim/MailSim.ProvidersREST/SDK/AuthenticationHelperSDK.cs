﻿using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading.Tasks;

using MailSim.Common;

namespace MailSim.ProvidersREST
{
    /// <summary>
    /// Provides clients for the different service endpoints.
    /// </summary>
    internal static class AuthenticationHelperSDK
    {
        private static readonly string ClientID = Resources.ClientID;
        private static string TenantId { get; set; }
        private const string AadServiceResourceId = "https://graph.windows.net/";

        private static readonly Uri ReturnUri = new Uri(Resources.ReturnUri);

        // Properties used for communicating with your Windows Azure AD tenant.
        private const string CommonAuthority = "https://login.microsoftonline.com/Common";
        internal const string OfficeResourceId = "https://outlook.office365.com/";

        private const string ModuleName = "AuthenticationHelper";

        //Static variables store the clients so that we don't have to create them more than once.
        private static ActiveDirectoryClient _graphClient = null;

        //Property for storing the authentication context.
        private static AuthenticationContext _authenticationContext { get; set; }

        //Property for storing and returning the authority used by the last authentication.
        private static string LastAuthority { get; set; }
        //Property for storing the tenant id so that we can pass it to the ActiveDirectoryClient constructor.
        // Property for storing the logged-in user so that we can display user properties later.
        internal static string LoggedInUser { get; set; }

        private static string UserName { get; set; }
        private static string Password { get; set; }

        /// <summary>
        /// Checks that a Graph client is available.
        /// </summary>
        /// <returns>The Graph client.</returns>
        internal static async Task<ActiveDirectoryClient> GetGraphClientAsync(string userName, string password)
        {
            //Check to see if this client has already been created. If so, return it. Otherwise, create a new one.
            if (_graphClient != null)
            {
                return _graphClient;
            }

            UserName = userName;
            Password = password;

            // Active Directory service endpoints
            Uri aadServiceEndpointUri = new Uri(AadServiceResourceId);

            try
            {
                //First, look for the authority used during the last authentication.
                //If that value is not populated, use CommonAuthority.
                string authority = String.IsNullOrEmpty(LastAuthority) ? CommonAuthority : LastAuthority;

                // Create an AuthenticationContext using this authority.
                _authenticationContext = new AuthenticationContext(authority);

                var token = await GetTokenHelperAsync(_authenticationContext, AadServiceResourceId);

                // Check the token
                if (string.IsNullOrEmpty(token))
                {
                    // User cancelled sign-in
                    throw new Exception("Sign-in cancelled");  // assuming we don't want to continue
                }
                else
                {
                    // Create our ActiveDirectory client.
                    _graphClient = new ActiveDirectoryClient(
                        new Uri(aadServiceEndpointUri, TenantId),
                        async () => await GetTokenHelperAsync(_authenticationContext, AadServiceResourceId));

                    return _graphClient;
                }
            }
            catch (Exception)
            {
                _authenticationContext.TokenCache.Clear();
                throw;
            }
        }

        internal static string GetToken(string resourceId)
        {
            return GetTokenHelper(_authenticationContext, resourceId);
        }

        internal static async Task<string> GetTokenAsync(string resourceId)
        {
            return await GetTokenHelperAsync(_authenticationContext, resourceId);
        }

        // Get an access token for the given context and resourceId. An attempt is first made to 
        // acquire the token silently. If that fails, then we try to acquire the token by prompting the user.
        private static async Task<string> GetTokenHelperAsync(AuthenticationContext context, string resourceId)
        {
            return await Task.Run(() => GetTokenHelper(context, resourceId));
        }

        private static string GetTokenHelper(AuthenticationContext context, string resourceId)
        {
            string accessToken = null;
            
            try
            {
                AuthenticationResult result;

                if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(Password))
                {
                    result = context.AcquireToken(resourceId, ClientID, ReturnUri);
                }
                else
                {
                    result = context.AcquireToken(resourceId, ClientID, new UserCredential(UserName, Password));
                }

                accessToken = result.AccessToken;

                LoggedInUser = result.UserInfo.UniqueId;
                TenantId = result.TenantId;
                LastAuthority = context.Authority;
            }
            catch (Exception ex)
            {
                Log.Out(Log.Severity.Warning, ModuleName, ex.ToString());
            }

            return accessToken;
        }
    }
}

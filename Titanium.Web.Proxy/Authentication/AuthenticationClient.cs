using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Models;

namespace Titanium.Web.Proxy.Authentication
{
    /// <summary>
    /// Implements support for various HTTP authentication schemes
    /// </summary>
    public static class AuthenticationClient
    {
        /// <summary>
        /// Authenticate to the specified remote URI.
        /// </summary>
        /// <param name="remoteUri">The remote URI.</param>
        /// <param name="challengeHeader">The challenge header.</param>
        /// <param name="credentialProvider">The credential provider.</param>
        /// <param name="secureTransportContext">The secure transport context.</param>
        /// <returns>Authorization header.</returns>
        private static async Task<HttpHeader> AuthenticateInternal(Uri remoteUri, HttpHeader challengeHeader, ICredentialProvider credentialProvider, bool preAuthenticationFailed, TransportContext secureTransportContext = null)
        {
            if (challengeHeader == null)
            {
                return null;
            }

            try
            {
                var authenticationRequest = WebRequest.Create(remoteUri);

                // If preauthentication is failed previously remove relevant cache entry
                if (preAuthenticationFailed)
                {
                    AuthorizationHeaderCache.Remove(remoteUri.Host);
                }

                HttpHeaderCollection proxyAuthorizationHeaders;

                AuthorizationHeaderCache.TryGetProxyAuthorizationHeaders(remoteUri.Host, out proxyAuthorizationHeaders);

                if (proxyAuthorizationHeaders != null)
                {
                    foreach (var header in proxyAuthorizationHeaders.Values)
                    {
                        authenticationRequest.Headers.Add(header.Name, header.Value);
                    }    
                }

                var requestType = authenticationRequest.GetType();

                requestType.InvokeMember("Async", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetProperty, null, authenticationRequest, new object[]
                {
                    false
                });

                var serverAuthenticationState = requestType.InvokeMember("ServerAuthenticationState", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetProperty, null, authenticationRequest, new object[0]);

                var serverAuthenticationStateType = serverAuthenticationState.GetType();

                serverAuthenticationStateType.InvokeMember("ChallengedUri", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField, null, serverAuthenticationState, new object[]
                {
                        remoteUri
                });

                if (secureTransportContext != null)
                {
                    serverAuthenticationStateType.InvokeMember("_TransportContext", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField, null, serverAuthenticationState, new object[]
                        {
                            secureTransportContext
                        });
                }

                var credentials = await credentialProvider.GetCredentials();
                var authorization = AuthenticationManager.Authenticate(challengeHeader.Value, authenticationRequest, credentials);

                var authorizationHeader = challengeHeader.Name.StartsWith("WWW", StringComparison.CurrentCultureIgnoreCase)
                    ? "Authorization"
                    : "Proxy-Authorization";

                return new HttpHeader(authorizationHeader, authorization?.Message);
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static async Task Authenticate(Uri remoteUri, HttpHeader challengeHeader,
            ICredentialProvider credentialProvider, IDictionary<string, HttpHeader> requestHeaders, 
            bool preAuthenticationFailed, TransportContext secureTransportContext = null)
        {
            var authorizationHeader = await AuthenticateInternal(remoteUri, challengeHeader, credentialProvider, preAuthenticationFailed, secureTransportContext);

            if (authorizationHeader != null)
            {
                requestHeaders[authorizationHeader.Name] = authorizationHeader;
                requestHeaders["Connection"] = new HttpHeader("Connection", "Keep-Alive");
                requestHeaders["Proxy-Connection"] = new HttpHeader("Proxy-Connection", "Keep-Alive");

                AuthorizationHeaderCache.Cache(remoteUri.Host, authorizationHeader);
            }
        }

        /// <summary>
        /// Authenticates the specified client wrapper.
        /// </summary>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="requestHeaders">The request headers.</param>
        /// <returns>Task.</returns>
        /// <exception cref="InvalidOperationException">Authorization failed.</exception>
        internal static void PreAuthenticate(Uri requestUri, IDictionary<string, HttpHeader> requestHeaders)
        {
            HttpHeaderCollection authorizationHeaderCollection;
            
            AuthorizationHeaderCache.TryGetAllAuthorizationHeaders(requestUri.Host, out authorizationHeaderCollection);

            if (authorizationHeaderCollection == null)
            {
                return;
            }

            foreach (var authorizationHeader in authorizationHeaderCollection.Values)
            {
                requestHeaders[authorizationHeader.Name] = authorizationHeader;
            }

            requestHeaders["Connection"] = new HttpHeader("Connection", "Keep-Alive");
            requestHeaders["Proxy-Connection"] = new HttpHeader("Proxy-Connection", "Keep-Alive");
        }
    }
}
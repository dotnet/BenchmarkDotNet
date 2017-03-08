#if CORE
using System;
using System.Net;

namespace RestSharp.Authenticators
{
    /// <summary>
    /// NtlmAuthenticator undefined for .netcore, though NetworkCredentials are in netstandart. So just implement as portability crudge
    /// </summary>
    internal class NtlmAuthenticator : IAuthenticator
    {
        private readonly ICredentials credentials;

        public NtlmAuthenticator()
            : this(CredentialCache.DefaultCredentials) { }

        public NtlmAuthenticator(string username, string password)
            : this(new NetworkCredential(username, password)) { }

        public NtlmAuthenticator(ICredentials credentials)
        {
            if (credentials == null)
            {
                throw new ArgumentNullException("credentials");
            }

            this.credentials = credentials;
        }

        public void Authenticate(IRestClient client, IRestRequest request)
        {
            request.Credentials = this.credentials;
        }
    }
}

#endif
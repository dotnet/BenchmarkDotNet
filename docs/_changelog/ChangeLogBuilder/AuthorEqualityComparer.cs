using System.Collections.Generic;
using Octokit;

namespace ChangeLogBuilder
{
    public class AuthorEqualityComparer : IEqualityComparer<Author>
    {
        public static readonly IEqualityComparer<Author> Default = new AuthorEqualityComparer();
        
        public bool Equals(Author x, Author y) => x.Login == y.Login;

        public int GetHashCode(Author author) => author.Login.GetHashCode();
    }
}
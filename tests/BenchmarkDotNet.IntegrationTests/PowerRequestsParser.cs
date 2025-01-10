using System;
using System.Collections.Generic;
using System.IO;

namespace BenchmarkDotNet.IntegrationTests;

/// <summary>
/// Parses the output of 'powercfg /requests' command into a list of <see cref="PowerRequest"/>s.
/// </summary>
/// <remarks>
/// <para>
/// Not using <see cref="https://github.com/sprache/Sprache">Sprache</see>. It is superseded by Superpower.
/// Not using <see cref="https://github.com/datalust/superpower">Superpower</see>. I gained more knowledge
/// implementing this class from scratch.
/// </para>
/// <para>Example input:</para>
/// <code>
/// DISPLAY:
/// [PROCESS] \Device\HarddiskVolume3\Program Files (x86)\Google\Chrome\Application\chrome.exe
/// Video Wake Lock
///
/// SYSTEM:
/// [DRIVER] Realtek High Definition Audio(SST) ...
/// Er wordt momenteel een audiostream gebruikt.
/// [PROCESS] \Device\HarddiskVolume3\...\NoSleep.exe
/// [PROCESS] \Device\HarddiskVolume3\Program Files (x86)\Google\Chrome\Application\chrome.exe
/// Video Wake Lock
///
/// AWAYMODE:
/// None.
///
/// EXECUTION:
/// [PROCESS] \Device\HarddiskVolume3\Program Files (x86)\Google\Chrome\Application\chrome.exe
/// Playing audio
///
/// PERFBOOST:
/// None.
///
/// ACTIVELOCKSCREEN:
/// None.
///
/// </code>
/// </remarks>
internal class PowerRequestsParser
{
    /// <summary>
    /// Parses output of 'powercfg /requests' into a list of <see cref="PowerRequest"/>s.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method takes a list of <see cref="Token"/>s. Examines next token and decides how to
    /// parse.
    /// </para>
    /// </remarks>
    /// <param name="input">Output of 'powercfg /requests'.</param>
    public static IEnumerable<PowerRequest> Parse(string input)
    {
        using TokenStream tokens = new TokenStream(Tokens(input));
        while (tokens.TryPeek().HasValue)
        {
            foreach (PowerRequest item in ParseRequestType(tokens))
            {
                yield return item;
            }
        }
    }

    private static IEnumerable<PowerRequest> ParseRequestType(TokenStream tokens)
    {
        Token requestType = tokens.Take(TokenType.RequestType);
        if (tokens.Peek().TokenType == TokenType.RequesterType)
        {
            while (tokens.Peek().TokenType == TokenType.RequesterType)
            {
                yield return ParseRequesterType(requestType, tokens);
            }
        }
        else
        {
            _ = tokens.Take(TokenType.None);
        }
        _ = tokens.Take(TokenType.EmptyLine);
    }

    private static PowerRequest ParseRequesterType(Token requestType, TokenStream tokens)
    {
        Token requesterType = tokens.Take(TokenType.RequesterType);
        Token requesterName = tokens.Take(TokenType.RequesterName);
        Token? reason = null;
        if (tokens.Peek().TokenType == TokenType.Reason)
        {
            reason = tokens.Take(TokenType.Reason);
        }
        return new PowerRequest(requestType.Value, requesterType.Value, requesterName.Value, reason?.Value);
    }

    /// <summary>
    /// Converts the input into a list of <see cref="Toden"/>s.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Looking at above sample, tokenizing is made simple when done line by line. Each line
    /// contains one or two <see cref="Token"/>s.
    /// </para>
    /// </remarks>
    /// <param name="input">Output of 'powercfg /requests'.</param>
    private static IEnumerable<Token> Tokens(string input)
    {
        // Contrary to calling input.Split('\r', '\n'), StringReader's ReadLine method does not
        // return an empty string when CR is followed by LF.
        StringReader reader = new StringReader(input);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Length == 0)
            {
                yield return new Token(TokenType.EmptyLine, "");
            }
            else if (line[line.Length - 1] == ':')
            {
                yield return new Token(TokenType.RequestType, line.Substring(0, line.Length - 1).ToString());
            }
            else if (string.Equals(line, "None.", StringComparison.InvariantCulture))
            {
                yield return new Token(TokenType.None, line);
            }
            else if (line[0] == '[')
            {
                int pos = line.IndexOf(']');
                yield return new Token(TokenType.RequesterType, line.Substring(1, pos - 1));
                yield return new Token(TokenType.RequesterName, line.Substring(pos + 2));
            }
            else
            {
                yield return new Token(TokenType.Reason, line);
            }
        }
    }

    /// <summary>
    /// Adds <see cref="Peek"/> and <see cref="TryPeek"/> to an <see cref="IEnumerable{T}"/> of
    /// <see cref="Token"/>s.
    /// </summary>
    /// <param name="tokens"></param>
    private class TokenStream(IEnumerable<Token> tokens) : IDisposable
    {
        private readonly IEnumerator<Token> tokens = tokens.GetEnumerator();
        private Token? cached;

        public Token? TryPeek() => cached ??= tokens.MoveNext() ? tokens.Current : null;

        public Token Peek() => TryPeek() ?? throw new EndOfStreamException();

        public Token Take(TokenType requestType)
        {
            Token peek = Peek();
            if (peek.TokenType == requestType)
            {
                cached = null;
                return peek;
            }
            else
            {
                throw new InvalidCastException($"Unexpected Token of type '{peek.TokenType}'. Expected type '{requestType}'.");
            }
        }

        public void Dispose() => tokens.Dispose();
    }

    private enum TokenType
    {
        EmptyLine,
        None,
        Reason,
        RequesterName,
        RequesterType,
        RequestType
    }

    private readonly struct Token(TokenType tokenType, string value)
    {
        public TokenType TokenType { get; } = tokenType;

        public string Value { get; } = value;
    }
}

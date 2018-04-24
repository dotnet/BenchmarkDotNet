using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BenchmarkDotNet.Encodings
{
    /// <summary> MultiEncoding String. </summary>
    /// <remarks> Contains different variants of string for different encoding </remarks>
    public class MultiEncodingString
    {
        private readonly Dictionary<string, string> _encodedStrings;

        
        public MultiEncodingString(string unicodePresentation, string asciiPresentation)
        {
            var encodedStrings = new[]
            {
                new KeyValuePair<Encoding, string>(Encoding.Unicode, unicodePresentation),
                new KeyValuePair<Encoding, string>(Encoding.ASCII, asciiPresentation)
            };
            
            _encodedStrings = encodedStrings.ToDictionary(_ => _.Key.EncodingName,
                                                          _ => _.Value);
        }

        public MultiEncodingString(IEnumerable<KeyValuePair<Encoding, string>> encodedStrings)
        {
            var sourceStrings = encodedStrings ?? new KeyValuePair<Encoding, string>[] { };
            
            _encodedStrings = sourceStrings.Where(kvp => kvp.Value != null)
                                           .ToDictionary(_ => _.Key.EncodingName, _ => _.Value);
        }

        
        public override string ToString() => GetString();

        public string GetString() => GetStringByEncoding(EncodingInfo.CurrentEncoding);

        private string GetStringByEncoding(Encoding encoding)
        {
            string encodedString;
            if (_encodedStrings.TryGetValue(encoding.EncodingName, out encodedString))
                return encodedString;
            
            return _encodedStrings.TryGetValue(GetFallback().EncodingName, out encodedString)
                ? encodedString
                : null;
        }

        private Encoding GetFallback() => Encoding.ASCII;
        
        
        public override bool Equals(object obj)
        {
            if (!(obj is MultiEncodingString otherMes))
                return false;

            return _encodedStrings.Count == otherMes._encodedStrings.Count
                   && _encodedStrings.All(p => otherMes._encodedStrings.ContainsKey(p.Key)
                                               && otherMes._encodedStrings[p.Key] == p.Value);
        }
        
        public override int GetHashCode()
        {
            return _encodedStrings
                .Aggregate(0, (current, encodedString) =>
                               current ^ encodedString.Key.GetHashCode() + encodedString.Value.GetHashCode());
        }
    }
}
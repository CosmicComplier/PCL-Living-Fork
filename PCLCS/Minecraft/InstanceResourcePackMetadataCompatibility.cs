using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCLCS;

// Scoped to LF ReadPackInfo metadata. Evidence: original CLR4 v5 report at
// 4eb50fbbc884fb284aac7377a897aaef4176f8d2; Newtonsoft source 0a2e291c0d9c0c7675d445703e51750363a549ef.
internal static class InstanceResourcePackMetadataCompatibility {
    internal static JObject ReadObject(string metadata) {
#if NETFRAMEWORK
        return JObject.Parse(metadata);
#else
        using var reader = new MetadataReader(metadata);
        JObject result = JObject.Load(reader);
        // Same trailing-content check as the pinned JObject.Parse. Parsing all
        // properties (including ignored/overwritten ones) precedes ref mutation.
        while (reader.Read()) { }
        return result;
#endif
    }

    internal static string? TokenText(JToken? token) {
#if !NETFRAMEWORK
        // JValue.ToString used Framework's default 15-digit general format.
        // Containers still use Newtonsoft's own invariant JSON writer, not G15.
        if (token is JValue { Value: double number } && !double.IsNaN(number) && !double.IsInfinity(number))
            return number.ToString("G15", CultureInfo.CurrentCulture);
#endif
        return token?.ToString();
    }

#if !NETFRAMEWORK
    private sealed class MetadataReader : JsonTextReader {
        private readonly string source;
        private int mappedLine = 1;
        private int lineStart;

        internal MetadataReader(string metadata) : base(new StringReader(metadata)) { source = metadata; }

        public override bool Read() {
            bool read = base.Read();
            if (!read || TokenType != JsonToken.Float || Value is not double number) return read;
            if (number == 0) {
                // Framework loses the sign for -0.0 and negative underflow.
                // Do not advance the reader's array index a second time.
                SetToken(JsonToken.Float, 0d, false);
            } else if (double.IsInfinity(number)) {
                int end = TokenEnd();
                // Pinned JsonTextReader has already validated the token. Only
                // explicit Infinity/-Infinity end in 'y'; numeric overflow does not.
                if (source[end - 1] != 'y') {
                    int start = end;
                    while (start > 0 && IsNumberCharacter(source[start - 1])) start--;
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Input string '{0}' is not a valid number. Path '{1}', line {2}, position {3}.",
                        source.Substring(start, end - start), Path, LineNumber, LinePosition);
                    throw new JsonReaderException(message, Path, LineNumber, LinePosition, null);
                }
            }
            return read;
        }

        private int TokenEnd() {
            // JsonTextReader counts CR, LF and CRLF as one line; its position is
            // a UTF16 offset after the token, independent of internal buffer shifts.
            while (mappedLine < LineNumber) {
                char value = source[lineStart++];
                if (value == '\r') {
                    if (lineStart < source.Length && source[lineStart] == '\n') lineStart++;
                    mappedLine++;
                } else if (value == '\n') mappedLine++;
            }
            return lineStart + LinePosition;
        }

        private static bool IsNumberCharacter(char value) =>
            value is >= '0' and <= '9' or '+' or '-' or '.' or 'e' or 'E';
    }
#endif
}

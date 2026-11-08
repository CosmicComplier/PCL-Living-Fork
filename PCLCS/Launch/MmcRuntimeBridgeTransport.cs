using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace PCLCS;

// Original LF host implementation; governed by the repository LICENCE.
// javac 17 --release 8 -encoding UTF-8 -g:none; shipped JavaWrapper.jar is unchanged.
public static class MmcRuntimeBridgeTransport {
    public const string EntryPoint = "pcllf.mmc.runtime.ArgvBridge";
    public const int MaximumFrameBytes = 1048576, MaximumStringBytes = 262144, MaximumArgumentCount = 4096;
    public const string ClassSha256 = "a88f60920cdac596a0380c2ea4ae49d5bfeb6795e9a032e396ccdb52f215a110";
    private const string ClassPayload = "yv66vgAAADQAzgoAAgADBwAEDAAFAAYBABBqYXZhL2xhbmcvT2JqZWN0AQAGPGluaXQ+AQADKClWCgAIAAkHAAoMAAsADAEAF2phdmEvaW8vRGF0YUlucHV0U3RyZWFtAQAHcmVhZEludAEAAygpSQcADgEAHHBjbGxmL21tYy9ydW50aW1lL0FyZ3ZCcmlkZ2UDAAQAAAoACAARDAASAAwBAAlhdmFpbGFibGUHABQBABNqYXZhL2lvL0lPRXhjZXB0aW9uCAAWAQAZSW52YWxpZCBNTUMgc3RyaW5nIGZyYW1lLgoAEwAYDAAFABkBABUoTGphdmEvbGFuZy9TdHJpbmc7KVYKAAgAGwwAHAAdAQAJcmVhZEZ1bGx5AQAFKFtCKVYJAB8AIAcAIQwAIgAjAQAhamF2YS9uaW8vY2hhcnNldC9TdGFuZGFyZENoYXJzZXRzAQAFVVRGXzgBABpMamF2YS9uaW8vY2hhcnNldC9DaGFyc2V0OwoAJQAmBwAnDAAoACkBABhqYXZhL25pby9jaGFyc2V0L0NoYXJzZXQBAApuZXdEZWNvZGVyAQAjKClMamF2YS9uaW8vY2hhcnNldC9DaGFyc2V0RGVjb2RlcjsJACsALAcALQwALgAvAQAiamF2YS9uaW8vY2hhcnNldC9Db2RpbmdFcnJvckFjdGlvbgEABlJFUE9SVAEAJExqYXZhL25pby9jaGFyc2V0L0NvZGluZ0Vycm9yQWN0aW9uOwoAMQAyBwAzDAA0ADUBAB9qYXZhL25pby9jaGFyc2V0L0NoYXJzZXREZWNvZGVyAQAQb25NYWxmb3JtZWRJbnB1dAEARyhMamF2YS9uaW8vY2hhcnNldC9Db2RpbmdFcnJvckFjdGlvbjspTGphdmEvbmlvL2NoYXJzZXQvQ2hhcnNldERlY29kZXI7CgAxADcMADgANQEAFW9uVW5tYXBwYWJsZUNoYXJhY3RlcgoAOgA7BwA8DAA9AD4BABNqYXZhL25pby9CeXRlQnVmZmVyAQAEd3JhcAEAGShbQilMamF2YS9uaW8vQnl0ZUJ1ZmZlcjsKADEAQAwAQQBCAQAGZGVjb2RlAQAsKExqYXZhL25pby9CeXRlQnVmZmVyOylMamF2YS9uaW8vQ2hhckJ1ZmZlcjsKAEQARQcARgwARwBIAQATamF2YS9uaW8vQ2hhckJ1ZmZlcgEACHRvU3RyaW5nAQAUKClMamF2YS9sYW5nL1N0cmluZzsKAEoASwcATAwATQBOAQAQamF2YS9sYW5nL1N0cmluZwEAB2luZGV4T2YBAAQoSSlJCABQAQAZSW52YWxpZCBNTUMgTlVMIGFyZ3VtZW50LgoASgBSDABTAAwBAAZsZW5ndGgDABVVWAgAVgEAGkludmFsaWQgTU1DIGFyZ3YgZW52ZWxvcGUuCgBYAFkHAFoMAFsAXAEAEGphdmEvdXRpbC9CYXNlNjQBAA1nZXRVcmxEZWNvZGVyAQAcKClMamF2YS91dGlsL0Jhc2U2NCREZWNvZGVyOwoAXgBfBwBgDABBAGEBABhqYXZhL3V0aWwvQmFzZTY0JERlY29kZXIBABYoTGphdmEvbGFuZy9TdHJpbmc7KVtCBwBjAQAiamF2YS9sYW5nL0lsbGVnYWxBcmd1bWVudEV4Y2VwdGlvbgMAEAAACABmAQAeSW52YWxpZCBNTUMgYXJndiBmcmFtZSBsZW5ndGguBwBoAQAcamF2YS9pby9CeXRlQXJyYXlJbnB1dFN0cmVhbQoAZwBqDAAFAB0KAAgAbAwABQBtAQAYKExqYXZhL2lvL0lucHV0U3RyZWFtOylWA01NQzIIAHABABdVbmtub3duIE1NQyBhcmd2IGZyYW1lLgoADQByDABzAHQBAApyZWFkU3RyaW5nAQAtKExqYXZhL2lvL0RhdGFJbnB1dFN0cmVhbTspTGphdmEvbGFuZy9TdHJpbmc7CAB2AQAScGNsbGYubW1jLnJ1bnRpbWUuCgBKAHgMAHkAegEACnN0YXJ0c1dpdGgBABUoTGphdmEvbGFuZy9TdHJpbmc7KVoIAHwBABlJbnZhbGlkIE1NQyB0YXJnZXQgY2xhc3MuCAB+AQACXC4KAEoAgAwAgQCCAQAFc3BsaXQBACgoTGphdmEvbGFuZy9TdHJpbmc7SSlbTGphdmEvbGFuZy9TdHJpbmc7CgBKAIQMAIUAhgEABmNoYXJBdAEABChJKUMKAIgAiQcAigwAiwCMAQATamF2YS9sYW5nL0NoYXJhY3RlcgEAFWlzSmF2YUlkZW50aWZpZXJTdGFydAEABChDKVoKAIgAjgwAjwCMAQAUaXNKYXZhSWRlbnRpZmllclBhcnQIAJEBABdJbnZhbGlkIE1NQyBhcmd2IGNvdW50LggAkwEAG0ludmFsaWQgTU1DIHByb3BlcnR5IGNvdW50LggAlQEAGkludmFsaWQgTU1DIHByb3BlcnR5IG5hbWUuCACXAQAXVHJhaWxpbmcgTU1DIGFyZ3YgZGF0YS4KAJkAmgcAmwwAnACdAQAQamF2YS9sYW5nL1N5c3RlbQEAC3NldFByb3BlcnR5AQA4KExqYXZhL2xhbmcvU3RyaW5nO0xqYXZhL2xhbmcvU3RyaW5nOylMamF2YS9sYW5nL1N0cmluZzsKAJ8AoAcAoQwAogCjAQAVamF2YS9sYW5nL0NsYXNzTG9hZGVyAQAUZ2V0U3lzdGVtQ2xhc3NMb2FkZXIBABkoKUxqYXZhL2xhbmcvQ2xhc3NMb2FkZXI7CgCfAKUMAKYApwEACWxvYWRDbGFzcwEAJShMamF2YS9sYW5nL1N0cmluZzspTGphdmEvbGFuZy9DbGFzczsIAKkBAARtYWluBwCrAQAPamF2YS9sYW5nL0NsYXNzBwCtAQATW0xqYXZhL2xhbmcvU3RyaW5nOwoAqgCvDACwALEBAAlnZXRNZXRob2QBAEAoTGphdmEvbGFuZy9TdHJpbmc7W0xqYXZhL2xhbmcvQ2xhc3M7KUxqYXZhL2xhbmcvcmVmbGVjdC9NZXRob2Q7CgCzALQHALUMALYAtwEAGGphdmEvbGFuZy9yZWZsZWN0L01ldGhvZAEABmludm9rZQEAOShMamF2YS9sYW5nL09iamVjdDtbTGphdmEvbGFuZy9PYmplY3Q7KUxqYXZhL2xhbmcvT2JqZWN0OwcAuQEAK2phdmEvbGFuZy9yZWZsZWN0L0ludm9jYXRpb25UYXJnZXRFeGNlcHRpb24KALgAuwwAvAC9AQAIZ2V0Q2F1c2UBABcoKUxqYXZhL2xhbmcvVGhyb3dhYmxlOwEACU1BWF9GUkFNRQEAAUkBAA1Db25zdGFudFZhbHVlAQAKTUFYX1NUUklORwEACE1BWF9BUkdTAwAAEAABAARDb2RlAQANU3RhY2tNYXBUYWJsZQcAxwEAAltCAQAKRXhjZXB0aW9ucwEAFihbTGphdmEvbGFuZy9TdHJpbmc7KVYHAMsBABNqYXZhL2xhbmcvVGhyb3dhYmxlAQAMSW5uZXJDbGFzc2VzAQAHRGVjb2RlcgAxAA0AAgAAAAMAGgC+AL8AAQDAAAAAAgBkABoAwQC/AAEAwAAAAAIADwAaAMIAvwABAMAAAAACAMMAAwACAAUABgABAMQAAAARAAEAAQAAAAUqtwABsQAAAAAACgBzAHQAAgDEAAAAfQADAAQAAABbKrYABzwbmwARGxIPowALGyq2ABCkAA27ABNZEhW3ABe/G7wITSostgAasgAetgAksgAqtgAwsgAqtgA2LLgAObYAP7YAQ04tA7YASZsADbsAE1kST7cAF78tsAAAAAEAxQAAABAAA/wAFwEJ/QA3BwDGBwBKAMgAAAAEAAEAEwAJAKkAyQACAMQAAAKBAAYACgAAAfoqvgSgAA4qAzK2AFESVKQADbsAE1kSVbcAF7+4AFcqAzK2AF1MpwAOTbsAE1kSVbcAF78rvhJkpAANuwATWRJltwAXv7sACFm7AGdZK7cAabcAa00stgAHEm6fAA27ABNZEm+3ABe/LLgAcU4ttgBRmQAWLbYAURECAKMADC0SdbYAd5kADbsAE1kSe7cAF78tEn0CtgB/OgQZBL42BQM2BhUGFQWiAFgZBBUGMjoHGQe2AFGZAA8ZBwO2AIO4AIeaAA27ABNZEnu3ABe/BDYIFQgZB7YAUaIAIBkHFQi2AIO4AI2aAA27ABNZEnu3ABe/hAgBp//chAYBp/+nLLYABzYEFQSbAAsVBBEQAKQADbsAE1kSkLcAF78VBL0ASjoFAzYGFQYVBKIAEhkFFQYsuABxU4QGAaf/7Sy2AAc2BhUGmwALFQYRBACkAA27ABNZEpK3ABe/FQa9AEo6BxUGvQBKOggDNgkVCRUGogAwGQcVCSy4AHFTGQgVCSy4AHFTGQcVCTK2AFGaAA27ABNZEpS3ABe/hAkBp//PLLYAEJkADbsAE1kSlrcAF78DNgkVCRUGogAXGQcVCTIZCBUJMrgAmFeECQGn/+i4AJ4ttgCkEqgEvQCqWQMSrFO2AK4BBL0AAlkDGQVTtgCyV6cACzoJGQm2ALq/sQACABsAJQAoAGIBywHuAfEAuAABAMUAAABlAB0RCUwHAGL8AAoHAMYQ/AAiBwAI/AAeBwBKCf4AEAcArAEB/AAhBwBKCfwAAgEg+QAF+AAF/AASAQn9AAkHAKwB+gAV/AASAQn+ABAHAKwHAKwBLfoABRD8AAIB+gAaZQcAuAcAyAAAAAQAAQDKAAEAzAAAAAoAAQBeAFgAzQAJ";
    public const string JavaSource = """
package pcllf.mmc.runtime;

import java.io.ByteArrayInputStream;
import java.io.DataInputStream;
import java.io.IOException;
import java.lang.reflect.InvocationTargetException;
import java.nio.ByteBuffer;
import java.nio.charset.CodingErrorAction;
import java.nio.charset.StandardCharsets;
import java.util.Base64;

// Original LF host transport. Repository LICENCE; not upstream Wrapper code.
public final class ArgvBridge {
    private static final int MAX_FRAME = 1048576;
    private static final int MAX_STRING = 262144;
    private static final int MAX_ARGS = 4096;
    private ArgvBridge() { }
    private static String readString(DataInputStream input) throws IOException {
        int length = input.readInt();
        if (length < 0 || length > MAX_STRING || length > input.available()) throw new IOException("Invalid MMC string frame.");
        byte[] bytes = new byte[length];
        input.readFully(bytes);
        String value = StandardCharsets.UTF_8.newDecoder().onMalformedInput(CodingErrorAction.REPORT)
            .onUnmappableCharacter(CodingErrorAction.REPORT).decode(ByteBuffer.wrap(bytes)).toString();
        if (value.indexOf(0) >= 0) throw new IOException("Invalid MMC NUL argument.");
        return value;
    }
    public static void main(String[] packet) throws Throwable {
        if (packet.length != 1 || packet[0].length() > ((MAX_FRAME + 2) / 3) * 4) throw new IOException("Invalid MMC argv envelope.");
        byte[] bytes;
        try { bytes = Base64.getUrlDecoder().decode(packet[0]); }
        catch (IllegalArgumentException invalid) { throw new IOException("Invalid MMC argv envelope."); }
        if (bytes.length > MAX_FRAME) throw new IOException("Invalid MMC argv frame length.");
        DataInputStream input = new DataInputStream(new ByteArrayInputStream(bytes));
        if (input.readInt() != 0x4d4d4332) throw new IOException("Unknown MMC argv frame.");
        String target = readString(input);
        if (target.length() == 0 || target.length() > 512 || target.startsWith("pcllf.mmc.runtime.")) throw new IOException("Invalid MMC target class.");
        for (String part : target.split("\\.", -1)) {
            if (part.length() == 0 || !Character.isJavaIdentifierStart(part.charAt(0))) throw new IOException("Invalid MMC target class.");
            for (int i = 1; i < part.length(); i++) if (!Character.isJavaIdentifierPart(part.charAt(i))) throw new IOException("Invalid MMC target class.");
        }
        int count = input.readInt();
        if (count < 0 || count > MAX_ARGS) throw new IOException("Invalid MMC argv count.");
        String[] arguments = new String[count];
        for (int i = 0; i < count; i++) arguments[i] = readString(input);
        int properties = input.readInt();
        if (properties < 0 || properties > 1024) throw new IOException("Invalid MMC property count.");
        String[] keys = new String[properties];
        String[] values = new String[properties];
        for (int i = 0; i < properties; i++) {
            keys[i] = readString(input); values[i] = readString(input);
            if (keys[i].length() == 0) throw new IOException("Invalid MMC property name.");
        }
        if (input.available() != 0) throw new IOException("Trailing MMC argv data.");
        for (int i = 0; i < properties; i++) System.setProperty(keys[i], values[i]);
        try {
            ClassLoader.getSystemClassLoader().loadClass(target).getMethod("main", String[].class).invoke(null, (Object) arguments);
        } catch (InvocationTargetException game) { throw game.getCause(); }
    }
}
""";
    public static byte[] CreateJarBytes() {
        byte[] classBytes = Convert.FromBase64String(ClassPayload);
        if (Convert.ToHexStringLower(SHA256.HashData(classBytes)) != ClassSha256) throw new InvalidOperationException("MMC bridge payload identity mismatch.");
        using MemoryStream output = new();
        using (ZipArchive archive = new(output, ZipArchiveMode.Create, true)) {
            var entry = archive.CreateEntry(EntryPoint.Replace('.', '/') + ".class", CompressionLevel.NoCompression);
            entry.LastWriteTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
            using var body = entry.Open(); body.Write(classBytes);
        }
        return output.ToArray();
    }
    public static string JarSha256() => Convert.ToHexStringLower(SHA256.HashData(CreateJarBytes()));
    // The frame includes live account values. Never persist/log/hash it as non-sensitive data.
    public static string Encode(string targetClass, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string>? restoredProperties = null) {
        ArgumentNullException.ThrowIfNull(targetClass); ArgumentNullException.ThrowIfNull(arguments);
        if (targetClass.Length is < 1 or > 512 || targetClass.StartsWith("pcllf.mmc.runtime.", StringComparison.Ordinal) || arguments.Count > MaximumArgumentCount || (restoredProperties?.Count ?? 0) > 1024)
            throw new ArgumentException("Invalid MMC runtime target/count.");
        using MemoryStream frame = new(); WriteInteger(frame, 0x4d4d4332); WriteString(frame, targetClass); WriteInteger(frame, arguments.Count);
        foreach (string argument in arguments) WriteString(frame, argument);
        WriteInteger(frame, restoredProperties?.Count ?? 0);
        if (restoredProperties is not null) foreach (var property in restoredProperties) {
            if (string.IsNullOrEmpty(property.Key)) throw new ArgumentException("Invalid MMC property name.");
            WriteString(frame, property.Key); WriteString(frame, property.Value);
        }
        return Convert.ToBase64String(frame.ToArray()).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
    private static void WriteString(Stream stream, string value) {
        if (value is null || value.Contains('\0')) throw new ArgumentException("Invalid NUL/null MMC runtime argument.");
        var utf8 = new UTF8Encoding(false, true); int length = utf8.GetByteCount(value);
        if (length > MaximumStringBytes || stream.Length > MaximumFrameBytes - 4L - length) throw new ArgumentException("MMC runtime string/frame exceeds its byte budget.");
        WriteInteger(stream, length); stream.Write(utf8.GetBytes(value));
    }
    private static void WriteInteger(Stream stream, int value) {
        Span<byte> bytes = stackalloc byte[4]; BinaryPrimitives.WriteInt32BigEndian(bytes, value); stream.Write(bytes);
    }
}


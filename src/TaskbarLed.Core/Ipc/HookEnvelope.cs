using System.Buffers;
using System.Text.Json;

namespace TaskbarLed.Core.Ipc;

/// <summary>
/// What the hook process hands to the panel: the tool's raw payload plus where and when it came from.
/// The hook does not interpret the payload, so new tool versions never require a new hook binary.
/// </summary>
public sealed record HookEnvelope(int Version, string Source, DateTimeOffset ReceivedAt, JsonElement Payload)
{
    public const int CurrentVersion = 1;

    /// <summary>Serializes an envelope around an already-encoded UTF-8 JSON payload.</summary>
    /// <exception cref="JsonException">The payload is not valid JSON.</exception>
    public static byte[] Encode(string source, DateTimeOffset receivedAt, ReadOnlySpan<byte> utf8JsonPayload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        var buffer = new ArrayBufferWriter<byte>(utf8JsonPayload.Length + 128);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteNumber("v", CurrentVersion);
            writer.WriteString("source", source);
            writer.WriteString("receivedAt", receivedAt);
            writer.WritePropertyName("payload");
            writer.WriteRawValue(utf8JsonPayload, skipInputValidation: false);
            writer.WriteEndObject();
        }

        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>Parses an envelope; returns false for anything malformed or from a newer protocol.</summary>
    public static bool TryDecode(ReadOnlySpan<byte> utf8, out HookEnvelope? envelope)
    {
        envelope = null;
        try
        {
            var reader = new Utf8JsonReader(utf8);
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("v", out var version) || !version.TryGetInt32(out var v) || v > CurrentVersion
                || !root.TryGetProperty("source", out var source) || source.GetString() is not { Length: > 0 } sourceId
                || !root.TryGetProperty("receivedAt", out var receivedAt) || !receivedAt.TryGetDateTimeOffset(out var at)
                || !root.TryGetProperty("payload", out var payload))
            {
                return false;
            }

            envelope = new HookEnvelope(v, sourceId, at, payload.Clone());
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

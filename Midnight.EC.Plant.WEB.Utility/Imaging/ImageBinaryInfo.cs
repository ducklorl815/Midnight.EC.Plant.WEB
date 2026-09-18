namespace Midnight.EC.Plant.WEB.Utility.Imaging;

/// <summary>
/// Lightweight image header probe for audit (no external image library).
/// Supports PNG IHDR and JPEG SOF markers.
/// </summary>
public static class ImageBinaryInfo
{
    public static bool TryGetDimensions(ReadOnlySpan<byte> bytes, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (bytes.Length < 24)
            return false;

        if (IsPng(bytes))
            return TryReadPng(bytes, out width, out height);

        if (IsJpeg(bytes))
            return TryReadJpeg(bytes, out width, out height);

        return false;
    }

    private static bool IsPng(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 8
        && bytes[0] == 0x89
        && bytes[1] == 0x50
        && bytes[2] == 0x4E
        && bytes[3] == 0x47
        && bytes[4] == 0x0D
        && bytes[5] == 0x0A
        && bytes[6] == 0x1A
        && bytes[7] == 0x0A;

    private static bool IsJpeg(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8;

    private static bool TryReadPng(ReadOnlySpan<byte> bytes, out int width, out int height)
    {
        width = 0;
        height = 0;
        // Signature (8) + length (4) + type "IHDR" (4) + width/height (8)
        if (bytes.Length < 24)
            return false;

        if (bytes[12] != (byte)'I' || bytes[13] != (byte)'H' || bytes[14] != (byte)'D' || bytes[15] != (byte)'R')
            return false;

        width = ReadInt32BigEndian(bytes.Slice(16, 4));
        height = ReadInt32BigEndian(bytes.Slice(20, 4));
        return width > 0 && height > 0;
    }

    private static bool TryReadJpeg(ReadOnlySpan<byte> bytes, out int width, out int height)
    {
        width = 0;
        height = 0;
        var i = 2;
        while (i + 9 < bytes.Length)
        {
            if (bytes[i] != 0xFF)
            {
                i++;
                continue;
            }

            var marker = bytes[i + 1];
            // Soften / restart / standalone markers without length
            if (marker == 0xD8 || marker == 0xD9 || (marker >= 0xD0 && marker <= 0xD7))
            {
                i += 2;
                continue;
            }

            if (i + 3 >= bytes.Length)
                return false;

            var segmentLength = (bytes[i + 2] << 8) | bytes[i + 3];
            if (segmentLength < 2 || i + 2 + segmentLength > bytes.Length)
                return false;

            // SOF0..SOF3, SOF5..SOF7, SOF9..SOF11, SOF13..SOF15
            var isSof = marker is (>= 0xC0 and <= 0xC3)
                or (>= 0xC5 and <= 0xC7)
                or (>= 0xC9 and <= 0xCB)
                or (>= 0xCD and <= 0xCF);
            if (isSof && segmentLength >= 7)
            {
                height = (bytes[i + 5] << 8) | bytes[i + 6];
                width = (bytes[i + 7] << 8) | bytes[i + 8];
                return width > 0 && height > 0;
            }

            i += 2 + segmentLength;
        }

        return false;
    }

    private static int ReadInt32BigEndian(ReadOnlySpan<byte> span) =>
        (span[0] << 24) | (span[1] << 16) | (span[2] << 8) | span[3];
}

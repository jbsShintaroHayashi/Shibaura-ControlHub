using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shibaura_ControlHub.Services;

public sealed class TfMixerClient : ITfMixerClient
{
    private const int DefaultPort = 49280;

    public async Task SendFaderAsync(string host, int channel, double linearValue, CancellationToken cancellationToken)
    {
        if (channel < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(channel), "チャンネル番号は 1 以上で指定してください。");
        }

        var clampedValue = Math.Clamp(linearValue, 0.0, 1.0);
        var address = $"/tf/mix/ch/{channel}/fader";
        var payload = BuildFloatMessage(address, (float)clampedValue);
        await SendAsync(host, payload, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendMuteAsync(string host, int channel, bool isMuted, CancellationToken cancellationToken)
    {
        if (channel < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(channel), "チャンネル番号は 1 以上で指定してください。");
        }

        var address = $"/tf/mix/ch/{channel}/on";
        var tfValue = isMuted ? 0 : 1;
        var payload = BuildIntMessage(address, tfValue);
        await SendAsync(host, payload, cancellationToken).ConfigureAwait(false);
    }

    private static async Task SendAsync(string host, byte[] payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("送信先 IP アドレスを入力してください。", nameof(host));
        }

        if (!IPAddress.TryParse(host, out var address))
        {
            throw new ArgumentException("送信先 IP アドレスの形式が正しくありません。", nameof(host));
        }

        using var udpClient = new UdpClient();
        udpClient.EnableBroadcast = false;
        var endPoint = new IPEndPoint(address, DefaultPort);
        cancellationToken.ThrowIfCancellationRequested();
        await udpClient.SendAsync(payload, payload.Length, endPoint).ConfigureAwait(false);
    }

    private static byte[] BuildFloatMessage(string oscAddress, float value)
    {
        Span<byte> data = stackalloc byte[4];
        BinaryPrimitives.WriteSingleBigEndian(data, value);
        return BuildMessage(oscAddress, ",f", data);
    }

    private static byte[] BuildIntMessage(string oscAddress, int value)
    {
        Span<byte> data = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(data, value);
        return BuildMessage(oscAddress, ",i", data);
    }

    private static byte[] BuildMessage(string oscAddress, string typeTags, ReadOnlySpan<byte> data)
    {
        var addressBytes = GetPaddedOscString(oscAddress);
        var typeTagBytes = GetPaddedOscString(typeTags);
        var payload = new byte[data.Length];
        data.CopyTo(payload);

        var message = new byte[addressBytes.Length + typeTagBytes.Length + payload.Length];
        Buffer.BlockCopy(addressBytes, 0, message, 0, addressBytes.Length);
        Buffer.BlockCopy(typeTagBytes, 0, message, addressBytes.Length, typeTagBytes.Length);
        Buffer.BlockCopy(payload, 0, message, addressBytes.Length + typeTagBytes.Length, payload.Length);

        return message;
    }

    private static byte[] GetPaddedOscString(string value)
    {
        var textBytes = Encoding.ASCII.GetBytes(value);
        var lengthWithNull = textBytes.Length + 1;
        var padding = (4 - (lengthWithNull % 4)) % 4;
        var totalLength = lengthWithNull + padding;
        var buffer = new byte[totalLength];
        Buffer.BlockCopy(textBytes, 0, buffer, 0, textBytes.Length);
        // 末尾は既定で 0 埋め
        return buffer;
    }
}


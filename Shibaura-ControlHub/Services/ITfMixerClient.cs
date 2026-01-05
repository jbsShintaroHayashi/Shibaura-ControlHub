using System.Threading;
using System.Threading.Tasks;

namespace Shibaura_ControlHub.Services;

public interface ITfMixerClient
{
    Task SendFaderAsync(string host, int channel, double linearValue, CancellationToken cancellationToken);

    Task SendMuteAsync(string host, int channel, bool isMuted, CancellationToken cancellationToken);
}


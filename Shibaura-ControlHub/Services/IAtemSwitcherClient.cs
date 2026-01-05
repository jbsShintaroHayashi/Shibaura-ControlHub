using System.Threading;
using System.Threading.Tasks;

namespace Shibaura_ControlHub.Services;

public interface IAtemSwitcherClient
{
    Task ConnectAsync(string ipAddress, CancellationToken cancellationToken);

    Task RouteAuxAsync(int auxIndex, int inputIndex, CancellationToken cancellationToken);
}


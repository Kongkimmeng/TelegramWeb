using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using System.Threading;
using System.Threading.Tasks;

public class TrackingCircuitHandler : CircuitHandler
{
    private readonly OnlineUserService _userService;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public TrackingCircuitHandler(OnlineUserService userService, AuthenticationStateProvider authenticationStateProvider)
    {
        _userService = userService;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        // Only track authenticated users
        if (user?.Identity?.IsAuthenticated == true)
        {
            _userService.Add(circuit.Id, user?.FindFirst("EmpID")?.Value ?? "Unknown");
        }
        await base.OnCircuitOpenedAsync(circuit, cancellationToken);
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _userService.Remove(circuit.Id);
        return base.OnCircuitClosedAsync(circuit, cancellationToken);
    }
}
namespace MUnique.OpenMU.Startup;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Interfaces;
using System.Threading;

/// <summary>
/// A minimal public endpoint which reports the current server status without any authentication.
/// Used by the public website container inside the docker network - the port is not exposed to the public internet.
/// </summary>
[ApiController]
[Route("api/public/status")]
[AllowAnonymous]
public class PublicStatusController : ControllerBase
{
    private readonly IDictionary<int, IGameServer> _gameServers;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublicStatusController"/> class.
    /// </summary>
    /// <param name="gameServers">The game servers.</param>
    public PublicStatusController(IDictionary<int, IGameServer> gameServers)
    {
        this._gameServers = gameServers;
    }

    /// <summary>
    /// Gets the current status of all game servers.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetStatusAsync(CancellationToken cancellationToken)
    {
        var servers = new List<object>();
        var playerNames = new List<string>();
        var totalPlayers = 0;
        foreach (var gameServer in this._gameServers.Values)
        {
            if (gameServer is not GameServer concreteServer)
            {
                continue;
            }

            var context = concreteServer.Context;
            totalPlayers += context.PlayerCount;
            await context.ForEachPlayerAsync(player =>
            {
                playerNames.Add(player.GetName());
                return Task.CompletedTask;
            }).ConfigureAwait(false);

            servers.Add(new
            {
                id = concreteServer.Id,
                description = concreteServer.Description,
                state = concreteServer.ServerState.ToString(),
                players = context.PlayerCount,
            });
        }

        return this.Ok(new
        {
            totalPlayers,
            players = playerNames,
            servers,
            utc = DateTime.UtcNow,
        });
    }
}

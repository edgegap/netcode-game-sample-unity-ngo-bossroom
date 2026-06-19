using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.BossRoom.ConnectionManagement;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class EdgegapMatchmakerClient : MonoBehaviour
{
    [SerializeField] string queueName = "bossroom";
    [SerializeField] string playerName = "Player";
    [SerializeField] float pollSeconds = 1f;
    [SerializeField] float timeoutSeconds = 120f;

    string status = "idle";
    bool busy;

    void OnGUI()
    {
        GUI.Label(new Rect(20, 20, 600, 30), $"Matchmaking: {status}");
        if (!busy && GUI.Button(new Rect(20, 50, 200, 50), "MATCHMAKE"))
            StartMatchmaking();
    }

    public async void StartMatchmaking()
    {
        busy = true;
        try
        {
            status = "init/auth";
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            var players = new List<Player> { new Player(AuthenticationService.Instance.PlayerId) };

            status = "creating ticket";
            var ticket = await MatchmakerService.Instance.CreateTicketAsync(
                players, new CreateTicketOptions(queueName));
            Debug.Log($"[MM] ticket {ticket.Id}");

            float elapsed = 0f;
            while (elapsed < timeoutSeconds)
            {
                var resp = await MatchmakerService.Instance.GetTicketAsync(ticket.Id);
                if (resp.Value is IpPortAssignment a)
                {
                    status = $"connecting {a.Ip}:{a.Port}";
                    Debug.Log($"[MM] assigned {a.Ip}:{a.Port}");
                    GameObject.Find("ConnectionManager")
                        .GetComponent<ConnectionManager>()
                        .StartClientIp(playerName, a.Ip, a.Port.Value);
                    return;
                }
                status = $"waiting... {elapsed:0}s";
                await Task.Delay(TimeSpan.FromSeconds(pollSeconds));
                elapsed += pollSeconds;
            }
            status = "TIMEOUT";
        }
        catch (Exception e) { status = "ERROR (see console)"; Debug.LogException(e); }
        finally { busy = false; }
    }
}

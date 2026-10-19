// Edgegap settings for the Boss Room sample, edited on Assets/Edgegap/Resources/EdgegapSettings.asset.
// EdgegapBootstrap reads them at startup, so Boss Room's scenes and prefabs don't need any change.
using UnityEngine;

[CreateAssetMenu(fileName = "EdgegapSettings", menuName = "Edgegap/Settings")]
public class EdgegapSettings : ScriptableObject
{
    public const string ResourcePath = "EdgegapSettings";

    [Header("Mode")]
    [Tooltip("Off (default): Boss Room's own menu connects to an IP and port, as in the original sample. "
        + "On: the main menu also shows a Find Match panel that uses the Edgegap Matchmaker.")]
    public bool UseEdgegapMatchmaker = false;

    [Header("Dedicated Server")]
    [Tooltip("UDP port the server listens on when ARBITRIUM_PORT_GAMEPORT_INTERNAL is not set (local runs). "
        + "On Edgegap the app version's internal port is used.")]
    public ushort ListenPort = 9998;

    [Tooltip("Stop the Edgegap deployment if nobody connects within this time.")]
    public float FirstPlayerTimeoutSeconds = 120f;

    [Tooltip("Stop the Edgegap deployment once all players have been gone for this long.")]
    public float EmptyServerTimeoutSeconds = 30f;

    [Header("Edgegap Matchmaker (matchmaker on)")]
    [Tooltip("Matchmaker API URL from the Edgegap dashboard.")]
    public string MatchmakerUrl = "";

    [Tooltip("Matchmaker Auth Token from the Edgegap dashboard. Safe to ship in clients: it grants no Edgegap account access.")]
    public string MatchmakerAuthToken = "";

    [Tooltip("Profile name from the matchmaker configuration.")]
    public string Profile = "casual";

    [Tooltip("Port name on the Edgegap app version.")]
    public string PortName = "gameport";

    [Tooltip("Ping Edgegap beacons and send the results in tickets. Needs a `latencies` rule in the profile.")]
    public bool UseLatencyBeacons = false;
}

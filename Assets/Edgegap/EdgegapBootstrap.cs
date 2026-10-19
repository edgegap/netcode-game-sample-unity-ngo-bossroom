// Adds the Edgegap components at startup, so Boss Room's scenes and prefabs stay untouched (easy upstream syncs).
// - Dedicated server build (-batchmode / Linux Dedicated Server): EdgegapServerStarter (MainMenu scene) starts
//   the server through Boss Room's ConnectionManager; EdgegapServerLifecycle logs the match and stops the
//   Edgegap deployment once the match is empty.
// - Client build: Boss Room's own menu connects by IP as before. When UseEdgegapMatchmaker is on, the main
//   menu also shows a Find Match panel (EdgegapMatchmakingClient).
//
// Command-line overrides (handy for scripted tests and for Edgegap's UNITY_COMMANDLINE_ARGS):
//   -edgegapListenPort <port>     server listen port when ARBITRIUM_PORT_GAMEPORT_INTERNAL is not set
//   -edgegapMatchmaker true|false use the matchmaker           -edgegapMatchmakerUrl <url>  -edgegapAuthToken <token>
//   -edgegapProfile <name>        matchmaker profile           -edgegapFindMatch            find a match on startup
//   Test flags (-edgegapConnect, -edgegapAutoReady, ...): see EdgegapTestDriver.
using System;
using UnityEngine;

public static class EdgegapBootstrap
{
    public static EdgegapSettings Settings { get; private set; }

    public static bool IsServerBuild
    {
        get
        {
#if UNITY_SERVER
            return true;
#else
            return Application.isBatchMode;
#endif
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        Settings = Resources.Load<EdgegapSettings>(EdgegapSettings.ResourcePath);
        if (Settings == null)
        {
            Debug.LogWarning("Edgegap | No Resources/EdgegapSettings asset, using defaults.");
            Settings = ScriptableObject.CreateInstance<EdgegapSettings>();
        }
        else
        {
            // Work on a copy: command-line overrides must not change the asset in the Editor.
            Settings = UnityEngine.Object.Instantiate(Settings);
        }

        ApplyCommandLine(Settings);

        if (IsServerBuild)
        {
            var go = CreateRoot();
            var server = go.AddComponent<EdgegapServerLifecycle>();
            server.FirstPlayerTimeoutSeconds = Settings.FirstPlayerTimeoutSeconds;
            server.EmptyServerTimeoutSeconds = Settings.EmptyServerTimeoutSeconds;
            return;
        }

        GameObject root = null;
        if (EdgegapTestDriver.IsRequested)
        {
            root = CreateRoot();
            root.AddComponent<EdgegapTestDriver>();
        }

        if (Settings.UseEdgegapMatchmaker)
        {
            var go = root != null ? root : CreateRoot();
            var matchmaker = go.AddComponent<EdgegapMatchmakingClient>();
            matchmaker.BaseUrl = Settings.MatchmakerUrl;
            matchmaker.AuthToken = Settings.MatchmakerAuthToken;
            matchmaker.Profile = Settings.Profile;
            matchmaker.PortName = Settings.PortName;
            matchmaker.UseLatencyBeacons = Settings.UseLatencyBeacons;
            matchmaker.FindMatchOnStart = HasFlag("-edgegapFindMatch");
        }
    }

    private static GameObject CreateRoot()
    {
        var go = new GameObject("Edgegap");
        UnityEngine.Object.DontDestroyOnLoad(go);
        return go;
    }

    private static void ApplyCommandLine(EdgegapSettings s)
    {
        if (ushort.TryParse(Arg("-edgegapListenPort"), out ushort listenPort)) s.ListenPort = listenPort;
        if (bool.TryParse(Arg("-edgegapMatchmaker"), out bool useMatchmaker)) s.UseEdgegapMatchmaker = useMatchmaker;
        if (Arg("-edgegapMatchmakerUrl") is string url) s.MatchmakerUrl = url;
        if (Arg("-edgegapAuthToken") is string token) s.MatchmakerAuthToken = token;
        if (Arg("-edgegapProfile") is string profile) s.Profile = profile;
    }

    public static bool HasFlag(string name) => Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;

    public static string Arg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}

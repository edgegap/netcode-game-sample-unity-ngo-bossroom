# Hosting Boss Room on Edgegap

This repository runs Unity's [Boss Room](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop) (Netcode for GameObjects) on [Edgegap](https://edgegap.com) dedicated servers. Every player is a client of a headless server that Edgegap runs close to them. No one hosts, and no Unity Gaming Services, Relay or Lobby are needed.

Players can join a server in two ways:

- **Direct IP (default).** Boss Room's own **Start with Direct IP → Join** connects to a server address and port, exactly like the original sample. Use it locally first, then against a deployment you start from the Edgegap dashboard.
- **Edgegap Matchmaker (optional, off by default).** The main menu shows a **Find Match** panel. The [Edgegap Matchmaker](https://docs.edgegap.com/learn/matchmaking) groups players, Edgegap starts a server near them, and every client connects to it automatically.

In both modes the server stops its own Edgegap deployment once the match is empty, so idle servers don't cost money. The game client never holds an Edgegap API token.

Tested end to end on 2026-10-19 with Unity `6000.0.54f1`, Netcode for GameObjects `1.13.0`, Unity Transport `2.5.2` (UDP) and the Edgegap Unity SDK `3.5.6` (resolved from `main` at the time, which pointed at the `3.5.6` release; `Packages/manifest.json` now pins the `3.5.6` tag). Two Windows clients played in the dungeon on the same Edgegap server, first by direct connection and then through the matchmaker (matched, connected and in Character Select within 40 s of launch). Each time, the server stopped its own deployment after the players left.

## What this adds

Everything Edgegap-specific lives in `Assets/Edgegap/` and adds itself at runtime, so Boss Room's scenes and prefabs are not modified by the matchmaker integration.

| File | Role |
|---|---|
| `Assets/Edgegap/Resources/EdgegapSettings.asset` | All settings: matchmaker on/off, local listen port, timeouts, matchmaker URL and token. |
| `Assets/Edgegap/EdgegapBootstrap.cs` | Runs before the first scene loads and adds the components below. |
| `Assets/Edgegap/EdgegapServerLifecycle.cs` | Server builds. Logs the match the matchmaker started (`N matched player(s)`) and the player count. On Edgegap, stops the deployment when nobody joins within 120 s, or 30 s after the last player leaves. |
| `Assets/Edgegap/EdgegapMatchmakingClient.cs`, `EdgegapMatchmakingDTOs.cs` | Matchmaker mode only. The Find Match panel on the main menu: creates a ticket, waits for `HOST_ASSIGNED`, then joins the server's public IP and external port through Boss Room's `ConnectionManager.StartClientIp`. |
| `Assets/Edgegap/EdgegapTestDriver.cs` | Command-line flags for scripted tests (join a server, pick a hero, take screenshots). Inactive unless a flag is passed. |
| `Assets/Edgegap/Editor/EdgegapServerBuild.cs` | **Tools → Edgegap → Build Linux Dedicated Server**, plus command-line server and client builds. |
| `Assets/Scripts/EdgegapServerStarter.cs` | Already in the sample (MainMenu scene). Starts the server with `StartHostIp("SERVER", "0.0.0.0", port)` on the `gameport` internal port; outside Edgegap it now falls back to `EdgegapSettings.ListenPort` (9998) instead of throwing, so the server runs locally and in `docker run`. |
| `Dockerfile`, `.dockerignore` | Container image for the server build. |
| `matchmaker-config.json` | Matchmaker configuration to upload in the dashboard. |
| `Packages/manifest.json` | Adds `com.edgegap.unity-sdk` (git, pinned to release tag `#3.5.6`) next to the existing Edgegap plugin. To move to a newer SDK release, change the tag, let Unity resolve it, and rerun the two-client test. |

The dedicated-server changes to Boss Room itself (server instead of host, no host payload, debug check) are listed in the [README](README.md#sample-modifications).

## Requirements

- Unity `6000.0.54f1` with the **Linux Build Support (Mono)** and **Linux Dedicated Server Build Support** modules, plus the module for your client platform.
- [Docker](https://docs.docker.com/get-docker/).
- An [Edgegap account](https://app.edgegap.com) (the free tier is enough).

The sample uses UDP, so clients are desktop builds (Windows, macOS, Linux).

## 1. Play locally (direct connection)

1. Open the project in Unity.
2. Build the server: **Tools → Edgegap → Build Linux Dedicated Server**. The output goes to `Builds/EdgegapServer/`.
3. Run it in Docker:
   ```bash
   docker build --platform linux/amd64 -t ngo-bossroom:local .
   docker run --rm -p 9998:9998/udp ngo-bossroom:local
   ```
   The log shows `Edgegap Server | Starting server on 0.0.0.0:9998.` The many `Shader ... is not supported` errors are normal for a headless build.
4. Build the client (**File → Build Profiles**, any desktop platform) and start it twice. To run two copies on one PC, give each its own profile: `Game.exe -AuthProfile p1` and `Game.exe -AuthProfile p2` (otherwise the server rejects the second one as the same player logging in again).
5. In both: **Start with Direct IP → Join**, `127.0.0.1`, port `9998`. Pick heroes, click **Ready**, and both enter the dungeon. The server logs `Edgegap Server | 2 player(s) connected.`

## 2. Play on an Edgegap server (direct connection)

1. Push the image to your Edgegap registry. Your registry project, username and token are on the dashboard's **Container Registry** page.
   ```bash
   docker login registry.edgegap.com
   docker build --platform linux/amd64 -t registry.edgegap.com/<project>/ngo-bossroom:v1 .
   docker push registry.edgegap.com/<project>/ngo-bossroom:v1
   ```
   Use a new tag for every build, never `latest`.
2. In the [dashboard](https://app.edgegap.com), create an application, then a version for that image:
   - **Port:** name `gameport`, port `7777`, protocol **UDP**. The server listens on the port Edgegap gives it in `ARBITRIUM_PORT_GAMEPORT_INTERNAL`, so the name must be `gameport`.
   - **Registry username and token:** from the Container Registry page. Without them, deployments fail to pull the image.
3. Deploy the version (**Deploy** on the version page). When it's ready, open the deployment and note its **public IP** and the external port of `gameport`.
4. In two clients: **Start with Direct IP → Join** with that IP and port. Unity Transport needs an IP address here, not the `*.pr.edgegap.net` host name.

Once both players leave, the server stops its own deployment after 30 s.

## 3. Turn on the Edgegap Matchmaker (optional)

The matchmaker is off by default. Turn it on once direct connection works:

1. In `matchmaker-config.json`, set `application.name` and `version` to your application and version.
2. Dashboard → **Matchmaker** → **Create Matchmaker** → upload the file → **Create and Start**.
3. Select `Assets/Edgegap/Resources/EdgegapSettings.asset`. Tick **Use Edgegap Matchmaker**, then paste the matchmaker's **API URL** into **Matchmaker Url** and its **Auth Token** into **Matchmaker Auth Token**. The Auth Token is meant for game clients: it only allows matchmaking calls and gives no access to your Edgegap account.
4. Rebuild the client and start it twice (with `-AuthProfile p1` / `p2` on one PC). Click **Find Match** in the panel on the left of the main menu in both. The status goes through searching → match found → joining, then both players land in Character Select on the same server.

The `casual` profile puts 2 to 8 players in a match. It fills up to 8 when it can; the expansion at 15 s lets it start with fewer (at least 2). Without an expansion stage the matchmaker only makes a partial match when the tickets expire (30 s), so keep one.

**Playing alone:** a single player is never matched. The minimum stays at 2 players in every stage (the 15 s expansion only exists so that 2 to 7 players get a match quickly), so a lone ticket keeps searching until `ticket_expiration_period` (30 s) cancels it, and the panel then shows "No match found in time, try again." To play alone, use **Start with Direct IP → Join** against a deployment, or add an expansion that lowers `min_team_size` to 1 (untested here; in Edgegap's FishNet sample such an expansion did not make a lone ticket match before its expiry).

**Known limitation:** the server does not check that a connecting player holds a matchmaker ticket for this match, so anyone who learns the server address can join. Deployments stop themselves when no one joins or everyone leaves, which limits cost. Before production, validate each player against the match's tickets (`MM_TICKET_IDS` / the matchmaker API) or an auth token. Boss Room's connection approval only rejects a full server or a duplicate player ID.

## Command-line options

These override `EdgegapSettings` in builds. They're handy for scripted tests and for Edgegap's `UNITY_COMMANDLINE_ARGS` variable.

| Option | Effect |
|---|---|
| `-edgegapMatchmaker true\|false` | Turn matchmaker mode on or off |
| `-edgegapMatchmakerUrl <url> -edgegapAuthToken <token>` | Matchmaker URL and Auth Token |
| `-edgegapProfile <name>` | Matchmaker profile |
| `-edgegapFindMatch` | Find a match as soon as the matchmaker is reachable |
| `-edgegapListenPort <port>` | Server listen port when not on Edgegap |
| `-edgegapConnect -edgegapAddress <ip> -edgegapPort <port>` | Test: join a server from the main menu |
| `-edgegapAutoReady <seat>` | Test: in Character Select, take hero seat 0-7 and lock in |
| `-edgegapScreenshots <folder>` | Test: save a screenshot every 10 s |
| `-edgegapQuitAfter <seconds>` | Test: quit after this long |

For example, two clients that find a match and start playing without any clicks:

```bash
Game.exe -screen-fullscreen 0 -AuthProfile p1 -edgegapMatchmaker true -edgegapMatchmakerUrl <url> -edgegapAuthToken <token> -edgegapFindMatch -edgegapAutoReady 0
Game.exe -screen-fullscreen 0 -AuthProfile p2 -edgegapMatchmaker true -edgegapMatchmakerUrl <url> -edgegapAuthToken <token> -edgegapFindMatch -edgegapAutoReady 1
```

## Shipping a new server build

Build the server → push a new tag → create a new app version → if you use the matchmaker: **Stop → Edit** `application.version` **→ Start**.

## Notes

- The headless server logs `NullReferenceException` in `ClientCharacter.OnAnimEvent` and `Transform child out of bounds` in `ClientPlayerAvatarNetworkAnimator.OnNetworkDespawn`. These come from Boss Room's client-side visuals running on the server; they don't affect play.
- Free tier: one deployment at a time, and the matchmaker restarts every 3 hours (its URL and token stay the same).
- Next steps for a real game: replace the IMGUI panel with a button in Boss Room's main menu (call `EdgegapMatchmakingClient.FindMatch()` / `CancelMatchmaking()` and show `Status`), check the client's ticket ID on the server (`MM_TICKET_IDS`), handle players joining a match in progress (backfill), and move to a production matchmaker tier before launch.

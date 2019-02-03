using CodeHatch.Build;
using CodeHatch.Engine.Core.Commands;
using CodeHatch.Engine.Networking;
using CodeHatch.Gaming;
using CodeHatch.Networking.Events;
using CodeHatch.Networking.Events.WorldEvents.TimeEvents;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using uMod.Libraries.Universal;
using uMod.Logging;

namespace uMod.Heat
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class HeatServer : IServer
    {
        #region Information

        /// <summary>
        /// Gets/sets the public-facing name of the server
        /// </summary>
        public string Name
        {
            get => DedicatedServerBypass.Settings.ServerName;
            set => DedicatedServerBypass.Settings.ServerName = value;
        }

        private static IPAddress address;
        private static IPAddress localAddress;

        /// <summary>
        /// Gets the public-facing IP address of the server, if known
        /// </summary>
        public IPAddress Address
        {
            get
            {
                try
                {
                    if (address == null)
                    {
                        uint ip;
                        string providerIp = DedicatedServerBypass.Settings.BindIP;
                        if (Utility.ValidateIPv4(providerIp) && !Utility.IsLocalIP(providerIp))
                        {
                            IPAddress.TryParse(providerIp, out address);
                            Interface.uMod.LogInfo($"IP address from server configuration: {address}");
                        }
                        else if ((ip = Steamworks.SteamGameServer.GetPublicIP()) > 0)
                        {
                            string publicIp = string.Concat(ip >> 24 & 255, ".", ip >> 16 & 255, ".", ip >> 8 & 255, ".", ip & 255); // TODO: uint IP address utility method
                            IPAddress.TryParse(publicIp, out address);
                            Interface.uMod.LogInfo($"IP address from Steam query: {address}");
                        }
                        else
                        {
                            WebClient webClient = new WebClient();
                            IPAddress.TryParse(webClient.DownloadString("http://api.ipify.org"), out address);
                            Interface.uMod.LogInfo($"IP address from external API: {address}");
                        }
                    }

                    return address;
                }
                catch (Exception ex)
                {
                    RemoteLogger.Exception("Couldn't get server's public IP address", ex);
                    return IPAddress.Any;
                }
            }
        }

        /// <summary>
        /// Gets the local IP address of the server, if known
        /// </summary>
        public IPAddress LocalAddress
        {
            get
            {
                try
                {
                    return localAddress ?? (localAddress = Utility.GetLocalIP());
                }
                catch (Exception ex)
                {
                    RemoteLogger.Exception("Couldn't get server's local IP address", ex);
                    return IPAddress.Any;
                }
            }
        }

        /// <summary>
        /// Gets the public-facing network port of the server, if known
        /// </summary>
        public ushort Port => GameManager.ServerData.Port;

        /// <summary>
        /// Gets the version or build number of the server
        /// </summary>
        public string Version => GameInfo.Version.ToString();

        /// <summary>
        /// Gets the network protocol version of the server
        /// </summary>
        public string Protocol => GameInfo.VersionName;

        /// <summary>
        /// Gets the language set by the server
        /// </summary>
        public CultureInfo Language => CultureInfo.InstalledUICulture;

        /// <summary>
        /// Gets the total of players currently on the server
        /// </summary>
        public int Players => Server.PlayerCount;

        /// <summary>
        /// Gets/sets the maximum players allowed on the server
        /// </summary>
        public int MaxPlayers
        {
            get => Server.PlayerLimit;
            set => Server.PlayerLimit = value;
        }

        /// <summary>
        /// Gets/sets the current in-game time on the server
        /// </summary>
        public DateTime Time
        {
            get => DateTime.Today.AddHours(GameClock.Instance.TimeOfDay);
            set => EventManager.CallEvent(new TimeSetEvent(value.Hour, GameClock.Instance.DaySpeed));
        }

        /// <summary>
        /// Gets information on the currently loaded save file
        /// </summary>
        public SaveInfo SaveInfo => null;

        #endregion Information

        #region Administration

        /// <summary>
        /// Bans the player for the specified reason and duration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string id, string reason, TimeSpan duration = default(TimeSpan))
        {
            // Check if already banned
            if (!IsBanned(id))
            {
                // Ban and kick user
                Server.Ban(ulong.Parse(id), (int)duration.TotalSeconds, reason);
            }
        }

        /// <summary>
        /// Gets the amount of time remaining on the player's ban
        /// </summary>
        /// <param name="id"></param>
        public TimeSpan BanTimeRemaining(string id)
        {
            return new DateTime(Server.GetBannedPlayerFromPlayerId(ulong.Parse(id)).ExpireDate) - DateTime.Now;
        }

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        /// <param name="id"></param>
        public bool IsBanned(string id) => Server.IdIsBanned(ulong.Parse(id));

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        public void Save() => GameManager.Save();

        /// <summary>
        /// Unbans the player
        /// </summary>
        /// <param name="id"></param>
        public void Unban(string id)
        {
            // Check if unbanned already
            if (IsBanned(id))
            {
                // Set to unbanned
                Server.Unban(ulong.Parse(id));
            }
        }

        #endregion Administration

        #region Chat and Commands

        /// <summary>
        /// Broadcasts the specified chat message and prefix to all players
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Broadcast(string message, string prefix, params object[] args)
        {
            if (!string.IsNullOrEmpty(message))
            {
                ulong avatarId = args.Length > 0 && args[0].IsSteamId() ? (ulong)args[0] : 0ul;
                message = args.Length > 0 ? string.Format(Formatter.ToRoKAnd7DTD(message), avatarId != 0ul ? args.Skip(1) : args) : Formatter.ToPlaintext(message);
                Server.BroadcastMessage(prefix != null ? $"{prefix} {message}" : message);
            }
        }

        /// <summary>
        /// Broadcasts the specified chat message to all players
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message) => Broadcast(message, null);

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            string failure = string.Empty;
            CommandManager.ExecuteCommand(Server.Instance.ServerPlayer.ID, $"/{command} {string.Join(" ", Array.ConvertAll(args, x => x.ToString()))}", ref failure);
        }

        #endregion Chat and Commands
    }
}

using CodeHatch.Common;
using CodeHatch.Engine.Chat;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.Players;
using uMod.Configuration;
using uMod.Libraries.Universal;
using uMod.Plugins;

namespace uMod.Heat
{
    /// <summary>
    /// Game hooks and wrappers for the core Heat plugin
    /// </summary>
    public partial class Heat
    {
        #region Player Hooks

        /// <summary>
        /// Called when the player is attempting to connect
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        [HookMethod("IOnUserApprove")]
        private object IOnUserApprove(Player player)
        {
            string id = player.Identifier;
            string ip = player.Connection.IpAddress; // TODO: Handle potential NullReferenceException

            // Let universal know player is joining
            Universal.PlayerManager.PlayerJoin(player.Identifier, player.Name); // TODO: Handle this automatically

            // Call universal hook
            object canLogin = Interface.Call("CanPlayerLogin", player.Name, id, ip);
            if (canLogin is string || canLogin is bool && !(bool)canLogin)
            {
                // Reject the player with message
                player.ShowPopup("Disconnected", canLogin is string ? canLogin.ToString() : "Connection was rejected"); // TODO: Localization
                player.Connection.Close(); // TODO: Handle potential NullReferenceException
                return ConnectionError.NoError;
            }

            // Let plugins know
            Interface.Call("OnPlayerApproved", player.Name, id, ip);

            return null;
        }

        /// <summary>
        /// Called when the player sends a message
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        [HookMethod("IOnPlayerChat")]
        private object IOnPlayerChat(PlayerMessageEvent evt)
        {
            // Ignore the server player
            if (evt.SenderId == 9999999999)
            {
                return null;
            }

            // Call game and universal hooks
            object chatSpecific = Interface.Call("OnPlayerChat", evt);
            object chatUniversal = Interface.Call("OnPlayerChat", evt.Sender.IPlayer, evt.Message);
            if (chatSpecific != null || chatUniversal != null)
            {
                // Cancel chat message event
                evt.SetCancelled(); // TODO: Test
                return true;
            }

            return null;
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="heatPlayer"></param>
        /// <returns></returns>
        [HookMethod("IOnPlayerConnected")]
        private void IOnPlayerConnected(Player heatPlayer)
        {
            // Ignore the server player
            if (heatPlayer.ID == 9999999999)
            {
                return;
            }

            if (permission.IsLoaded)
            {
                // Update player's stored username
                permission.UpdateNickname(heatPlayer.Identifier, heatPlayer.Name);

                // Set default groups, if necessary
                uModConfig.DefaultGroups defaultGroups = Interface.uMod.Config.Options.DefaultGroups;
                if (!permission.UserHasGroup(heatPlayer.Identifier, defaultGroups.Players))
                {
                    permission.AddUserGroup(heatPlayer.Identifier, defaultGroups.Players);
                }
                if (heatPlayer.HasPermission("admin") && !permission.UserHasGroup(heatPlayer.Identifier, defaultGroups.Administrators))
                {
                    permission.AddUserGroup(heatPlayer.Identifier, defaultGroups.Administrators);
                }
            }

            // Let universal know player connected
            Universal.PlayerManager.PlayerConnected(heatPlayer);

            // Call game-specific hook
            Interface.Call("OnPlayerConnected", heatPlayer);

            // Find universal player
            IPlayer player = Universal.PlayerManager.FindPlayerById(heatPlayer.Identifier);
            if (player != null)
            {
                // Set IPlayer object on Player
                heatPlayer.IPlayer = player;

                // Call universal hook
                Interface.Call("OnPlayerConnected", player);
            }
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="heatPlayer"></param>
        [HookMethod("IOnPlayerDisconnected")]
        private void IOnPlayerDisconnected(Player heatPlayer)
        {
            // Ignore the server player
            if (heatPlayer.ID == 9999999999)
            {
                return;
            }

            // Call game-specific hook
            Interface.Call("OnPlayerDisconnected", heatPlayer);

            // Call universal hook
            Interface.Call("OnPlayerDisconnected", heatPlayer.IPlayer, lang.GetMessage("Unknown", this, heatPlayer.IPlayer.Id));

            // Let universal know
            Universal.PlayerManager.PlayerDisconnected(heatPlayer);
        }

        /// <summary>
        /// Called when the player is banned
        /// </summary>
        /// <param name="evt"></param>
        [HookMethod("OnPlayerBanned")]
        private void OnPlayerBanned(PlayerKickEvent evt)
        {
            if (evt.Player != null)
            {
                // Call universal hook
                Interface.Call("OnPlayerBanned", evt.Player.IPlayer, evt.Reason);
            }
        }

        /// <summary>
        /// Called when the player is kicked
        /// </summary>
        /// <param name="evt"></param>
        [HookMethod("OnPlayerKicked")]
        private void OnPlayerKicked(PlayerKickEvent evt)
        {
            if (evt.Player != null)
            {
                // Call universal hook
                Interface.Call("OnPlayerKicked", evt.Player.IPlayer, evt.Reason);
            }
        }

        /// <summary>
        /// Called when the player is spawning
        /// </summary>
        /// <param name="evt"></param>
        [HookMethod("OnPlayerSpawn")]
        private void OnPlayerSpawn(PlayerFirstSpawnEvent evt)
        {
            if (evt.Player != null)
            {
                // Call universal hook
                Interface.Call("OnPlayerSpawn", evt.Player.IPlayer);
            }
        }

        /// <summary>
        /// Called when the player has spawned
        /// </summary>
        /// <param name="evt"></param>
        [HookMethod("OnPlayerSpawned")]
        private void OnPlayerSpawned(PlayerSpawnEvent evt)
        {
            if (evt.Player != null)
            {
                // Call universal hook
                Interface.Call("OnPlayerSpawned", evt.Player.IPlayer, evt.Position);
            }
        }

        /// <summary>
        /// Called when the player is respawning
        /// </summary>
        /// <param name="evt"></param>
        [HookMethod("OnPlayerRespawn")] // Not being called every time?
        private void OnPlayerRespawn(PlayerRespawnEvent evt)
        {
            if (evt.Player != null)
            {
                // Call universal hook
                Interface.Call("OnPlayerRespawn", evt.Player.IPlayer, evt.Position);
            }
        }

        #endregion Player Hooks
    }
}

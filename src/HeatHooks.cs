﻿using CodeHatch.Common;
using CodeHatch.Engine.Chat;
using CodeHatch.Engine.Core.Commands;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.Players;
using System;
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
        [HookMethod("IOnPlayerConnect")]
        private object IOnPlayerConnect(Player player)
        {
            if (player != null)
            {
                // Let universal know player is joining
                Universal.PlayerManager.PlayerJoin(player.Identifier, player.Name); // TODO: Handle this automatically

                // Call universal hook
                object canLogin = Interface.Call("CanPlayerConnect", player.Name, player.Identifier, player.Connection.IpAddress); // TODO: Handle potential NullReferenceException
                if (canLogin is string || canLogin is bool && !(bool)canLogin)
                {
                    // Reject the player with message
                    if (CodeHatch.Engine.Networking.Server.PlayerIsQueued(player.ID))
                    {
                        player.ShowPopup("Disconnected", canLogin is string ? canLogin.ToString() : "Connection was rejected"); // TODO: Localization
                        player.Connection.Close(); // TODO: Handle potential NullReferenceException
                    }
                    else
                    {
                        CodeHatch.Engine.Networking.Server.Kick(player, player.ID, player.Name, canLogin is string ? canLogin.ToString() : "Connection was rejected");
                    }
                    return ConnectionError.NoError;
                }

                // Let plugins know
                Interface.Call("OnPlayerApproved", player.Name, player.Identifier, player.Connection.IpAddress); // TODO: Handle potential NullReferenceException
            }

            return null;
        }

        /// <summary>
        /// Called when the player sends a chat message
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        [HookMethod("IOnPlayerChat")]
        private object IOnPlayerChat(PlayerMessageEvent evt)
        {
            // Ignore the server player
            if (evt.Sender.Equals(CodeHatch.Engine.Networking.Server.ServerPlayer))
            {
                return null;
            }

            // Only handle custom, global, and local chat for now
            if (evt.Message.Channel == Channel.Custom || evt.Message.Channel == Channel.Global || evt.Message.Channel == Channel.Local)
            {
                // Call game-specific and universal hooks
                object hookSpecific = Interface.Call("OnPlayerChat", evt);
                object hookUniversal = Interface.Call("OnPlayerChat", evt.Sender.IPlayer, evt.Message);
                if (hookSpecific != null || hookUniversal != null)
                {
                    // Cancel chat message event
                    evt.SetCancelled();
                    return true;
                }
            }

            // TODO: Handle Channel.Group and Channel.Whisper

            return null;
        }

        /// <summary>
        /// Called when the player runs a command
        /// </summary>
        /// <param name="evt"></param>
        [HookMethod("IOnPlayerCommand")]
        private object IOnPlayerCommand(PlayerCommandEvent evt)
        {
            // Ignore the server player
            if (evt.Sender.Equals(CodeHatch.Engine.Networking.Server.ServerPlayer))
            {
                return null;
            }

            // Call game-specific and universal hooks
            object hookSpecific = Interface.Call("OnPlayerCommand", evt);
            object hookUniversal = Interface.Call("OnPlayerCommand", evt.Sender.IPlayer, evt.Label, evt.CommandArgs);
            if (hookSpecific != null || hookUniversal != null)
            {
                // Cancel chat command event
                evt.SetCancelled();
                return true;
            }

            return null;
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        [HookMethod("IOnPlayerConnected")]
        private void IOnPlayerConnected(PlayerJoinEvent evt)
        {
            // Ignore the server player
            if (evt.Player.Equals(CodeHatch.Engine.Networking.Server.ServerPlayer)) // TODO: Handle potential NullReferenceException
            {
                return;
            }

            if (permission.IsLoaded)
            {
                // Update player's stored username
                permission.UpdateNickname(evt.Player.Identifier, evt.Player.Name);

                // Set default groups, if necessary
                uModConfig.DefaultGroups defaultGroups = Interface.uMod.Config.Options.DefaultGroups;
                if (!permission.UserHasGroup(evt.Player.Identifier, defaultGroups.Players))
                {
                    permission.AddUserGroup(evt.Player.Identifier, defaultGroups.Players);
                }
                if (evt.Player.HasPermission("admin") && !permission.UserHasGroup(evt.Player.Identifier, defaultGroups.Administrators))
                {
                    permission.AddUserGroup(evt.Player.Identifier, defaultGroups.Administrators);
                }
            }

            // Let universal know player connected
            Universal.PlayerManager.PlayerConnected(evt.Player);

            // Call game-specific hook
            Interface.Call("OnPlayerConnected", evt.Player);

            // Find universal player
            IPlayer player = Universal.PlayerManager.FindPlayerById(evt.Player.Identifier);
            if (player != null)
            {
                // Set IPlayer object on Player
                evt.Player.IPlayer = player;

                // Call universal hook
                Interface.Call("OnPlayerConnected", player);
            }
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="evt"></param>
        [HookMethod("IOnPlayerDisconnected")]
        private void IOnPlayerDisconnected(PlayerLeaveEvent evt)
        {
            // Ignore the server player
            if (evt.Player.Equals(CodeHatch.Engine.Networking.Server.ServerPlayer)) // TODO: Handle potential NullReferenceException
            {
                return;
            }

            // Call game-specific hook
            Interface.Call("OnPlayerDisconnected", evt.Player);

            // Call universal hook
            Interface.Call("OnPlayerDisconnected", evt.Player.IPlayer, lang.GetMessage("Unknown", this, evt.Player.IPlayer.Id));

            // Let universal know
            Universal.PlayerManager.PlayerDisconnected(evt.Player);
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

        #region Server Hooks

        /// <summary>
        /// Called when a command is ran on the server
        /// </summary>
        /// <param name="cmdInfo"></param>
        /// <returns></returns>
        [HookMethod("IOnServerCommand")]
        private object IOnServerCommand(CommandInfo cmdInfo)
        {
            // Call universal hook
            if (Interface.Call("OnServerCommand", cmdInfo.Label.ToLowerInvariant(), string.Join(" ", Array.ConvertAll(cmdInfo.Args, x => x.ToString()))) != null)
            {
                return true;
            }

            return null;
        }

        #endregion Server Hooks
    }
}

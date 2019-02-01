using CodeHatch.Common;
using CodeHatch.Engine.Behaviours;
using CodeHatch.Engine.Core.Commands;
using CodeHatch.Engine.Modules.Health;
using CodeHatch.Engine.Networking;
using CodeHatch.Game.Sleeping;
using CodeHatch.Networking.Events;
using System;
using System.Globalization;
using uMod.Libraries;
using uMod.Libraries.Universal;
using UnityEngine;

namespace uMod.Heat
{
    /// <summary>
    /// Represents a player, either connected or not
    /// </summary>
    public class HeatPlayer : IPlayer, IEquatable<IPlayer>
    {
        private static Permission libPerms;
        private readonly Player player;
        private readonly ulong steamId;

        internal HeatPlayer(string playerId, string name)
        {
            if (libPerms == null)
            {
                libPerms = Interface.uMod.GetLibrary<Permission>();
            }

            Name = name.Sanitize();
            steamId = ulong.Parse(playerId);
            Id = playerId;
        }

        internal HeatPlayer(Player player) : this(player.Identifier, player.Name)
        {
            this.player = player;
        }

        #region Objects

        /// <summary>
        /// Gets the object that backs the player
        /// </summary>
        public object Object => player;

        /// <summary>
        /// Gets the player's last command type
        /// </summary>
        public CommandType LastCommand { get; set; }

        #endregion Objects

        #region Information

        /// <summary>
        /// Gets/sets the name for the player
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the ID for the player (unique within the current game)
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the player's language
        /// </summary>
        public CultureInfo Language => CultureInfo.GetCultureInfo("en"); // TODO: Implement when possible

        /// <summary>
        /// Gets the player's IP address
        /// </summary>
        public string Address => player.Connection?.IpAddress ?? "0.0.0.0";

        /// <summary>
        /// Gets the player's average network ping
        /// </summary>
        public int Ping => player.Connection?.AveragePing ?? 0;

        /// <summary>
        /// Returns if the player is a server admin
        /// </summary>
        public bool IsAdmin => player?.HasPermission("admin") ?? false;

        /// <summary>
        /// Returns if the player is a server moderator
        /// </summary>
        public bool IsModerator => IsAdmin;

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        public bool IsBanned => Server.IdIsBanned(steamId);

        /// <summary>
        /// Gets if the player is connected
        /// </summary>
        public bool IsConnected => player?.Connection?.IsConnected ?? false;

        /// <summary>
        /// Returns if the player is sleeping
        /// </summary>
        public bool IsSleeping
        {
            get
            {
                ISleeper sleeper = player?.Entity?.Get<ISleeper>();
                return sleeper != null && sleeper.IsSleeping;
            }
        }

        /// <summary>
        /// Returns if the player is the server
        /// </summary>
        public bool IsServer => false;

        #endregion Information

        #region Administration

        /// <summary>
        /// Bans the player for the specified reason and duration
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string reason, TimeSpan duration = default(TimeSpan))
        {
            // Check if already banned
            if (!IsBanned)
            {
                // Ban and kick user
                Server.Ban(steamId, (int)duration.TotalSeconds, reason);
            }
        }

        /// <summary>
        /// Gets the amount of time remaining on the player's ban
        /// </summary>
        public TimeSpan BanTimeRemaining => new DateTime(Server.GetBannedPlayerFromPlayerId(steamId).ExpireDate) - DateTime.Now;

        /// <summary>
        /// Heals the player's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Heal(float amount) => player.Heal(amount);

        /// <summary>
        /// Gets/sets the player's health
        /// </summary>
        public float Health
        {
            get => player.GetHealth().CurrentHealth;
            set => player.GetHealth().CurrentHealth = value;
        }

        /// <summary>
        /// Damages the player's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Hurt(float amount)
        {
            EventManager.CallEvent(new EntityHealthChangeEvent(player.Entity, amount));
        }

        /// <summary>
        /// Kicks the player from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason) => Server.Kick(player, reason);

        /// <summary>
        /// Causes the player's character to die
        /// </summary>
        public void Kill() => player.Kill();

        /// <summary>
        /// Gets/sets the player's maximum health
        /// </summary>
        public float MaxHealth
        {
            get => player.GetHealth().MaxHealth;
            set => player.GetHealth().MaxHealth = value;
        }

        /// <summary>
        /// Renames the player to specified name <param name="name"></param>
        /// </summary>
        public void Rename(string name) => player.CurrentCharacter.ChangeName(name); // TODO: Handle potential NullReferenceException

        /// <summary>
        /// Teleports the player's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z)
        {
            Vector3 destination = player.Entity.Position; // TODO: Handle potential NullReferenceException
            player.Entity.TryTeleport(new Vector3(destination.x + 1f, destination.y + 1f, destination.z + 1f));
        }

        /// <summary>
        /// Teleports the player's character to the specified generic position
        /// </summary>
        /// <param name="pos"></param>
        public void Teleport(GenericPosition pos) => Teleport(pos.X, pos.Y, pos.Z);

        /// <summary>
        /// Unbans the player
        /// </summary>
        public void Unban()
        {
            // Check if unbanned already
            if (IsBanned)
            {
                // Set to unbanned
                Server.Unban(steamId);
            }
        }

        #endregion Administration

        #region Location

        /// <summary>
        /// Gets the position of the player
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Position(out float x, out float y, out float z)
        {
            Vector3 position = player.Entity.Position; // TODO: Handle potential NullReferenceException
            x = position.x;
            y = position.y;
            z = position.z;
        }

        /// <summary>
        /// Gets the position of the player
        /// </summary>
        /// <returns></returns>
        public GenericPosition Position()
        {
            Vector3 position = player.Entity.Position; // TODO: Handle potential NullReferenceException
            return new GenericPosition(position.x, position.y, position.z);
        }

        #endregion Location

        #region Chat and Commands

        /// <summary>
        /// Sends the specified message and prefix to the player
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Message(string message, string prefix, params object[] args)
        {
            if (!string.IsNullOrEmpty(message))
            {
                // TODO: Add universal avatar handling
                message = args.Length > 0 ? string.Format(Formatter.ToRoKAnd7DTD(message), args) : Formatter.ToRoKAnd7DTD(message);
                string formatted = prefix != null ? $"{prefix} {message}" : message;
                player.SendMessage(formatted);
            }
        }

        /// <summary>
        /// Sends the specified message to the player
        /// </summary>
        /// <param name="message"></param>
        public void Message(string message) => Message(message, null);

        /// <summary>
        /// Replies to the player with the specified message and prefix
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Reply(string message, string prefix, params object[] args) => Message(message, prefix, args);

        /// <summary>
        /// Replies to the player with the specified message
        /// </summary>
        /// <param name="message"></param>
        public void Reply(string message) => Message(message, null);

        /// <summary>
        /// Runs the specified console command on the player
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            string failure = string.Empty;
            CommandManager.ExecuteCommand(steamId, $"/{command} {string.Join(" ", Array.ConvertAll(args, x => x.ToString()))}", ref failure);
        }

        #endregion Chat and Commands

        #region Permissions

        /// <summary>
        /// Gets if the player has the specified permission
        /// </summary>
        /// <param name="perm"></param>
        /// <returns></returns>
        public bool HasPermission(string perm) => libPerms.UserHasPermission(Id, perm);

        /// <summary>
        /// Grants the specified permission on this player
        /// </summary>
        /// <param name="perm"></param>
        public void GrantPermission(string perm) => libPerms.GrantUserPermission(Id, perm, null);

        /// <summary>
        /// Strips the specified permission from this player
        /// </summary>
        /// <param name="perm"></param>
        public void RevokePermission(string perm) => libPerms.RevokeUserPermission(Id, perm);

        /// <summary>
        /// Gets if the player belongs to the specified group
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public bool BelongsToGroup(string group) => libPerms.UserHasGroup(Id, group);

        /// <summary>
        /// Adds the player to the specified group
        /// </summary>
        /// <param name="group"></param>
        public void AddToGroup(string group) => libPerms.AddUserGroup(Id, group);

        /// <summary>
        /// Removes the player from the specified group
        /// </summary>
        /// <param name="group"></param>
        public void RemoveFromGroup(string group) => libPerms.RemoveUserGroup(Id, group);

        #endregion Permissions

        #region Operator Overloads

        /// <summary>
        /// Returns if player's unique ID is equal to another player's unique ID
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IPlayer other) => Id == other?.Id;

        /// <summary>
        /// Returns if player's object is equal to another player's object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) => obj is IPlayer && Id == ((IPlayer)obj).Id;

        /// <summary>
        /// Gets the hash code of the player's unique ID
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => Id.GetHashCode();

        /// <summary>
        /// Returns a human readable string representation of this IPlayer
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"HeatPlayer[{Id}, {Name}]";

        #endregion Operator Overloads
    }
}

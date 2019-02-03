extern alias References;

using CodeHatch.Engine.Networking;
using References::ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using uMod.Libraries.Universal;

namespace uMod.Heat
{
    /// <summary>
    /// Represents a generic player manager
    /// </summary>
    public class HeatPlayerManager : IPlayerManager
    {
        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
        private struct PlayerRecord
        {
            public string Name;
            public string Id;
        }

        private IDictionary<string, PlayerRecord> playerData;
        private IDictionary<string, HeatPlayer> allPlayers;
        private IDictionary<string, HeatPlayer> connectedPlayers;
        private const string dataFileName = "umod";

        internal void Initialize()
        {
            playerData = ProtoStorage.Load<Dictionary<string, PlayerRecord>>(dataFileName) ?? new Dictionary<string, PlayerRecord>();
            allPlayers = new Dictionary<string, HeatPlayer>();
            connectedPlayers = new Dictionary<string, HeatPlayer>();

            foreach (KeyValuePair<string, PlayerRecord> pair in playerData)
            {
                allPlayers.Add(pair.Key, new HeatPlayer(pair.Value.Id, pair.Value.Name));
            }
        }

        internal void PlayerJoin(string playerId, string playerName)
        {
            if (playerData.TryGetValue(playerId, out PlayerRecord record))
            {
                record.Name = playerName;
                playerData[playerId] = record;
                allPlayers.Remove(playerId);
                allPlayers.Add(playerId, new HeatPlayer(playerId, playerName));
            }
            else
            {
                record = new PlayerRecord { Id = playerId, Name = playerName };
                playerData.Add(playerId, record);
                allPlayers.Add(playerId, new HeatPlayer(playerId, playerName));
            }
        }

        internal void PlayerConnected(Player player)
        {
            allPlayers[player.Identifier] = new HeatPlayer(player);
            connectedPlayers[player.Identifier] = new HeatPlayer(player);
        }

        internal void PlayerDisconnected(Player player) => connectedPlayers.Remove(player.Identifier);

        internal void SavePlayerData() => ProtoStorage.Save(playerData, dataFileName);

        #region Player Finding

        /// <summary>
        /// Gets all players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> All => allPlayers.Values.Cast<IPlayer>();

        /// <summary>
        /// Gets all connected players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> Connected => connectedPlayers.Values.Cast<IPlayer>();

        /// <summary>
        /// Gets all sleeping players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> Sleeping => null; // TODO: Implement if/when possible

        /// <summary>
        /// Finds a single player given unique ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IPlayer FindPlayerById(string id)
        {
            return allPlayers.TryGetValue(id, out HeatPlayer player) ? player : null;
        }

        /// <summary>
        /// Finds a single connected player given game object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public IPlayer FindPlayerByObj(object obj) => connectedPlayers.Values.FirstOrDefault(p => p.Object == obj);

        /// <summary>
        /// Finds a single player given a partial name or unique ID (case-insensitive, wildcards accepted, multiple matches returns null)
        /// </summary>
        /// <param name="partialNameOrId"></param>
        /// <returns></returns>
        public IPlayer FindPlayer(string partialNameOrId)
        {
            IPlayer[] players = FindPlayers(partialNameOrId).ToArray();
            return players.Length == 1 ? players[0] : null;
        }

        /// <summary>
        /// Finds any number of players given a partial name or unique ID (case-insensitive, wildcards accepted)
        /// </summary>
        /// <param name="partialNameOrId"></param>
        /// <returns></returns>
        public IEnumerable<IPlayer> FindPlayers(string partialNameOrId)
        {
            foreach (HeatPlayer player in allPlayers.Values)
            {
                if (player.Name != null && player.Name.IndexOf(partialNameOrId, StringComparison.OrdinalIgnoreCase) >= 0 || player.Id == partialNameOrId)
                {
                    yield return player;
                }
            }
        }

        #endregion Player Finding
    }
}

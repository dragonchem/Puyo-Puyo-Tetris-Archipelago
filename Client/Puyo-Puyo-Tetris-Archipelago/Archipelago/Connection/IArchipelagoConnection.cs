using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Archipelago.Connection
{
    public interface IArchipelagoConnection
    {
        bool Connected { get; }

        /// <summary>
        /// All the data we make available in the apworld
        /// </summary>
        IReadOnlyDictionary<string, object>? SlotData { get; }

        /// <summary>
        /// All locations active in the current archipelago session
        /// </summary>
        IReadOnlyCollection<long> AllLocations { get; }

        /// <summary>
        /// All locations we have checked
        /// </summary>
        IReadOnlyCollection<long> CheckedLocations { get; }

        /// <summary>
        /// Items we have received
        /// </summary>
        IReadOnlySet<long> ReceivedItemIds { get; }

        // Events, mostly forwards
        event EventHandler? OnConnected;
        event EventHandler? OnDisconnected;
        event EventHandler? OnItemsChanged;
        event EventHandler? OnLocationsChanged;
        event EventHandler<string>? OnMessage;
        event EventHandler<string>? OnError;

        /// <summary>
        /// Tries to connect to the archipelago session specified
        /// </summary>
        /// <returns></returns>
        Task<bool> Connect(ConnectionInfo info);

        /// <summary>
        /// Disconnects the session and clears all session data
        /// TODO: decide if we should immediately mark everything as locked until reconnected
        /// </summary>
        void Disconnect();
    }
}
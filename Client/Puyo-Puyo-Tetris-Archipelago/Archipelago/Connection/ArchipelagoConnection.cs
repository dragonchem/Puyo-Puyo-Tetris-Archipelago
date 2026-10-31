using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Archipelago.Connection
{
    public class ArchipelagoConnection : IArchipelagoConnection
    {
        private const string GameName = "Puyo Puyo Tetris";

        private ArchipelagoSession? _session;
        private readonly HashSet<long> _receivedItemIds = new HashSet<long>();

        public bool Connected => _session?.Socket.Connected ?? false;

        /// <summary>
        /// All the data we make available in the apworld
        /// </summary>
        public IReadOnlyDictionary<string, object>? SlotData { get; private set; }
        
        /// <summary>
        /// All locations active in the current archipelago session
        /// </summary>
        public IReadOnlyCollection<long> AllLocations =>
            _session?.Locations.AllLocations ?? (IReadOnlyCollection<long>)Array.Empty<long>();
        /// <summary>
        /// All locations we have checked
        /// </summary>
        public IReadOnlyCollection<long> CheckedLocations => 
            _session?.Locations.AllLocationsChecked ?? (IReadOnlyCollection<long>)Array.Empty<long>();

        /// <summary>
        /// Items we have received
        /// </summary>
        public IReadOnlyCollection<long> ReceivedItemIds =>
            _session?.Items.AllItemsReceived.Select(x => x.ItemId).ToList() ?? (IReadOnlyCollection<long>)Array.Empty<long>();
        
        // Events, mostly forwards
        public event EventHandler? OnConnected;
        public event EventHandler? OnDisconnected;
        public event EventHandler? OnItemsChanged;
        public event EventHandler? OnLocationsChanged;
        public event EventHandler<string>? OnMessage;
        public event EventHandler<string>? OnError;

        /// <summary>
        /// Tries to connect to the archipelago session specified
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Connect(ConnectionInfo info)
        {
            // Input validation
            if (string.IsNullOrEmpty(info.Host))
                throw new ArgumentException("Invalid host");
            if (info.Port == null || !int.TryParse(info.Port, out int _))
                throw new ArgumentException("Invalid port");
            if (string.IsNullOrEmpty(info.Slot))
                throw new ArgumentException("Slot is required");

            // Reset current state
            Disconnect();
            _receivedItemIds.Clear();
            SlotData = null;

            ArchipelagoSession session = ArchipelagoSessionFactory.CreateSession(info.Host, int.Parse(info.Port!));

            // Hook before login so the replay of past items / checks isn't missed.
            HookEvents(session);
            _session = session;

            // Connect and log in to slot
            await session.ConnectAsync();
            LoginResult result = await session.LoginAsync(
                GameName,
                info.Slot,
                ItemsHandlingFlags.AllItems, // Everything through events, even our local items
                version: Version.Parse("0.6.7"), // Seems to break on older versions
                tags: null,
                uuid: null,
                password: string.IsNullOrEmpty(info.Password) ? null : info.Password,
                requestSlotData: true
            );

            // Handle success
            if (result is LoginSuccessful success)
            {
                SlotData = success.SlotData;
                OnConnected?.Invoke(this, EventArgs.Empty);
                return true;
            }

            // Handle error
            if (result is LoginFailure failure)
            {
                throw new ArgumentException(string.Join(";", failure.Errors));
            }

            throw new Exception("Unexpected result.", new Exception(JsonSerializer.Serialize(result)));
        }

        /// <summary>
        /// Disconnects the session and clears all session data
        /// TODO: decide if we should immediately mark everything as locked until reconnected
        /// </summary>
        public void Disconnect()
        {
            if (_session == null) return;

            UnhookEvents(_session);
            try
            {
                if (_session.Socket.Connected)
                    _session.Socket.DisconnectAsync();
            }
            catch (Exception)
            {
                // Disconnect exception is fine to simply ignore
            }
        }

        /// <summary>
        /// Subscribes to all archipelago hooks and forwards them
        /// </summary>
        /// <param name="session"></param>
        private void HookEvents(ArchipelagoSession session)
        {
            session.Items.ItemReceived += Items_ItemReceived;
            session.Locations.CheckedLocationsUpdated += Locations_CheckedLocationsUpdated;
            session.MessageLog.OnMessageReceived += MessageLog_OnMessageReceived;
            session.Socket.SocketClosed += Socket_SocketClosed;
            session.Socket.ErrorReceived += Socket_ErrorReceived;
        }

        /// <summary>
        /// Unsubscribes from all hooks
        /// </summary>
        /// <param name="session"></param>
        private void UnhookEvents(ArchipelagoSession session)
        {
            session.Items.ItemReceived -= Items_ItemReceived;
            session.Locations.CheckedLocationsUpdated -= Locations_CheckedLocationsUpdated;
            session.MessageLog.OnMessageReceived -= MessageLog_OnMessageReceived;
            session.Socket.SocketClosed -= Socket_SocketClosed;
            session.Socket.ErrorReceived -= Socket_ErrorReceived;
        }

        /// <summary>
        /// When item is received from archipelago
        /// </summary>
        /// <param name="helper"></param>
        private void Items_ItemReceived(ReceivedItemsHelper helper)
        {
            bool changed = false;
            while (helper.Any())
            {
                ItemInfo item = helper.DequeueItem();
                if (_receivedItemIds.Add(item.ItemId))
                    changed = true;
            }
            if (changed) OnItemsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Updates checked location list
        /// </summary>
        /// <param name="newCheckedLocations"></param>
        private void Locations_CheckedLocationsUpdated(ReadOnlyCollection<long> newCheckedLocations)
        {
            if (newCheckedLocations.Count > 0)
                OnLocationsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Chat message received
        /// </summary>
        /// <param name="message"></param>
        private void MessageLog_OnMessageReceived(LogMessage message)
        {
            OnMessage?.Invoke(this, message.ToString());
        }

        /// <summary>
        /// Connection closed
        /// </summary>
        /// <param name="reason"></param>
        private void Socket_SocketClosed(string reason)
        {
            OnDisconnected?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// On exception in socket conn
        /// </summary>
        /// <param name="e"></param>
        /// <param name="message"></param>
        private void Socket_ErrorReceived(Exception e, string? message)
        {
            OnError?.Invoke(this, message ?? e.Message);
        }

        /// <summary>
        /// Marks the location as checked on the server
        /// </summary>
        /// <param name="locationIds"></param>
        public void SendLocations(IReadOnlyCollection<long> locationIds)
        {
            Task.Run(() =>
            {
                if (_session == null || locationIds.Count == 0) return Task.CompletedTask;
                return _session.Locations.CompleteLocationChecksAsync(locationIds.ToArray());
            });
        }

        /// <summary>
        /// Marks the game as completed
        /// </summary>
        public void SendGoal()
        {
            Task.Run(() =>
            {
                if (_session == null) return;
                _session.SetGoalAchieved();
            });
        }
    }
}

using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory
{
    public enum GameMode : uint
    {
        None = 0xFFFFFFFF,
        Adventure = 8,
    }

    public static class ModeReader
    {
        public static GameMode Read(IProcessMemory game)
        {
            uint raw = BitConverter.ToUInt32(game.ReadByteArray(StaticMemoryLocation.CurrentModeGlobal.Ptr(), 4), 0);
            return (GameMode)raw;
        }
        
        public static GameMode Read(IGameBridge bridge)
            => bridge.Attached && bridge.Game != null ? Read(bridge.Game) : GameMode.None;

        public static bool IsAdventure(IProcessMemory game) => Read(game) == GameMode.Adventure;
        public static bool IsAdventure(IGameBridge bridge) => Read(bridge) == GameMode.Adventure;
    }
}

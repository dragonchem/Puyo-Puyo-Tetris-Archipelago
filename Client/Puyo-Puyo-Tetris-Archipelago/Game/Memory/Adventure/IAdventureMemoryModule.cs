using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Adventure
{
    public interface IAdventureMemoryModule
    {
        AdventureStage[] Read();
        void SetGameCleared(int index);
        void SetApUnlocked(int index, bool unlocked);
        void SetStars(int index, int stars);
        void SetCursorIndex(ushort index);
        void Wipe(int index, bool unlocked);
        void RefreshMap();
    }
}

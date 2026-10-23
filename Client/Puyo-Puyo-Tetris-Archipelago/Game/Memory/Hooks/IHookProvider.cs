using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks
{
    public interface IHookProvider
    {
        List<IGameHook> Build(IProcessMemory game);
    }
}

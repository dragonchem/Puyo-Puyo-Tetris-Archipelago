using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Interfaces
{
    /// <summary>
    /// Pollable module that the GameLoop class will call
    /// </summary>
    public interface IPollable
    {
        void Poll();
    }
}

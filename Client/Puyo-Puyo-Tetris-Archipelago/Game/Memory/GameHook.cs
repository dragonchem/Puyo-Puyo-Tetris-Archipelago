using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing.IndexedProperties;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory
{
    /// <summary>
    /// Class to write hooks into the game, hooks are used for adding custom code into the game, or trigger 
    /// </summary>
    public abstract class GameHook : IGameHook
    {
        protected readonly IProcessMemory _game;
        protected readonly ILogger _logger;

        /// <summary>
        /// The original bytes in the game before we make any modifications
        /// </summary>
        protected readonly byte[]? _originalBytes;

        /// <summary>
        /// Set to true if the hook has been set, should be validated to see if still correct, use only to see if arm was even ran
        /// </summary>
        protected bool CurrentlyArmed;

        /// <summary>
        /// Length of the hook in memory
        /// </summary>
        protected uint Length;

        /// <summary>
        /// Hook's name
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Address of the hook
        /// </summary>
        public IntPtr Address { get; protected set; }

        protected GameHook(string name, IntPtr address, IProcessMemory game, ILogger logger)
        {
            _game = game;
            _logger = logger;
            Name = name;
            Address = address;
        }

        public bool IsReady()
        {
            return _originalBytes != null && AddressEquals(_originalBytes);
        }

        public HookState GetState()
        {
            // If originalbytes not known, unknown
            if (_originalBytes is null) return HookState.Unknown;

            // If armed, then armed
            if (IsArmed()) return HookState.Armed;

            // Check if ready to attach, before degraded because originalbytes are as expected (no patch applied yet)
            if (IsReady()) return HookState.Ready;


            return CurrentlyArmed ? HookState.Degraded : HookState.Unknown;
        }


        public bool ReadyToAttach()
        {
            return GetState() == HookState.Ready;
        }

        /// <summary>
        /// Returns the entire byte array of the hook
        /// </summary>
        /// <returns></returns>
        protected byte[] ReadHookLocation()
        {
            return _game.ReadByteArray(Address, Length);
        }

        /// <summary>
        /// Reads the current state of the hook location and returns whether the contents are what we expect them to be
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        protected bool AddressEquals(byte[] bytes)
        {
            byte[] current = ReadHookLocation();
            return current.AsSpan().SequenceEqual(bytes);
        }

        /// <summary>
        /// Arms the hook, if the state allows it
        /// </summary>
        public bool Arm()
        {
            // If already armed, don't re-arm
            if (IsArmed())
            {
                _logger.LogError($"Hook {Name} already armed.");
                throw new InvalidOperationException($"Hook {Name} already armed at {Address}.");
            }

            // If not ready, don't arm
            if (!IsReady()) return false;

            // Run arming function
            ArmHook();

            // Validate that arm actually landed as expected
            bool armed = IsArmed();
            if (!armed)
            {
                _logger.LogWarning($"Hook {Name} not armed succesfully.");
                return false;
            }

            // Set state and return
            CurrentlyArmed = true;
            _logger.LogInformation($"Hook {Name} succesfully armed at ${Address}.");
            return true;
        }

        /// <summary>
        /// Disarms the hook, restoring it to the vanilla state
        /// </summary>
        public void Disarm()
        {
            // Check if already vanilla
            if (IsReady())
            {
                CurrentlyArmed = false;
                _logger.LogInformation($"Hook ${Name} already vanilla, nothing to disarm");
                return;
            }

            // Check for state difference that is not our hook, just in case
            if (!IsArmed() && !CurrentlyArmed)
            {
                _logger.LogError($"Hook ${Name} not ours to disarm.");
                return;
            }

            // disarm hook
            DisarmHook();
            CurrentlyArmed = false;
            _logger.LogInformation($"Hook {Name} disarmed.");
        }

        /// <summary>
        /// Free the memory
        /// </summary>
        public void Free()
        {
            CurrentlyArmed = false;
            FreeHook();
        }

        // abstract functions the child classes need to implement
        public abstract void FreeHook();
        public abstract void DisarmHook();
        public abstract void ArmHook();
        public abstract bool IsArmed();
        public abstract void FindOriginalBytes();
    }
}

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Adventure.Adventure
{
    public class AdventureMemoryModule : GameMemoryModule
    {
        public const int StageCount = 100;
        // The offset from the Adventure store struct start to the actual stage table
        private const int TableOffset = 0xF0;

        public AdventureMemoryModule(GameBridge game, ILogger<AdventureMemoryModule> logger) : base(game, logger) { }

        /// <summary>
        /// Direct reference to the start of the stage table
        /// </summary>
        private static nint TableBase =>
            StaticMemoryLocation.AdventureStore.Ptr() + TableOffset;

        /// <summary>
        /// Get the pointer to the stage record at index i in the stage table
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private static nint RecordAddr(int i) =>
            TableBase + i * Unsafe.SizeOf<StageRecord>();

        public AdventureStage[] Read()
        {
            AdventureStage[] stages = new AdventureStage[StageCount];
            for (int i = 0; i < StageCount; i++)
            {
                StageRecord raw = ReadStruct<StageRecord>(RecordAddr(i));
                stages[i] = AdventureDecode.Decode(i, raw);
            }
            return stages;
        }

        /// <summary>
        /// Changes the cleared state of a stage to the desired value
        /// </summary>
        /// <param name="index"></param>
        /// <param name="unlocked"></param>
        public void SetCleared(int index, bool cleared)
        {
            // Decode the live stage and write back through encoder
            nint address = RecordAddr(index);
            AdventureStage stage = AdventureDecode.Decode(index, ReadStruct<StageRecord>(address));
            Write(index, stage with { Cleared = cleared });
        }

        /// <summary>
        /// Changes the stars of a stage to the desired value
        /// </summary>
        /// <param name="index"></param>
        /// <param name="unlocked"></param>
        public void SetStars(int index, int stars)
        {
            // Decode the live stage and write back through encoder
            nint address = RecordAddr(index);
            AdventureStage stage = AdventureDecode.Decode(index, ReadStruct<StageRecord>(address));
            Write(index, stage with { Stars = stars });
        }

        public void Write(int index, AdventureStage stage)
        {
            // Get stage address
            nint address = RecordAddr(index);

            // Get the existing stage record so we can keep the live Tag for the unknown bits
            StageRecord existing = ReadStruct<StageRecord>(address);

            // Encode and write
            StageRecord record = AdventureEncode.Encode(stage, existing.Tag);
            WriteStruct(address, in record);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Adventure.Adventure
{
    internal static class StageMasks
    {
        public const uint Cleared = 0b1; // bit 0 = 0000 0001 -> stage cleared
        public const uint Stars = 0b11u << 1; // bits 1-2 = 0000 0110 -> star count (0-3)
        public const int StarsShift = 1; // bits 1-2 start at bit 1 -> shift right one to read/write the value
        public const uint Visited = 0b1u << 8; // bit 8 = 1 0000 0000 -> stage visited
    }

    /// <summary>
    /// Adventure stage data
    /// </summary>
    /// <param name="Act"></param>
    /// <param name="Stage"></param>
    /// <param name="Cleared"></param>
    /// <param name="Stars"></param>
    /// <param name="Visited"></param>
    /// <param name="Score"></param>
    public readonly record struct AdventureStage(int Act, int Stage, bool Cleared, int Stars, bool Visited, uint Score);

    internal static class AdventureDecode
    {
        /// <summary>
        /// Masks the raw stage values and decodes them into the AdventureStage record
        /// </summary>
        /// <param name="i"></param>
        /// <param name="raw"></param>
        /// <returns></returns>
        public static AdventureStage Decode(int i, StageRecord raw) => new AdventureStage(
            Act: i / 10 + 1,
            Stage: i % 10 + 1,
            Cleared: (raw.Tag & StageMasks.Cleared) != 0,
            Stars: (int)(raw.Tag & StageMasks.Stars) >> StageMasks.StarsShift,
            Visited: (raw.Tag & StageMasks.Visited) != 0,
            Score: raw.Score
        );
    }

    internal static class AdventureEncode
    {
        /// <summary>
        /// Encodes the AdventureStage record back into a uint raw tag, preserving any bits we do not touch
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="rawTag"></param>
        /// <returns></returns>
        public static StageRecord Encode(AdventureStage stage, uint rawTag)
        {
            // Copy rawTag values into new uint for manipulation
            uint newTag = rawTag;

            // cleared (bit 0)
            if (stage.Cleared) newTag |= StageMasks.Cleared;   // force bit 0 on, leave every other bit unchanged
            else newTag &= ~StageMasks.Cleared;  // force bit 0 off, leave every other bit unchanged

            // stars (bit 1-2), clear first then re-set
            newTag &= ~StageMasks.Stars;
            newTag |= (uint)stage.Stars << StageMasks.StarsShift & StageMasks.Stars; // Shift left one to move 0-3 to bit 1-2, then reapply using AND mask

            return new StageRecord { Tag = newTag, Score = stage.Score };
        }
    }

    /// <summary>
    /// Raw struct for the stage record data
    /// </summary>
    internal struct StageRecord
    {
        /// <summary>
        /// bit0 = cleared, bit1-2 = stars, bit3-4 = medal? (sourced by bayoen, never seen it set), bit8 = visited
        /// </summary>
        public uint Tag;
        /// <summary>
        /// unsigned 32 bit int
        /// </summary>
        public uint Score;
    }
}

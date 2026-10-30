using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Archipelago.Translation
{
    public sealed class SlotData
    {
        public string ItemLocationVersion { get; }
        public int GoalPercentage { get; }

        public SlotData(IReadOnlyDictionary<string, object> raw)
        {
            // Convert required, slotdata sees it as generic object
            ItemLocationVersion =
                Convert.ToString(raw["item_location_version"], CultureInfo.InvariantCulture) ?? "";
            GoalPercentage =
                Convert.ToInt32(raw["goal_percentage"], CultureInfo.InvariantCulture);
        }
    }
}

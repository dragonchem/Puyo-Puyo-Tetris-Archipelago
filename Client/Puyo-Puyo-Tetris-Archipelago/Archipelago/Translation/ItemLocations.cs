using System.IO;
using System.Text.Json;

namespace Puyo_Puyo_Tetris_Archipelago.Archipelago.Translation
{
    /// <summary>
    /// Reads the item location data file and serves it in class form for the rest of the application
    /// </summary>
    public class ItemLocations
    {
        private const string FilePath = "Data/item_locations.json";

        /// <summary>
        /// Version of the data file, direct from the json
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Item to ID dict
        /// </summary>
        public Dictionary<string, long> Items { get; }

        /// <summary>
        /// Location to ID dict
        /// </summary>
        public Dictionary<string, long> Locations { get; }

        public ItemLocations()
        {
            // Validate file path exists and read string
            string json;
            try
            {
                string path = System.IO.Path.Combine(AppContext.BaseDirectory, FilePath);
                json = File.ReadAllText(path);
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException("item_locations.json not found.");
            }

            // Parse json
            JsonValues? values = JsonSerializer.Deserialize<JsonValues>(json);
            if (values == null)
                throw new InvalidDataException("item_locations.json is in an unexpected format.");

            Version = values.item_location_version;
            Locations = values.locations;
            Items = values.items;
        }

        private sealed record JsonValues(
            string item_location_version,
            Dictionary<string, long> items,
            Dictionary<string, long> locations
        );
    }
}

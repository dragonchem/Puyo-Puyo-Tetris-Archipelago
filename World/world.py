from collections.abc import Mapping
from typing import Any

from worlds.AutoWorld import World

from . import items, locations, regions, rules, web_world
from . import options as ppt_options

class PPTWorld(World):
    """
    Puyo Puyo Tetris is a puzzle game combination of the Puyo Puyo and Tetris series by SEGA.
    """

    # Metadata
    game = "Puyo Puyo Tetris"
    # web = web_world.APQuestWebWorld() TODO: Uncomment this when file is finished

    # Options YAML
    options_dataclass = ppt_options.PPTOptions
    options: ppt_options.PPTOptions

    # Load the locations from the data file
    location_name_to_id = locations.LOCATION_NAME_TO_ID
    item_name_to_id = items.ITEM_NAME_TO_ID

    # Default menu always unlocked and starting location
    origin_region_name = "Menu"

    # Create the regions and locations
    def create_regions(self) -> None:
        regions.create_and_connect_regions(self)
        locations.create_all_locations(self)

    # Create the unlock condition rules
    def set_rules(self) -> None:
        rules.set_all_rules(self)

    # Create the items
    def create_items(self) -> None:
        items.create_all_items(self)

    # Required helper function that creates an item based on the name 
    def create_item(self, name: str) -> items.PPTItem:
        return items.create_item_with_correct_classification(self, name)

    # Required filler item function
    def get_filler_item_name(self) -> str:
        return items.get_random_filler_item_name(self)

    # Set data that the client can request
    def fill_slot_data(self) -> Mapping[str, Any]:
        return {
            "item_location_version": locations.ITEM_LOCATION_VERSION,
            "goal_percentage": self.options.goal_percentage.value,
        }

from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import ItemClassification, Location
from worlds.puyo_puyo_tetris import item_locations

from . import items

if TYPE_CHECKING:
    from .world import PPTWorld

# Static
STAGE_COUNT = 100
STARS_PER_STAGE = 3

# Load locations and version from the data file
ITEM_LOCATION_VERSION: str = item_locations.VERSION
LOCATION_NAME_TO_ID: dict[str, int] = item_locations.LOCATION_NAME_TO_ID


class PPTLocation(Location):
    game = "Puyo Puyo Tetris"

# Helper function to turn the location names into a dict of location name to id
def get_location_names_with_ids(location_names: list[str]) -> dict[str, int | None]:
    return {location_name: LOCATION_NAME_TO_ID[location_name] for location_name in location_names}

# Entrance func for creating locations
def create_all_locations(world: PPTWorld) -> None:
    create_regular_locations(world)
    create_events(world)

# Static locations
def create_regular_locations(world: PPTWorld) -> None:
    adventure = world.get_region("Adventure")
    adventure.add_locations(LOCATION_NAME_TO_ID, PPTLocation)

# Unused so far, might be used in the future
def create_events(world: PPTWorld) -> None:
    pass


def stage_star_location_names() -> list[list[str]]:
    names = list(LOCATION_NAME_TO_ID)
    return [
        names[i * STARS_PER_STAGE:(i + 1) * STARS_PER_STAGE]
        for i in range(STAGE_COUNT)
    ]

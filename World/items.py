from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Item, ItemClassification
from worlds.puyo_puyo_tetris import item_locations

if TYPE_CHECKING:
    from .world import PPTWorld

# Read the item name to id map from the data file, they are all progression items
ITEM_NAME_TO_ID: dict[str, int] = item_locations.ITEM_NAME_TO_ID
FILLER_NAME = "Temp Filler"

# All the unlock names for the items
UNLOCK_NAMES: list[str] = [
    name for name in ITEM_NAME_TO_ID if name != FILLER_NAME
]


class PPTItem(Item):
    game = "Puyo Puyo Tetris"

# Required function to give back a filler item
def get_random_filler_item_name(world: PPTWorld) -> str:
    return FILLER_NAME

# Return the classification type for the items
def classification_for(name: str) -> ItemClassification:
    # Mark filler items
    if name == FILLER_NAME:
        return ItemClassification.filler

    # Everything else is progression
    return ItemClassification.progression

# Return the typed item with classification
def create_item_with_correct_classification(world: PPTWorld, name: str) -> PPTItem:
    return PPTItem(name, classification_for(name), ITEM_NAME_TO_ID[name], world.player)


# Create the list of items in the world
def create_all_items(world: PPTWorld) -> None:
    # Create our local pool of items
    pool: list[PPTItem] = []

    # Add all our enabled items to the pool
    for name in UNLOCK_NAMES:
        pool.append(create_item_with_correct_classification(world, name))

    # How many slots are unfilled
    total_locations = len(world.multiworld.get_unfilled_locations(world.player))
    remaining = total_locations - len(pool)

    # Fill the rest of the slots with filler items
    for _ in range(remaining):
        pool.append(create_item_with_correct_classification(world, FILLER_NAME))

    # Add to the multiworld without overwriting
    world.multiworld.itempool += pool
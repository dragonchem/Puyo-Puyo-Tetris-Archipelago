from __future__ import annotations

from math import ceil
from typing import TYPE_CHECKING

from rule_builder.rules import Has, HasFromList

from . import items, locations

if TYPE_CHECKING:
    from .world import PPTWorld

# Entrance func
def set_all_rules(world: PPTWorld) -> None:
    set_all_entrance_rules(world)
    set_all_location_rules(world)
    set_completion_condition(world)


# Set all entrance unlock rules
def set_all_entrance_rules(world: PPTWorld) -> None:
    # TODO: Once entrances exist in client, set rules
    pass

# Set all location unlock rules
def set_all_location_rules(world: PPTWorld) -> None:
    buckets = locations.stage_star_location_names()
    assert len(buckets) - 1 == len(items.UNLOCK_NAMES), (
        "available stages doesn't match star stage unlock requirements"
    )

    for stars, unlock in zip(buckets[1:], items.UNLOCK_NAMES):
        needs_unlock = Has(unlock)
        for location_name in stars:
            world.set_rule(world.get_location(location_name), needs_unlock)


    pass

# What determines the game being considered completed
def set_completion_condition(world: PPTWorld) -> None:
    total_stars = locations.STAGE_COUNT * locations.STARS_PER_STAGE
    stars_needed = ceil(world.options.goal_percentage.value / 100 * total_stars)
    stages_needed = ceil(stars_needed / locations.STARS_PER_STAGE)
    unlocks_needed = max(0, stages_needed - 1)

    world.set_completion_rule(HasFromList(*items.UNLOCK_NAMES, count=unlocks_needed))
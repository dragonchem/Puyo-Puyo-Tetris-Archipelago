from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Region

if TYPE_CHECKING:
    from .world import PPTWorld

# Entrance func for creating regions
def create_and_connect_regions(world: PPTWorld) -> None:
    create_all_regions(world)
    connect_regions(world)

# Create the regions
def create_all_regions(world: PPTWorld) -> None:
    menu = Region("Menu", world.player, world.multiworld)
    adventure = Region("Adventure", world.player, world.multiworld)

    world.multiworld.regions += [menu, adventure]

# Connect them together so that AP knows what order they can be accessed in
def connect_regions(world: PPTWorld) -> None:
    menu = world.get_region("Menu")
    adventure = world.get_region("Adventure")

    menu.connect(adventure, "Menu -> Adventure")


# TODO: Add the other menus, and gate the menus depending on the options picked in the yaml.
# Needs client work too for blocking off buttons on main menu / submenus
# Check APQuest for how to read if options are enabled

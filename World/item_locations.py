from __future__ import annotations

import json
import pkgutil

# Loads json data file into static variables for us to reference
def _load() -> dict:
    raw = pkgutil.get_data(__package__, "data/item_locations.json")

    # If no data, error
    if raw is None:
        raise FileNotFoundError(
            "Puyo Puyo Tetris: data/item_locations.json is missing."
        )

    # Return as dict
    return json.loads(raw.decode("utf-8"))

_data = _load()

# Cast into the static variables with correct typing
VERSION: str = _data["item_location_version"]
ITEM_NAME_TO_ID: dict[str, int] = _data["items"]
LOCATION_NAME_TO_ID: dict[str, int] = _data["locations"]

# Puyo-Puyo-Tetris-Archipelago
Archipelago implementation for Puyo Puyo Tetris

Still in very early stages

## Download
| Stable<br>Tested and Verified | Beta<br>Tested but not Verified | Dev<br>Active WIP |
| ----------------------------- | ------------------------------- | ----------------- |
| [![Tests](https://github.com/dragonchem/Puyo-Puyo-Tetris-Archipelago/actions/workflows/workflow-main.yml/badge.svg)](https://github.com/dragonchem/Puyo-Puyo-Tetris-Archipelago/actions/workflows/workflow-main.yml) | [![Tests](https://github.com/dragonchem/Puyo-Puyo-Tetris-Archipelago/actions/workflows/workflow-beta.yml/badge.svg)](https://github.com/dragonchem/Puyo-Puyo-Tetris-Archipelago/actions/workflows/workflow-beta.yml) | [![Tests](https://github.com/dragonchem/Puyo-Puyo-Tetris-Archipelago/actions/workflows/workflow-dev.yml/badge.svg)](https://github.com/dragonchem/Puyo-Puyo-Tetris-Archipelago/actions/workflows/workflow-dev.yml) |
| [download](https://github.com/dragonchem/Puyo-Puyo-Tetris-Archipelago/releases/latest) | [download](https://nightly.link/dragonchem/Puyo-Puyo-Tetris-Archipelago/workflows/workflow-beta/beta/Puyo-Puyo-Tetris-Archipelago-win-x64.zip) | [download](https://nightly.link/dragonchem/Puyo-Puyo-Tetris-Archipelago/workflows/workflow-dev/dev/Puyo-Puyo-Tetris-Archipelago-win-x64.zip) |
| [apworld]() | [apworld]() | [apworld]() |

## AI Usage Disclosure
All generative AI has been purposefully avoided in the making of the game client and apworld.
All of the assets are either custom made, or ripped from the game's sprite sheets (https://www.spriters-resource.com/pc_computer/puyopuyotetris/).

Some other programs were referenced in the making of this client, such as the credits mentioned below, or APQuest (the archipelago example world).
These may have been AI assisted, but I believe none of the current code used in the project has been touched by AI.

## Install instructions
1. Go to the version you want to download, Stable (not released yet), Beta or Dev.
2. Download the client zip, and the apworld.
3. Install the apworld into your local archipelago, open the launcher and open the options creator.
4. Set your desired options, then export the yaml.
5. Create a multiworld (https://archipelago.gg/tutorial/Archipelago/setup_en).
6. Launch both the client and Puyo Puyo Tetris (order does not matter).
7. Connect to the multiworld host, and you should be ready to go!


## Credits
Zetris: https://github.com/ZetrisAI, contains various sources that touch memory manipulation
PPT-Sandbox: https://github.com/FugEbinMeme/PPT-Sandbox, by the creator of Zetris, a tool to change memory values
Bayoen: https://github.com/bayoen, A collection of various tools made for Puyo Puyo Tetris through external memory reading
mat1jaczyyy: https://github.com/mat1jaczyyy, whose ProcessMemory project is used to interact with the actual executable of the game

These projects are instrumental for the functionality needed for archipelago, and have been directly used as sources for various parts of the project.

## Current plans
TODO

Feel free to fork / modify this, license is available [here](https://github.com/dragonchem/Puyo-Puyo-Tetris-Archipelago/blob/main/LICENSE)

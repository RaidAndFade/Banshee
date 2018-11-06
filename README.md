# Banshee
A Warcraft 3 Game emulator, inspired by GHost++, Made in C#. Pretty Simple

# Current State
- Lobby is COMPLETE (Cannot add bots, no admins, no lobby-close packet, but can do everything a normal player can do)
- Cannot provide map downloads
- Games are started with the `!start` command by any player in lobby, at any point, even if there are not enough players for a game
- Everyone has access to all commands

# Installation
`dotnet publish` in the main directory should work. If you are compiling with VSCODE then use the `build2` task to do the same thing.

# Libraries & Credit
Using `DotNetZip`, `MpqTool` and `Nito.KitchenSink.CRC` 

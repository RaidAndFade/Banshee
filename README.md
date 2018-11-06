# Banshee
A Warcraft 3 Game emulator, inspired by GHost++, Made in C#. Pretty Simple

# Current State
- HOSTBOT IS PRACTICALLY COMPLETE AND FULLY FUNCTIONAL AS A PURE HOSTBOT

Missing essential features / Solvable Issues:
- Roles (every user has access to every command)
- B.NET is not implemented nor supported (it is not entirely planned either tbh, i don't plan on investing on an extra CDKey for this bot)
- Unhosting on lan does not notify clients that the game is no longer public (UDP PKT 0x33 is not implemented)
- Client disconnects are not checked, we rely entirely on 0x21 packets, or ping timeouts. This is bad, but it is the working solution until I find a better one.

Unsolvable Issues:
- ingame: If there is only one person on your team, the bot cannot see your messages, (TALK TO /ALL INSTEAD)
This is a limitation of the WC3 Protocol, the client itself never sends messages if there is nobody else on the team
- ingame: (Related to above) If you are the only observer in a game, there is no way to use commands, since you do not have access to /ALL. *Recommended fix : use Referees (configured on line 296 of Map.cs)*

# Installation
`dotnet publish` in the main directory should work. If you are compiling with VSCODE then use the `build2` task to do the same thing.

# Libraries & Credit
Using `DotNetZip`, `MpqTool` and `Nito.KitchenSink.CRC` 

Special thanks to the makers of [ghostpp++](https://github.com/uakfdotb/ghostpp) for the work they have already done, and for posting it all as Open Source. I would have a much harder time figuring this stuff out without them.

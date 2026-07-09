# COM3D25 Exclusive Maid NTR Block Plugin

[中文说明](README.zh-CN.md)

A BepInEx plugin for COM3D2.5 that blocks supported NTR-related content for maids whose contract status is `MaidStatus.Contract.Exclusive`.

Non-exclusive maids are not affected.

## Download

Download the latest DLL from GitHub Releases:

https://github.com/leking502/COM3D25.ExclusiveMaidNTRBlock.Plugin/releases

Plugin file:

`COM3D25.ExclusiveMaidNTRBlock.Plugin.dll`

## Features

- Blocks NTR scenario events for exclusive maids
- Blocks Free Mode everyday NTR events
- Blocks Private Mode NTR events
- Blocks EmpireLife NTR content
- Blocks NTR schedule and facility tasks
- Blocks Kasizuki customers other than the master for exclusive maids
- Blocks Honeymoon NTR events
- Blocks NTR Yotogi classes, skill lists, skill selection entries, and result entries
- Does not affect non-exclusive maids

## Installation

1. Download `COM3D25.ExclusiveMaidNTRBlock.Plugin.dll` from the Releases page.
2. Put the DLL into the game's `BepInEx/plugins/` directory.
3. Start the game.

## Configuration

Press `F10` in game to show or hide the plugin configuration window.

Each supported module can be enabled or disabled separately:

- Scenario events
- Free Mode everyday events
- Private Mode events
- EmpireLife
- Schedule and facility tasks
- Kasizuki
- Honeymoon
- Yotogi class list
- Yotogi skill list
- Normal Yotogi skill selection
- Yotogi result page
- Free Yotogi skill selection

## Notes

The plugin does not modify the global NTR lock stored in the player's save data, and it does not permanently change `lockNTRPlay`.

The block is applied only when there is a clear current maid context and the current maid is exclusive.

This is an early release. The Release DLL has been verified to compile. If you find missing scenes or unsupported entries, please report them through Issues or the forum thread.

## Build From Source

The project references COM3D2.5 and BepInEx assemblies from a local game installation.

Use one of these options before building:

- Copy `Directory.Build.props.example` to `Directory.Build.props`, then set `COM3D25GameDir` to your game directory.
- Set the `COM3D25_GAME_DIR` environment variable to your game directory.

Then build:

```powershell
dotnet build .\ExclusiveMaidNTRBlock.csproj -c Release
```

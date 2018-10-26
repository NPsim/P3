## P3 - Pseudo Population Parser
### What is this?

This is my 2018 summer side project.

P3 is a program that aids mvm popfile creators in avoiding simple errors such as invalid attributes, typo'd values, and general structure.

### Why Pseudo?

This isn't the actual parser that's used in the game.

P3 emulates it to a degree.

### How do I get it?
Check the GitHub releases or find the download link on the Discord server below.

### Connecting P3 to a text editor (Notepad++)
    # Save the following command as a Run.. command
    path\to\P3.exe -pop "$(FULL_CURRENT_PATH)"
    # Example:
    C:\Users\Simple\Desktop\P3\P3.exe -pop "$(FULL_CURRENT_PATH)"
    # Optional:
    Add a Hotkey to your Run.. command to parse instantly.


### Current Features
 * Invalid Structure Scanning
   * Detect if a Key-Value is out of place. 
 * Illegal Type Scanning
 * TFBot Template Name Tracking
   * Ensure `Template` always has a valid target.
   * Supports `#base` template importing!
 * Item/Character Attribute Scanning
   * Ensure `ItemAttributes` and `CharacterAttributes` entries exist according to TF2 Source files. 
 * TFBot Item Scanning
   * Ensure `ItemAttributes` modify a valid item.
   * Ensure `Item` and `ItemName` are always correct according to TF2 Source files.
 * Configurable Warnings
   * Ensure Tank Health is always between X and Y values.
   * Ensure Tank Health is always a multiple of X value.
   * Ensure TFBot Health is always a multiple of X value.
   * Ensure Waves always drop a multiple of X value.
 * Wave Credit Calculations
   * Calculate maximum possible credits to help avoid a Buffer Overflow.
 * Template Name Scanning
   * Ensure `WaitForAllDead` and `WaitForAllSpawned` always have a valid target.
 * Show All Available Templates
 * Multi-Color Console Output
   * 32 supported colors! (Not all guaranteed to be used) 

### Special Thanks
 * what a nerd gamer
 * Remilia Scarlet

### Contacting Me
Add me on Steam: [NPOSim]

Discord: `Subsimple#9640`

Discord Server: [gRPuRdj]

[NPOSim]: <https://steamcommunity.com/id/NPOsim/>
[gRPuRdj]: <https://discord.gg/gRPuRdj>

### Bug Reports
Contact me on Steam, Discord, or just submit an Issue, whatever works.
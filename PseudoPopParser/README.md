
## What is this?
This is my 2018 summer side project; later migrated and revitalized with ANTLR as a 2019 summer side project.
P3, or PseudoPopulationParser, is a Population language parser built in ANTLR that aids MvM mission creators in error avoidance and popfile analysis.

P3 features the ability to detect both common and rare errors as well as warn the user of potentially risky implementations that the TF2 MvM Populator may reject.

## How do I get it?
Go to the [GitHub releases] or find the download link in the development Discord server below.

## System Requirements
* **Microsoft .NET Framework 4.6.1** or higher
* Optional: **Java SE Runtime Environment 8u71** or higher to use the Map Analyzer feature

## Current Features
Features are constantly being added and modified as development progresses.  
An extensive list of all features is available on the development Discord server.
#### Notable Features:
* Language Parsing
	* Color-coded console output
	* Error and warning detection with Codes (documented with examples in the [P3 Reference])
	* Detect bad syntax
	* Detect rare errors
	* Line number indicator
	* ``WaitForAllDead`` and ``WaitForAllSpawned`` coordination with WaveSpawn names
* Popfile Feedback
	* Calculate total credits dropped per wave
	* Calculate total possible credits
	* Generate a list of all custom icons used
	* View all TFBot Templates implemented
* Internal Inventory Simulator
	* Tracks every TFBot's inventory
	* Simulates every TFBot's backpack and item slots
* Map Analyzer (courtesy of [BSPSource])
	* Generate a list of all TFBot Spawns
	* Generate a list of all Logic IDs (such as map events)
	* Generate a list of all Flanking Path Tags
	* Generate a list of all Tank Path Nodes
* Item and Item/Character Attribute Database
	* Ensures items and attributes given to TFBots actually exist
	* Search through all possible items and attributes in TF2
	* Search for an item's inherent attributes with ability to see hidden attributes

[P3 Reference]:https://github.com/NPsim/P3/blob/master/PseudoPopParser/P3_Reference.pdf

## Connecting P3 to a text editor (Notepad++)
Configure P3 to parse your current WIP popfile when desired in Notepad++.

    # Save the following command as a Run.. command
    path\to\P3.exe -pop "$(FULL_CURRENT_PATH)"
	
    # Example:
    C:\Users\You\Desktop\P3\P3.exe -pop "$(FULL_CURRENT_PATH)"
	
    # Optional:
    Add a Hotkey to your Run.. command to parse instantly.

## Building P3 Yourself
All builds (as of August 2021) are completed using **Microsoft Visual Studio 2019**.  
You should be able to just open the solution with Visual Studio 2019.
If the solution fails build, try reinstalling ANTLR4 and its 2 other dependencies via nuget.
Contact me if any issues occur.
#### External Dependencies:
* ANTLR 4.6.6, Terence Parr, obtained via NuGet

## Special Thanks
 * **mc** and **Remilia** for major elements in feature development
 * **Sigsegv** for rough decompiles of TF2; found here: [mvm-reversed]
 * **ata4** and the **BSPSource team** for developing an open-sourced BSP decompiler; found here: [BSPSource]
 * **Gatebots of Potato.TF** for considering my work

[mvm-reversed]: https://github.com/sigsegv-mvm/mvm-reversed
[BSPSource]: https://github.com/ata4/bspsrc

## Contacting Me
Steam: [NPOSim]  
Discord: `sub#9640`  
P3 Development Discord Server: [4kTAn5V2E6]

[NPOSim]: <https://steamcommunity.com/id/NPOsim/>
[4kTAn5V2E6]: <https://discord.gg/4kTAn5V2E6>
[GitHub releases]:https://github.com/NPsim/P3/releases

## Bug Reports
Contact me on Steam or Discord, or just submit an Issue; whatever works.
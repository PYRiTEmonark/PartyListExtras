# Party List Extras

A Dalamud plugin to add some extra information about personal/party status effects to the party list.
Note that the information is restricted to buffs provided by players only, fight-related effects will not be added to the plugin.

For want of a better method, what the statuses actually do is explained in JSON files under `./ffxivPartyListExtras/StatusData`.
All json files are read in and converted to a `StatusEffectData` struct.
This is because the client holds no information on what the status effects actually do, just who has what.
Files are split into Job/Role, this is for readability only and isn't strictly necessary.

Note that the end goal isn't to perfectly illustrate damage, just to simplify the myriad of buffs that can be hard to parse mid-fight.

## Progress

Currently some buffs aren't applied properly. These are:
- AST: Role Related buffs from arcana
- BRD: Radiant Finale depending on job gauge
- PLD: Intervention buffed when sentinel/rampart active, Block rate on Passage of Arms
- SGE: Kerachole and Taurochole cannot be stacked
- WAR: Thrill of Battle assumes the enhanced version (WAR gets no healing up below lvl78)
- Tank: Reprisal and Arm's Length. Potential for "effective" mitigation to be calculated
- Ditto for "effective" damage for things that are currently fuzzed under "special"
- Physical Ranged - Tactician, Shield Samba and Troubadour do not stack

### Jobs

#### Tank
- DRK: Complete**
- GNB: Complete*
- PLD: Complete*
- WAR: Complete**

#### Healer
- AST: Complete*
- SCH: Complete*
- SGE: Complete*
- WHM: Complete*

#### Melee
- DRG: Complete*
- MNK: Complete*
- NIN: Complete**
- RPR: Complete*
- SAM: Complete*

#### Magical Ranged
- BLM: Complete*
- SMN: Complete*
- RDM: Complete*

#### Physical Ranged
- BRD: Complete*
- DNC: Complete*
- MCH: Complete*


#### Save-The-Queen content
- Bozja Essences: Complete**
- DRN/DRS Essences: Complete*
- Banners: Complete**
- Bozja Actions: Partially done
- DRN/DRS Actions: Complete*
- Zadnor Actions: Complete**

*Entries needing verification
**Entries needing additional work (job gauge data, character level, stack handling)

PRs/Issues very welcome.

Status effect IDs are logged as Debug, or use /plx missing to dump all seen ids to the log.
Row IDs should be identical to the ones in the excel data, however as there may be duplicates in the data in game verification is appreciated (but not required).


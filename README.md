# Party List Extras

A Dalamud plugin to add some extra information about personal/party status effects to the party list.
Note that the information is restricted to buffs provided by players only, fight-related effects will not be added to the plugin.

## Progress

For want of a better method, what the statuses actually do is explained in JSON files under `./ffxivPartyListExtras/StatusData`.
This is because the client holds no information on what the status effects actually do, just who has what.
Files are split into Job/Role, this is for readability only and isn't strictly nessacary.

Currently some buffs aren't applied properly. These are:
- PLD: Intervention buffed when sentinel/rampart active, Block rate on Passage of Arms
- AST: Role Related buffs from arcana
- BRD: Radiant Finale depending on job gauge
- SGE: Kerachole and Taurochole cannot be stacked
- Tank: Reprisal and Arm's Length. Potential for "effective" mitigation to be calculated
- Ditto for "effective" damage for things that are currently fuzzed under "special"
- Physical Ranged - Tactician, Shield Samba and Troubadour do not stack

### Jobs

#### Tank
- DRK: Complete
- GNB: Complete
- PLD: *Partial, see above
- WAR: *Stance only

#### Healer
- AST: Level 30 + *Divination - Role related buffs not implimented
- SCH: 
- SGE: Level 70 only
- WHM: 

#### Melee
- DRG: *Dragon's Eye, *Lance Charge and *Battle Litany
- MNK: *Brotherhood, *Riddle of Earth and *Riddle of Fire
- NIN:
- RPR: Crest and *Arcane Circle
- SAM: Third Eye only

#### Magical Ranged
- BLM: Complete
- SMN: 
- RDM: Complete

#### Phyiscal Ranged
- BRD: Raging Strikes, *Battle Voice and *Troubador
- DNC: Up to Level 60
- MCH: *Tacticain only

PRs/Issues very welcome.

Status effect IDs are logged as Debug, or use /plx missing to dump all seen ids to the log.
Row IDs should be identical to the ones in the excel data, however as there may be duplicates in the data in game verification is appreciated (but not required).
Entries needing verification are marked with an asterisk.

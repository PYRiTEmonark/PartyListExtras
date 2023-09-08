# ffxiv Party List Extras

Adds some extra information about personal/party status effects to the party list.
Note that the information is restricted to buffs provided by players only, fight-related effects will not be added to the plugin.

## Progress

For want of a better method, what the statuses actually do is explained in JSON files under `./ffxivPartyListExtras/StatusData`.
This is because the client holds no information on what the status effects actually do, just who has what.
Files are split into Job and Role, this is for readability only and isn't strictly nessacary.

Currently, some conditional buffs aren't applied properly. These are:
- PLD: Intervention buffed when sentinel/rampart active
- AST: Role Related buffs from arcana
- Tank: Reprisal and Arm's Length. Potential for "effective" mitigation to be calculated

### Jobs

- AST: Partial - Role related buffs not implimented
- BRD: Raging Strikes and *Troubador only
- DNC: Partial up to level 60
- DRG: *Dragon's Eye only
- DRK: Partial, up to level 60
- GNB: Complete
- MCH: *Tacticain only
- MNK: *Brotherhood only
- PLD: *Partial - Intervention missing additional effect
- RDM: Embolden only
- RPR: Crest only
- SGE: Partial
- WAR: *Stance only
- All other jobs: Nothing (yet)

PRs/Issues very welcome.
Status effect IDs are logged as Debug, or use /plx missing to dump all seen ids to the log.
Row IDs should be identical to the ones in the excel data, however as there may be duplicates in the data in game verification is appreciated (but not required).
`*` indicates that the job has been filled in but not tested in game.

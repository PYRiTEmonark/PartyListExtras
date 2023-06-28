# ffxiv Party List Extras

Adds some extra information about personal/party status effects to the party list.
Note that the information is restricted to buffs provided by players only, fight-related effects will not be added to the plugin.

## Progress

For want of a better method, what the statuses actually do is explained in JSON files under `ffxivPartyListExtras/StatusData`.
Files are split into Job and Role, this is for readability only and isn't strictly nessacary.

### Jobs

- GNB: Complete
- DRK: Partial, up to level 50
- SGE: Kardion only
- DNC: Dance Partner only
- All other jobs: Nothing (yet)

PRs for other jobs welcome.
Status effect IDs are logged, or use /plx debug to dump all seen ids to the log.
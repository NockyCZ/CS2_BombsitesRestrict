### CS2 Bombsite Restrict plugin using [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)

This plugin restricts the random bombsite of each round if there are less than X players in teams.
When a player attempts to plant the bomb on a disabled site, he will be teleported slightly upwards to cancel the bomb planting.


Configuration in
```configs/plugins/BombsiteRestrict/BombsiteRestrict.json```

|   | What it does |
| ------------- | ------------- |
| `Minimum players`  | Minimum number of players to disable random planting |
| `Disable plant message`  | Message sent at the beginning of the round if planting is disabled |
| `Warning message chat/center` | Message if a player attempts to plant the bomb on a disabled site |
| `Message type` | Where will the warning message be sent (0 - Chat , 1 - Center , 2 - Both) |

### Installation
1. Unzip into your servers `csgo/addons/counterstrikesharp/plugins/BombsiteRestrict/` dir

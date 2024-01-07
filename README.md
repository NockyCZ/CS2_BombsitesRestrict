### CS2 Bombsite Restrict plugin using [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)

This plugin restricts the bombsites of each round if there are less than X players in teams.


Configuration in
```configs/plugins/BombsiteRestrict/BombsiteRestrict.json```

|   | What it does |
| ------------- | ------------- |
| `Minimum players`  | Minimum number of players to disable random planting |
| `Disable plant message`  | Message sent at the beginning of the round if planting is disabled |
| `Warning message chat/center` | Message if a player attempts to plant the bomb on a disabled site |
| `Count bots as players` | If bots will be counted as valid players (`true` or `false`) |
| `Message type` | Where will the warning message be sent (`0` - Chat , `1` - Center , `2` - Both) |
| `Disabled site` | Which bombsite will be disabled (`0` - Random , `1` - A , `2` - B) |
| `Which team count as players` | It allows setting which team will be considered as the 'Minimum players'. (`0` - Both teams , `1` - Only CT , `2` - Only T)|

### Installation
1. Download the lastest release https://github.com/NockyCZ/CS2_BombsitesRestrict/releases
2. Unzip into your servers `csgo/addons/counterstrikesharp/plugins/BombsiteRestrict/` dir

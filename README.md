### CS2 Bombsite Restrict plugin using [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)

### Discord Support Server
![Discord Banner 2](https://discordapp.com/api/guilds/1149315368465211493/widget.png?style=banner2)

## 

This plugin restricts the bombsites of each round if there are less than X players in teams.

Configuration in
```configs/plugins/BombsiteRestrict/BombsiteRestrict.json```

|   | What it does |
| ------------- | ------------- |
| `Minimum players`  | Minimum number of players to disable random planting |
| `Count bots as players` | If bots will be counted as valid players (`true` or `false`) |
| `Disabled site` | Which bombsite will be disabled (`0` - Random , `1` - A , `2` - B) |
| `Which team count as players` | It allows setting which team will be considered as the 'Minimum players'. (`0` - Both teams , `1` - Only CT , `2` - Only T)|
| `Send plant restrict message to team` | Which team gets a message when bombsite is disabled (`0` - Both teams , `1` - Only CT , `2` - Only T)|
| `Center message timer` | Customize how many seconds at the round start, will be center message displayed. To disabled center message, set this option to `0`. |

## Command
|  Name | Allowed Values | Additional info  |
| ------------- | ------------- | ------------- |
| `css_restrictbombsite` | <1 / 2> | `1 - A` , `2 - B` Can be executed only from server console |


### Installation
1. Download the lastest release https://github.com/NockyCZ/CS2_BombsitesRestrict/releases
2. Unzip into your servers `csgo/addons/counterstrikesharp/plugins/` dir
3. Restart the server

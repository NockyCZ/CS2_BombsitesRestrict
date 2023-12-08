using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json.Serialization;

namespace BombsiteRestrict;

public class GenerateConfig : BasePluginConfig
{
    [JsonPropertyName("Minimum players")] public int iMinPlayers { get; set; } = 6;
    [JsonPropertyName("Disable plant message")] public string szDisablePlant { get; set; } = "In this round, the bomb can only be planted on {SITE}";
    [JsonPropertyName("Warning message chat")] public string szWarnMsgChat { get; set; } = "You can't plant the bomb on this site!";
    [JsonPropertyName("Warning message center")] public string szWarnMsgCenter { get; set; } = "You can't plant the bomb on this site!";
    [JsonPropertyName("Count bots as players")] public bool bCountBots { get; set; } = false;
    [JsonPropertyName("Message type")] public int iWarnMsgType { get; set; } = 2;
    [JsonPropertyName("Disabled site")] public int iDisabledSite { get; set; } = 0;
    [JsonPropertyName("Which team count as players")] public int iTeam { get; set; } = 0;
}

public class BombsiteRestrict : BasePlugin, IPluginConfig<GenerateConfig>
{
    public override string ModuleName => "Bombsites Restrict";
    public override string ModuleAuthor => "Nocky";
    public override string ModuleVersion => "1.0.4";
    private static float g_fBombsiteA;
    private static float g_fBombsiteB;
    private static int g_iDisabledPlantPosition;
    private static int g_iDisabledSite;
    private static bool g_bPluginDisabled;

    public GenerateConfig Config{ get; set; } = null!;
    public void OnConfigParsed(GenerateConfig config){ 
        Config = config; 
    }

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMapStart>(mapName =>{
            Server.NextFrame(() =>{
                var Bombsites = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("func_bomb_target");
                if(Bombsites.Count() != 2){
                    g_bPluginDisabled = true;
                    Console.WriteLine("[Bombsites] The Bombsite Restrict is disabled, because there are no bomb plants on this map.");
                }
                else{
                    g_bPluginDisabled = false;
                }
            });
        });
    }

    [GameEventHandler(HookMode.Post)]
    public HookResult EventEnterBombzone(EventEnterBombzone @event, GameEventInfo info){
        if (g_bPluginDisabled)
            return HookResult.Continue; 

        if (g_iDisabledPlantPosition != 0 && @event.Hasbomb){
            CCSPlayerController player = @event.Userid;
            var position = player.Pawn.Value.AbsOrigin!;
            int iPosType = 0;
            if(IsMapNuke()){
                iPosType = 2;
            }
            int iSite = GetNearestBombsite((int)position[iPosType]);
            if(iSite == g_iDisabledSite && iSite != 0){
                Server.NextFrame(() =>{
                    player.PrintToCenter($"{Config.szWarnMsgCenter}");
                });
            }
        }
        return HookResult.Continue; 
    }
    [GameEventHandler(HookMode.Pre)]
    public HookResult EventBombBeginplant(EventBombBeginplant @event, GameEventInfo info){
        if (g_bPluginDisabled)
            return HookResult.Continue; 
        if (g_iDisabledPlantPosition != 0){
            var site = new CBombTarget(NativeAPI.GetEntityFromIndex(@event.Site));
            int iSiteType;
            if (site.IsBombSiteB){
                iSiteType = 2;
            }
            else{
                iSiteType = 1;
            }
            if (g_iDisabledSite == iSiteType){
                CCSPlayerController player = @event.Userid;
                Server.NextFrame(() =>{
                    player.DropActiveWeapon();
                    if (Config.iWarnMsgType == 0 || Config.iWarnMsgType == 2)
                        player.PrintToChat($" {ChatColors.Red}[Bombsite] {ChatColors.Default}{Config.szWarnMsgChat}");
                    if (Config.iWarnMsgType == 1 || Config.iWarnMsgType == 2)
                        player.PrintToCenter($"{Config.szWarnMsgCenter}");
                });
            }
        }
        return HookResult.Continue; 
    }
    [GameEventHandler(HookMode.Pre)]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info){
        if (g_bPluginDisabled)
            return HookResult.Continue; 
        if (!GameRules().WarmupPeriod){
            if (GetPlayersCount() <= Config.iMinPlayers){
                SetupMapBombsites();
                int iSite = Config.iDisabledSite;
                if (Config.iDisabledSite == 0){
                    Random random = new Random();
                    iSite = random.Next(1, 3);
                }
                DisableRandomPlant(iSite);
                g_iDisabledSite = iSite;
            }
            else{
                g_iDisabledPlantPosition = 0;
            }
        }
        return HookResult.Continue; 
    }
    public void DisableRandomPlant(int site){
        switch(site){
            case 1:
            {
                g_iDisabledPlantPosition = (int)g_fBombsiteA;
                string szMSG = $"{ChatColors.Red}[Bombsite] {ChatColors.Default}{Config.szDisablePlant}";
                szMSG = szMSG.Replace("{SITE}", $" {ChatColors.Darkred}B{ChatColors.Default}");
                Server.PrintToChatAll($" {szMSG}");
                break;
            }
            case 2:
            {
                g_iDisabledPlantPosition = (int)g_fBombsiteB;
                string szMSG = $"{ChatColors.Red}[Bombsite] {ChatColors.Default}{Config.szDisablePlant}";
                szMSG = szMSG.Replace("{SITE}", $" {ChatColors.Darkred}A{ChatColors.Default}");
                Server.PrintToChatAll($" {szMSG}");
                break;
            }
        }
    }
    public void SetupMapBombsites(){
        if(IsMapNuke()){
            g_fBombsiteB = -700;
            g_fBombsiteA = -350;
        }
        else{
            var index = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("cs_player_manager");
            foreach (var pos in index){
                g_fBombsiteA = Schema.GetSchemaValue<float>(pos.Handle, "CCSPlayerResource", "m_bombsiteCenterA");
                g_fBombsiteB = Schema.GetSchemaValue<float>(pos.Handle, "CCSPlayerResource", "m_bombsiteCenterB");
            }
        }
    }
    private static bool IsMapNuke(){
        if(Server.MapName.Contains("nuke"))
            return true;
        return false;
    }
    private int GetNearestBombsite(int pos){
        int DistanceA = Math.Abs(pos - (int)g_fBombsiteA);
        int DistanceB = Math.Abs(pos - (int)g_fBombsiteB);
        if (DistanceA < DistanceB){
            return 1;
        }
        else if (DistanceA > DistanceB){
            return 2;
        }
        return 0;
    }
    private int GetPlayersCount(){
        int iCount = 0;
        Utilities.GetPlayers().ForEach(player =>{
            if (Config.iTeam == 1 && (CsTeam)player.TeamNum == CsTeam.CounterTerrorist){
                if(!Config.bCountBots){
                    if(!player.IsBot)
                        iCount++;
                }
                else
                    iCount++;
            }
            else if (Config.iTeam == 2 && (CsTeam)player.TeamNum == CsTeam.Terrorist){
                if(!Config.bCountBots){
                    if(!player.IsBot)
                        iCount++;
                }
                else
                    iCount++;
            }
            else if(Config.iTeam == 0 && (CsTeam)player.TeamNum == CsTeam.Terrorist || (CsTeam)player.TeamNum == CsTeam.CounterTerrorist){
                if(!Config.bCountBots){
                    if(!player.IsBot)
                        iCount++;
                }
                else
                    iCount++;
            }
        });
        return iCount;
    }
    internal static CCSGameRules GameRules(){
        return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
    }
}

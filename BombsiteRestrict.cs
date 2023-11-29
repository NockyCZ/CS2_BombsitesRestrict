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
    [JsonPropertyName("Message type")] public int iWarnMsgType { get; set; } = 2;
}

public class BombsiteRestrict : BasePlugin, IPluginConfig<GenerateConfig>
{
    public override string ModuleName => "Bombsites Restrict";
    public override string ModuleAuthor => "Nocky";
    public override string ModuleVersion => "1.0.0";
    private static float g_fBombisteA;
    private static float g_fBombisteB;
    private static int g_iDisabledPlantPosition;

    public GenerateConfig Config{ get; set; } = null!;
    public void OnConfigParsed(GenerateConfig config){ 
        Config = config; 
    }
    
    [GameEventHandler(HookMode.Post)]
    public HookResult EventEnterBombzone(EventEnterBombzone @event, GameEventInfo info)
    {
        if (g_iDisabledPlantPosition != 0 && @event.Hasbomb){
            CCSPlayerController player = @event.Userid;
            var position = player.PlayerPawn.Value.AbsOrigin!;
            int iPosition = (int)position[0];
            int minPosition = g_iDisabledPlantPosition - 400;
            int maxPosition = g_iDisabledPlantPosition + 400;
            if(iPosition <= maxPosition && iPosition >= minPosition){
                Server.NextFrame(() =>{
                    player.PrintToCenter($"{Config.szWarnMsgCenter}");
                });
            }
        }
        return HookResult.Continue; 
    }
    [GameEventHandler(HookMode.Pre)]
    public HookResult EventBombBeginplant(EventBombBeginplant @event, GameEventInfo info)
    {
        if (g_iDisabledPlantPosition != 0){
            CCSPlayerController player = @event.Userid;
            var position = player.PlayerPawn.Value.AbsOrigin!;
            var angle = player.PlayerPawn.Value.AbsRotation!;
            var velocity = player.PlayerPawn.Value.AbsVelocity;
            int iPosition = (int)position[0];
            int minPosition = g_iDisabledPlantPosition - 400;
            int maxPosition = g_iDisabledPlantPosition + 400;
            if(iPosition <= maxPosition && iPosition >= minPosition){
                Server.NextFrame(() =>{
                    position[2] += 50.00f;
                    if (Config.iWarnMsgType == 0 || Config.iWarnMsgType == 2)
                        player.PrintToChat($" {ChatColors.Red}[Bombsite] {ChatColors.Default}{Config.szWarnMsgChat}");
                    if (Config.iWarnMsgType == 1 || Config.iWarnMsgType == 2)
                        player.PrintToCenter($"{Config.szWarnMsgCenter}");
                    player.Teleport(position, angle, velocity);
                });
            }
        }
        return HookResult.Continue; 
    }
    
    [GameEventHandler(HookMode.Pre)]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info){
        if (!GameRules().WarmupPeriod){
            if (GetPlayersCount() <= Config.iMinPlayers){
                SetupMapBombsites();
                Random random = new Random();
                int iSite = random.Next(1, 3);
                DisableRandomPlant(iSite);
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
                g_iDisabledPlantPosition = (int)g_fBombisteA;
                string szMSG = $"{ChatColors.Red}[Bombsite] {ChatColors.Default}{Config.szDisablePlant}";
                szMSG = szMSG.Replace("{SITE}", $" {ChatColors.Darkred}B{ChatColors.Default}");
                Server.PrintToChatAll($" {szMSG}");
                break;
            }
            case 2:
            {
                g_iDisabledPlantPosition = (int)g_fBombisteB;
                string szMSG = $"{ChatColors.Red}[Bombsite] {ChatColors.Default}{Config.szDisablePlant}";
                szMSG = szMSG.Replace("{SITE}", $" {ChatColors.Darkred}A{ChatColors.Default}");
                Server.PrintToChatAll($" {szMSG}");
                break;
            }
        }
    }

    public void SetupMapBombsites(){
        var index = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("cs_player_manager");
        foreach (var pos in index){
            g_fBombisteA = Schema.GetSchemaValue<float>(pos.Handle, "CCSPlayerResource", "m_bombsiteCenterA");
            g_fBombisteB = Schema.GetSchemaValue<float>(pos.Handle, "CCSPlayerResource", "m_bombsiteCenterB");
            //Server.PrintToChatAll($"{g_fBombisteA} | {g_fBombisteB}");
        }

        /*var Bombsites = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("func_bomb_target");
        Server.PrintToChatAll($"{bombsites.Count()}");
        foreach (var sites in Bombsites){
            float Min = Schema.GetSchemaValue<float>(sites.Handle, "CCollisionProperty", "m_vecMins");
            float Max = Schema.GetSchemaValue<float>(sites.Handle, "CCollisionProperty", "m_vecMaxs");
            Server.PrintToChatAll($"{Min} | {Max}");
        }*/
    }
    private static int GetPlayersCount(){
        int iCount = 0;
        Utilities.GetPlayers().ForEach(player =>{
            if(!player.IsBot && (player.TeamNum == 1 || player.TeamNum == 2)){
                iCount++;
            }
        });
        return iCount;
    }
    internal static CCSGameRules GameRules(){
        return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
    }
}

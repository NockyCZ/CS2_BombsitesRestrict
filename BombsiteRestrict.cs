using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Modules.Timers;

namespace BombsiteRestrict;

[MinimumApiVersion(142)]
public class GenerateConfig : BasePluginConfig
{
    [JsonPropertyName("Minimum players")] public int iMinPlayers { get; set; } = 6;
    [JsonPropertyName("Count bots as players")] public bool bCountBots { get; set; } = false;
    [JsonPropertyName("Disabled site")] public int iDisabledSite { get; set; } = 0;
    [JsonPropertyName("Which team count as players")] public int iTeam { get; set; } = 0;
}

public class BombsiteRestrict : BasePlugin, IPluginConfig<GenerateConfig>
{
    public override string ModuleName => "Bombsites Restrict";
    public override string ModuleAuthor => "Nocky";
    public override string ModuleVersion => "1.0.5";
    private static int g_iDisabledSite;
    private static bool g_bPluginDisabled;

    public GenerateConfig Config { get; set; } = null!;
    public void OnConfigParsed(GenerateConfig config)
    {
        Config = config;
    }
    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMapStart>(mapName =>
        {
            Server.NextFrame(() =>
            {
                var Bombsites = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("func_bomb_target");
                if (Bombsites.Count() != 2)
                {
                    g_bPluginDisabled = true;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[Bombsites] The Bombsite Restrict is disabled, because there are no bomb plants on this map.");
                    Console.ResetColor();
                }
                else
                {
                    g_bPluginDisabled = false;
                }
            });
        });
    }
    [GameEventHandler(HookMode.Pre)]
    public HookResult EventBombBeginplant(EventBombBeginplant @event, GameEventInfo info)
    {
        if (g_bPluginDisabled)
            return HookResult.Continue;

        if (g_iDisabledSite != 0)
        {
            var site = new CBombTarget(NativeAPI.GetEntityFromIndex(@event.Site));
            int iSiteType = 1;
            if (site.IsBombSiteB)
                iSiteType = 2;

            if (g_iDisabledSite == iSiteType)
            {
                CCSPlayerController player = @event.Userid;
                Server.NextFrame(() =>
                {
                    player.PrintToChat($"{Localizer["Prefix"]} {Localizer["Warning_Message"]}");
                    player.PrintToCenter($"{Localizer["Warning_Message_Center"]}");
                });

                var entity = new CEntityInstance(NativeAPI.GetEntityFromIndex(@event.Site));
                entity.AcceptInput("Disable");

                AddTimer(0.5f, () => { entity.AcceptInput("Enable"); }, TimerFlags.STOP_ON_MAPCHANGE);
            }
        }
        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (g_bPluginDisabled)
            return HookResult.Continue;
        if (!GameRules().WarmupPeriod)
        {
            if (GetPlayersCount() <= Config.iMinPlayers)
            {
                int iSite = Config.iDisabledSite;
                if (Config.iDisabledSite == 0)
                {
                    Random random = new Random();
                    iSite = random.Next(1, 3);
                }
                g_iDisabledSite = iSite;
                string site = g_iDisabledSite == 1 ? "B" : "A";
                Server.PrintToChatAll($"{Localizer["Prefix"]} {Localizer["Bombsite_Disabled", site]}");
            }
            else
            {
                g_iDisabledSite = 0;
            }
        }
        return HookResult.Continue;
    }
    private int GetPlayersCount()
    {
        int iCount = 0;
        Utilities.GetPlayers().ForEach(player =>
        {
            if (Config.iTeam == 1 && (CsTeam)player.TeamNum == CsTeam.CounterTerrorist)
            {
                if (!Config.bCountBots)
                {
                    if (!player.IsBot)
                        iCount++;
                }
                else
                    iCount++;
            }
            else if (Config.iTeam == 2 && (CsTeam)player.TeamNum == CsTeam.Terrorist)
            {
                if (!Config.bCountBots)
                {
                    if (!player.IsBot)
                        iCount++;
                }
                else
                    iCount++;
            }
            else if (Config.iTeam == 0 && (CsTeam)player.TeamNum == CsTeam.Terrorist || (CsTeam)player.TeamNum == CsTeam.CounterTerrorist)
            {
                if (!Config.bCountBots)
                {
                    if (!player.IsBot)
                        iCount++;
                }
                else
                    iCount++;
            }
        });
        return iCount;
    }
    internal static CCSGameRules GameRules()
    {
        return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
    }
}

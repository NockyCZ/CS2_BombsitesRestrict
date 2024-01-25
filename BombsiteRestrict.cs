using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Modules.Commands;

namespace BombsiteRestrict;

[MinimumApiVersion(142)]
public class GenerateConfig : BasePluginConfig
{
    [JsonPropertyName("Minimum players")] public int iMinPlayers { get; set; } = 6;
    [JsonPropertyName("Count bots as players")] public bool bCountBots { get; set; } = false;
    [JsonPropertyName("Disabled site")] public int iDisabledSite { get; set; } = 0;
    [JsonPropertyName("Which team count as players")] public int iTeam { get; set; } = 0;
    [JsonPropertyName("Send plant restrict message to team")] public int iMessageTeam { get; set; } = 0;
    [JsonPropertyName("Allow center message")] public bool bAllowedCenter { get; set; } = true;
}

public class BombsiteRestrict : BasePlugin, IPluginConfig<GenerateConfig>
{
    public override string ModuleName => "Bombsites Restrict";
    public override string ModuleAuthor => "Nocky";
    public override string ModuleVersion => "1.0.6";
    private static int g_iDisabledSite;
    private static int g_iMessageTeam;
    private static bool g_bPluginDisabled;

    public GenerateConfig Config { get; set; } = null!;
    public void OnConfigParsed(GenerateConfig config)
    {
        Config = config;
    }
    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnTick>(() =>
        {
            if (Config.bAllowedCenter && g_iDisabledSite != 0)
            {
                foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV))
                {
                    string site = g_iDisabledSite == 1 ? "B" : "A";
                    if (g_iMessageTeam == (byte)CsTeam.CounterTerrorist || g_iMessageTeam == (byte)CsTeam.Terrorist)
                    {
                        if (p.TeamNum == g_iMessageTeam)
                        {
                            p.PrintToCenterHtml($"{Localizer["Bombsite_Disabled_Center", site]}");
                        }
                    }
                    else
                    {
                        p.PrintToCenterHtml($"{Localizer["Bombsite_Disabled_Center", site]}");
                    }
                }
            }
        });
        RegisterListener<Listeners.OnMapStart>(mapName =>
        {
            Server.NextFrame(() =>
            {
                g_iDisabledSite = 0;
                g_iMessageTeam = Config.iMessageTeam;
                if (g_iMessageTeam == 1)
                    g_iMessageTeam = (byte)CsTeam.CounterTerrorist;
                else if (g_iMessageTeam == 2)
                    g_iMessageTeam = (byte)CsTeam.Terrorist;


                var Bombsites = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("func_bomb_target");
                if (Bombsites.Count() != 2)
                {
                    g_bPluginDisabled = true;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[Bombsite Restrict] The Bombsite Restrict plugin is disabled, because there are no bomb plants on this map.");
                    Console.ResetColor();
                }
                else
                {
                    g_bPluginDisabled = false;
                }
            });
        });
    }

    [ConsoleCommand("css_restrictbombsite", "Restrict bombsite for current map")]
    [CommandHelper(1, "<site>", whoCanExecute: CommandUsage.SERVER_ONLY)]
    public void RestrictBombsite_CMD(CCSPlayerController player, CommandInfo info)
    {
        var arg = info.GetArg(1);
        if (!int.TryParse(arg, out int site))
        {
            info.ReplyToCommand($"[Bombsite Restrict] The site must be a number!");
            return;
        }
        if (site < 1 && site > 2)
        {
            info.ReplyToCommand($"[Bombsite Restrict] The site must be a 1 (A) or 2 (B)!");
            return;
        }
        string allowedSite = site == 1 ? "A" : "B";
        info.ReplyToCommand($"[Bombsite Restrict] {allowedSite} plant was blocked on this map. If there are less than {Config.iMinPlayers} players.");
        Config.iDisabledSite = site;
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        if (g_bPluginDisabled)
            return HookResult.Continue;

        var Sites = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("func_bomb_target");
        foreach (var entity in Sites)
        {
            entity.AcceptInput("Enable");
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
                DisableBombsite();

                if (g_iMessageTeam == (byte)CsTeam.CounterTerrorist || g_iMessageTeam == (byte)CsTeam.Terrorist)
                {
                    foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && p.TeamNum == g_iMessageTeam))
                    {
                        player.PrintToChat($"{Localizer["Prefix"]} {Localizer["Bombsite_Disabled", site]}");
                    }
                }
                else
                {
                    Server.PrintToChatAll($"{Localizer["Prefix"]} {Localizer["Bombsite_Disabled", site]}");
                }
            }
            else
            {
                g_iDisabledSite = 0;
            }
        }
        return HookResult.Continue;
    }
    private static void DisableBombsite()
    {
        var Sites = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("func_bomb_target");
        foreach (var entity in Sites)
        {
            var site = new CBombTarget(NativeAPI.GetEntityFromIndex((int)entity.Index));
            int entitySite = site.IsBombSiteB ? 2 : 1;
            if (entitySite == g_iDisabledSite)
                entity.AcceptInput("Disable");
        }
    }
    private int GetPlayersCount()
    {
        int iCount = 0;
        foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsHLTV && p.Connected == PlayerConnectedState.PlayerConnected))
        {
            if (Config.iTeam == 0 && (player.TeamNum == (byte)CsTeam.Terrorist || player.TeamNum == (byte)CsTeam.CounterTerrorist))
            {
                if (!Config.bCountBots && !player.IsBot)
                {
                    iCount++;
                }
                else if (Config.bCountBots)
                {
                    iCount++;
                }
            }
            else if ((Config.iTeam == 1 || Config.iTeam == 2) && player.TeamNum == Config.iTeam + 1)
            {
                if (!Config.bCountBots && !player.IsBot)
                {
                    iCount++;
                }
                else if (Config.bCountBots)
                {
                    iCount++;
                }
            }
        }
        return iCount;
    }
    internal static CCSGameRules GameRules()
    {
        return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
    }
}

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Modules.Commands;


namespace BombsiteRestrict;
public class Config : BasePluginConfig
{
    [JsonPropertyName("Minimum players")] public int iMinPlayers { get; set; } = 6;
    [JsonPropertyName("Count bots as players")] public bool bCountBots { get; set; } = false;
    [JsonPropertyName("Disabled site")] public int iDisabledSite { get; set; } = 0;
    [JsonPropertyName("Which team count as players")] public int iTeam { get; set; } = 0;
    [JsonPropertyName("Send plant restrict message to team")] public int iMessageTeam { get; set; } = 0;
    [JsonPropertyName("Center message timer")] public int iTimer { get; set; } = 15;
}

public class BombsiteRestrict : BasePlugin, IPluginConfig<Config>
{
    public override string ModuleName => "Bombsite Restrict";
    public override string ModuleAuthor => "Nocky (SourceFactory.eu)";
    public override string ModuleVersion => "1.0.9";
    public Config Config { get; set; } = new Config();
    public void OnConfigParsed(Config config) { Config = config; }
    private static CounterStrikeSharp.API.Modules.Timers.Timer? hudTimer;
    private int disabledSite;
    private int teamMessages;
    private bool isPluginDisabled;
    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnTick>(() =>
        {
            if (disabledSite != 0)
            {
                var site = disabledSite == 1 ? "B" : "A";
                foreach (var p in Utilities.GetPlayers().Where(p => !p.IsBot && !p.IsHLTV))
                {
                    if (teamMessages == 3 || teamMessages == 2)
                    {
                        if (p.TeamNum == teamMessages)
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

        RegisterListener<Listeners.OnMapEnd>(() =>
        {
            if (hudTimer != null)
                hudTimer.Kill();
        });
        RegisterListener<Listeners.OnMapStart>(mapName =>
        {
            Server.NextFrame(() =>
            {
                disabledSite = 0;
                teamMessages = Config.iMessageTeam;
                if (teamMessages == 1)
                    teamMessages = 3;
                else if (teamMessages == 2)
                    teamMessages = 2;


                var Bombsites = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("func_bomb_target");
                if (Bombsites.Count() != 2)
                {
                    isPluginDisabled = true;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[Bombsite Restrict] The Bombsite Restrict plugin is disabled, because there are no bomb plants on this map.");
                    Console.ResetColor();
                }
                else
                {
                    isPluginDisabled = false;
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
        if (isPluginDisabled)
            return HookResult.Continue;

        if (hudTimer != null)
            hudTimer.Kill();

        disabledSite = 0;
        var Sites = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("func_bomb_target");
        foreach (var entity in Sites)
        {
            if (entity.IsValid)
                entity.AcceptInput("Enable");
        }

        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Post)]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (isPluginDisabled)
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
                disabledSite = iSite;
                string site = disabledSite == 1 ? "B" : "A";
                DisableBombsite();

                if (teamMessages == 3 || teamMessages == 2)
                {
                    foreach (var player in Utilities.GetPlayers().Where(p => !p.IsBot && !p.IsHLTV && p.TeamNum == teamMessages))
                    {
                        player.PrintToChat($"{Localizer["Prefix"]} {Localizer["Bombsite_Disabled", site, Config.iMinPlayers]}");
                    }
                }
                else
                {
                    Server.PrintToChatAll($"{Localizer["Prefix"]} {Localizer["Bombsite_Disabled", site, Config.iMinPlayers]}");
                }
                if (Config.iTimer > 1)
                {
                    hudTimer = AddTimer(Config.iTimer, () =>
                    {
                        disabledSite = 0;
                    });
                }
            }
            else
            {
                disabledSite = 0;
            }
        }
        return HookResult.Continue;
    }
    private void DisableBombsite()
    {
        var Sites = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("func_bomb_target");
        foreach (var entity in Sites)
        {
            var site = new CBombTarget(NativeAPI.GetEntityFromIndex((int)entity.Index));
            int entitySite = site.IsBombSiteB ? 2 : 1;
            if (entitySite == disabledSite)
            {
                if (entity.IsValid)
                    entity.AcceptInput("Disable");
            }
        }
    }
    private int GetPlayersCount()
    {
        var playersList = Utilities.GetPlayers().Where(p => !p.IsHLTV && p.Connected == PlayerConnectedState.PlayerConnected && (p.Team == CsTeam.Terrorist || p.Team == CsTeam.CounterTerrorist)).ToList();

        if (!Config.bCountBots)
            playersList.RemoveAll(p => p.IsBot);

        if (Config.iTeam == 1 || Config.iTeam == 2)
            playersList.RemoveAll(p => p.TeamNum != Config.iTeam + 1);

        return playersList.Count();
    }
    internal static CCSGameRules GameRules()
    {
        return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
    }
}

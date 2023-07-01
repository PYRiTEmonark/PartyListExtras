using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using ffxivPartyListExtras.Windows;
using Dalamud.Game.Gui;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.ClientState.Objects;
using System.Collections.Generic;
using ImGuiScene;
using System.Linq;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI;
using System.Text.Json;
using System;
using Dalamud.Utility;
using static Lumina.Data.Parsing.Uld.NodeData;

namespace ffxivPartyListExtras
{
    // number to update so the DLL actually changes: 5
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "ffxivPartyListExtras";
        private const string CommandName = "/plx";

        private bool OverlayEnabled = true;

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("ffxivPartyListExtras");
        public GameGui GameGui { get; init; }
        public ChatGui ChatGui { get; init; }
        public ClientState ClientState { get; init; }
        public ObjectTable ObjectTable { get; init; }
        public PartyList PartyList { get; init; }


        private ConfigWindow ConfigWindow { get; init; }
        private OverlayWindow OverlayWindow { get; init; }
        internal Dictionary<string, TextureWrap> textures = new Dictionary<string, TextureWrap>();
        internal Dictionary<int, StatusEffectData> statusEffectData = new Dictionary<int, StatusEffectData>();

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] GameGui gameGui,
            [RequiredVersion("1.0")] ChatGui chatGui,
            [RequiredVersion("1.0")] ClientState clientState,
            [RequiredVersion("1.0")] ObjectTable objectTable,
            [RequiredVersion("1.0")] PartyList partyList)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.GameGui = gameGui;
            this.ChatGui = chatGui;
            this.ClientState = clientState;
            this.PartyList = partyList;
            this.ObjectTable = objectTable;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            ConfigWindow = new ConfigWindow(this);
            OverlayWindow = new OverlayWindow(this);
            OverlayWindow.IsOpen = true;

            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(OverlayWindow);

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            LoadAssets();

            // DO THIS LAST
            // otherwise if there's an error the command gets registered
            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "toggles the overlay"
            });
        }

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            
            ConfigWindow.Dispose();
            OverlayWindow.Dispose();

            this.CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            // Currently just toggle the window on slash command
            if (args == "missing") {
                PluginLog.Information(
                    "Missing Status Ids: {0}",
                    string.Join("", OverlayWindow.missing_ids
                        .Select(x => string.Format("{0} = {1}; ", x.Item1, x.Item2))));
            } else if (args == "missing clear") {
                OverlayWindow.missing_ids.Clear();
            } else if (args == "reload") {
                LoadAssets();
            }
            else OverlayEnabled = !OverlayEnabled;
        }

        private void LoadAssets()
        {
            textures = new Dictionary<string, TextureWrap>();
            statusEffectData = new Dictionary<int, StatusEffectData>();

            // Loads/Reloads icons and data files
            PluginLog.Information("Loading/Reloading PartyListExtras assets");

            // Find our image files
            var baseImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "Icons");
            var imageNames = Directory.GetFiles(baseImagePath, "*.png").Select(Path.GetFileName).ToArray();

            // Logging cus VS refuses to copy images sometimes
            PluginLog.Debug("Loading images from {0}", baseImagePath);

            // Load images into the dict
            foreach (var imageName in imageNames)
            {
                if (imageName == null) continue;
                var imagePath = Path.Combine(baseImagePath, imageName);
                this.textures.Add(imageName, this.PluginInterface.UiBuilder.LoadImage(imagePath));
            }

            PluginLog.Debug("Images Loaded: {0}", string.Join(',', imageNames));

            // as above but for status .json files in /StatusData
            var baseDataPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "StatusData");
            var dataNames = Directory.GetFiles(baseDataPath, "*.json").Select(Path.GetFileName).ToArray();

            PluginLog.Debug("Loading data files from {0}", baseDataPath);

            foreach (var dataName in dataNames)
            {
                if (dataName == null) continue;
                var dataPath = Path.Combine(baseDataPath, dataName);
                using (FileStream fs = File.OpenRead(dataPath))
                {
                    var rawData = JsonSerializer.Deserialize<List<StatusEffectData>>(fs);
                    if (rawData == null)
                    {
                        PluginLog.Warning("Data file {0} didn't load - Badly formatted?");
                        continue;
                    }

                    foreach (StatusEffectData sxd in rawData)
                    {
                        if (statusEffectData.ContainsKey(sxd.row_id))
                        {
                            PluginLog.Warning("Key {0} exists twice; accepted {1}, rejected {2}",
                                sxd.row_id, statusEffectData[sxd.row_id].status_name, sxd.status_name);
                            continue;
                        }
                        this.statusEffectData.Add(sxd.row_id, sxd);
                    }
                }
            }

            PluginLog.Debug("Data files Loaded: {0}", string.Join(',', dataNames));
        }

        private unsafe void DrawUI()
        {
            this.WindowSystem.Draw();

            // show overlay if open
            AddonPartyList* apl = (AddonPartyList*)GameGui.GetAddonByName("_PartyList");
            if (apl == null) { return; }
            var isvis = apl->AtkUnitBase.IsVisible;
            OverlayWindow.IsOpen = isvis && OverlayEnabled;
        }

        public void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }
    }
    internal struct StatusEffectData
    {
        // to be popped out and used as Mapping Key
        public required int row_id { get; set; }
        // should be as in game
        public required string status_name { get; set; }

        // special acts as a custom field, e.g. stance, invuln
        public string? special { get; set; }

        // Mitigation
        public float? phys_mit { get; set; }
        public float? magi_mit { get; set; }
        public float? othr_mit { get; set; }

        // Damage Up
        public float? phys_up { get; set; }
        public float? magi_up { get; set; }
        public float? othr_up { get; set; }

        // HP Regen in potency per tick
        // TODO: anything better than that?
        public float? regen { get; set; }

    }

}


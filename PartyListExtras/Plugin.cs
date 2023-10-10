using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using System.Collections.Generic;
using ImGuiScene;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.UI;
using System.Text.Json;
using Dalamud.Plugin.Services;
using PartyListExtras.Windows;
using Dalamud.Interface.Internal;
using System.Text.Json.Serialization;

namespace PartyListExtras
{
    // number to update so the DLL actually changes: 9
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "PartyListExtras";
        private const string CommandName = "/plx";

        private bool overlayEnabled = true;

        private DalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("PartyListExtras");
        public IGameGui GameGui { get; init; }
        public IChatGui ChatGui { get; init; }
        public IClientState ClientState { get; init; }
        public IObjectTable ObjectTable { get; init; }
        public IPartyList PartyList { get; init; }
        public IPluginLog pluginLog { get; init; }


        private ConfigWindow ConfigWindow { get; init; }
        private OverlayWindow OverlayWindow { get; init; }
        internal Dictionary<string, IDalamudTextureWrap> textures = new Dictionary<string, IDalamudTextureWrap>();
        internal Dictionary<int, StatusEffectData> statusEffectData = new Dictionary<int, StatusEffectData>();

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager,
            [RequiredVersion("1.0")] IGameGui gameGui,
            [RequiredVersion("1.0")] IChatGui chatGui,
            [RequiredVersion("1.0")] IClientState clientState,
            [RequiredVersion("1.0")] IObjectTable objectTable,
            [RequiredVersion("1.0")] IPartyList partyList,
            [RequiredVersion("1.0")] IPluginLog pluginLog)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.GameGui = gameGui;
            this.ChatGui = chatGui;
            this.ClientState = clientState;
            this.PartyList = partyList;
            this.ObjectTable = objectTable;
            this.pluginLog = pluginLog;

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
                HelpMessage = "Toggles Overlay. Use /plx help for other commands."
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
            // this isn't the greatest way of doing it but it's fine
            if (args == "missing")
            {
                pluginLog.Information(
                    "Missing Status Ids: {0}",
                    string.Join("", OverlayWindow.missing_ids
                        .Select(x => string.Format("{0} = {1}; ", x.Item1, x.Item2))));
            }
            else if (args == "missing clear")
            {
                OverlayWindow.missing_ids.Clear();
            }
            else if (args == "reload")
            {
                LoadAssets();
            }
            else if (args == "config")
            {
                ConfigWindow.IsOpen = true;
            }
            else if (args == "help")
            {
                ChatGui.Print("Party List Extras commands:\n" +
                    "/plx - toggle overlay\n" +
                    "/plx help - sends this message\n" +
                    "/plx reload - load data files and images\n" +
                    "/plx config - opens config window");
                //ChatGui.UpdateQueue();
            }
            else if (args == "")
            {
                overlayEnabled = !overlayEnabled;
            }
            else
            {
                ChatGui.Print("Unknown command - use /plx help for information");
            }
        }

        private void LoadAssets()
        {
            textures = new Dictionary<string, IDalamudTextureWrap>();
            statusEffectData = new Dictionary<int, StatusEffectData>();

            // Loads/Reloads icons and data files
            pluginLog.Information("Loading/Reloading PartyListExtras assets");

            // Find our image files
            var baseImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "Icons");
            var imageNames = Directory.GetFiles(baseImagePath, "*.png").Select(Path.GetFileName).ToArray();

            // Logging cus VS refuses to copy images sometimes
            pluginLog.Debug("Loading images from {0}", baseImagePath);

            // Load images into the dict
            foreach (var imageName in imageNames)
            {
                if (imageName == null) continue;
                var imagePath = Path.Combine(baseImagePath, imageName);
                this.textures.Add(imageName, this.PluginInterface.UiBuilder.LoadImage(imagePath));
            }

            pluginLog.Debug("Images Loaded: {0}", string.Join(',', imageNames));

            // as above but for status .json files in /StatusData
            var baseDataPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "StatusData");
            var dataNames = Directory.GetFiles(baseDataPath, "*.json").Select(Path.GetFileName).ToArray();

            pluginLog.Debug("Loading data files from {0}", baseDataPath);

            foreach (var dataName in dataNames)
            {
                if (dataName == null) continue;
                var dataPath = Path.Combine(baseDataPath, dataName);
                using (FileStream fs = File.OpenRead(dataPath))
                {
                    List<StatusEffectData>? rawData;
                    try
                    {
                         rawData = JsonSerializer.Deserialize<List<StatusEffectData>>(fs);
                    } catch (JsonException ex) {
                        pluginLog.Warning("Error loading file {0} - {1}", dataName, ex.Message);
                        continue;
                    }
                    if (rawData == null)
                    {
                        pluginLog.Warning("Data file {0} didn't load - Badly formatted?");
                        continue;
                    }

                    foreach (StatusEffectData sxd in rawData)
                    {
                        if (statusEffectData.ContainsKey(sxd.row_id))
                        {
                            pluginLog.Warning("Key {0} exists twice; accepted {1}, rejected {2}",
                                sxd.row_id, statusEffectData[sxd.row_id].status_name, sxd.status_name);
                            continue;
                        }
                        this.statusEffectData.Add(sxd.row_id, sxd);
                    }
                }
            }

            pluginLog.Debug("Data files Loaded: {0}", string.Join(", ", dataNames));
        }

        private unsafe void DrawUI()
        {
            this.WindowSystem.Draw();

            // show overlay if open
            AddonPartyList* apl = (AddonPartyList*)GameGui.GetAddonByName("_PartyList");
            if (apl == null) { return; }
            var isvis = apl->AtkUnitBase.IsVisible;
            OverlayWindow.IsOpen = isvis && overlayEnabled;
        }

        public void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }
    }

}

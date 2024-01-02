using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using Hjson;
using Newtonsoft.Json;
using PartyListExtras.Windows;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PartyListExtras
{
    // number to update so the DLL actually changes: 11
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "PartyListExtras";
        private const string CommandName = "/plx";

        internal DalamudPluginInterface PluginInterface { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("PartyListExtras");

        private ICommandManager CommandManager { get; init; }
        public IGameGui GameGui { get; init; }
        public IChatGui ChatGui { get; init; }
        public IClientState ClientState { get; init; }
        public IObjectTable ObjectTable { get; init; }
        public IFlyTextGui FlyTextGui { get; init; }
        public IPartyList PartyList { get; init; }
        public ICondition Condition { get; init; }
        public IPluginLog log { get; init; }
        public IGameInteropProvider Hooks { get; init; }
        public ISigScanner SigScanner { get; init; }


        internal ConfigWindow ConfigWindow { get; init; }
        internal OverlayWindow OverlayWindow { get; init; }
        internal FlyText flytext { get; init; }
        internal Dictionary<string, IDalamudTextureWrap > textures = new Dictionary<string, IDalamudTextureWrap>();
        internal Dictionary<int, StatusEffectData> statusEffectData = new Dictionary<int, StatusEffectData>();

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager,
            [RequiredVersion("1.0")] IGameGui gameGui,
            [RequiredVersion("1.0")] IChatGui chatGui,
            [RequiredVersion("1.0")] IClientState clientState,
            [RequiredVersion("1.0")] IObjectTable objectTable,
            [RequiredVersion("1.0")] IPartyList partyList,
            [RequiredVersion("1.0")] ICondition condition,
            [RequiredVersion("1.0")] IPluginLog pluginLog,
            [RequiredVersion("1.0")] IFlyTextGui flyTextGui,
            [RequiredVersion("1.0")] IGameInteropProvider hooks,
            [RequiredVersion("1.0")] ISigScanner sigScanner)

        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.GameGui = gameGui;
            this.ChatGui = chatGui;
            this.ClientState = clientState;
            this.PartyList = partyList;
            this.Condition = condition;
            this.ObjectTable = objectTable;
            this.FlyTextGui = flyTextGui;
            this.log = pluginLog;
            this.Hooks = hooks;
            this.SigScanner = sigScanner;


            // Set up configuration
            try
            {
            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            }
            catch (JsonSerializationException)
            {
                var fname = this.PluginInterface.ConfigFile.FullName;
                File.Move(fname, fname + "BAK", true);

                ChatGui.Print("Invalid Configuration file - The old file has been backed up\n" +
                    "If you've recently updated, especially if you've skipped versions, this may be unavoidable\n" +
                    "If the error persists please send feedback in the plugin installer", "PartyListExtras", 16);
                this.Configuration = new Configuration();
            }

            this.Configuration.Initialize(this.PluginInterface);

            // Set up windows
            ConfigWindow = new ConfigWindow(this);
            OverlayWindow = new OverlayWindow(this);

            WindowSystem.AddWindow(ConfigWindow);

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            LoadAssets();

            // Open plugin config on first open
            if (pluginInterface.Reason == PluginLoadReason.Installer)
            {
                this.ConfigWindow.IsOpen = true;
            }

            // Register fly text listener
            flytext = new FlyText(this);

            // DO THIS LAST
            // otherwise if there's an error the command gets registered
            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens config window. Use /plx help for other commands."
            });
        }

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();

            ConfigWindow.Dispose();
            OverlayWindow.Dispose();

            this.CommandManager.RemoveHandler(CommandName);

            flytext.Dispose();
        }

        private void OnCommand(string command, string args)
        {
            // this isn't the greatest way of doing it but it's fine
            if (args == "help")
            {
                ChatGui.Print("Party List Extras commands:\n" +
                    "/plx - opens config window\n" +
                    "/plx on/off/toggle - enables, disables and toggles the overlay respectively\n" +
                    "/plx reload - load data files and images\n" +
                    "/plx help - sends this message"
                );
                //ChatGui.UpdateQueue();
            }
            else if (args == "missing")
            {
                log.Information(
                    "Missing Status Ids: {0}",
                    string.Join("", OverlayWindow.missing_ids
                        .Select(x => string.Format("{0} = {1}; ", x.Item1, x.Item2))));
            }
            else if (args == "missing clear") OverlayWindow.missing_ids.Clear();
            else if (args == "reload") LoadAssets();
            else if (args == "on")
            {
                Configuration.EnableOverlay = true;
                Configuration.Save();
            }
            else if (args == "off")
            {
                Configuration.EnableOverlay = false;
                Configuration.Save();
            }
            else if (args == "toggle")
            {
                Configuration.EnableOverlay = !Configuration.EnableOverlay;
                Configuration.Save();
            }
            else if (args == "") ConfigWindow.IsOpen = true;
            else ChatGui.Print("Unknown command - use /plx help for information");
        }

        private void LoadAssets()
        {
            textures = new Dictionary<string, IDalamudTextureWrap>();
            statusEffectData = new Dictionary<int, StatusEffectData>();

            // Loads/Reloads icons and data files
            log.Information("Loading/Reloading PartyListExtras assets");

            // Find our image files
            var baseImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "Icons");
            var imageNames = Directory.GetFiles(baseImagePath, "*.png").Select(Path.GetFileName).ToArray();

            // Logging cus VS refuses to copy images sometimes
            log.Debug("Loading images from {0}", baseImagePath);

            // Load images into the dict
            foreach (var imageName in imageNames)
            {
                if (imageName == null) continue;
                var imagePath = Path.Combine(baseImagePath, imageName);
                this.textures.Add(imageName, this.PluginInterface.UiBuilder.LoadImage(imagePath));
            }

            log.Debug("Images Loaded: {0}", string.Join(',', imageNames));

            // as above but for status .json files in /StatusData
            var baseDataPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "StatusData");
            var dataNames = Directory.GetFiles(baseDataPath, "*.json").Select(Path.GetFileName).ToArray();

            log.Debug("Loading data files from {0}", baseDataPath);

            foreach (var dataName in dataNames)
            {
                if (dataName == null) continue;
                var dataPath = Path.Combine(baseDataPath, dataName);
                using (FileStream fs = File.OpenRead(dataPath))
                {
                    // Load a data file into StatusEffectData objects
                    List<StatusEffectData>? rawData;
                    try {
                        var jsonString = HjsonValue.Load(fs).ToString();
                        rawData = JsonConvert.DeserializeObject<List<StatusEffectData>>(jsonString);
                    } catch (JsonException ex) {
                        log.Warning("Error loading file {0} - {1}", dataName, ex.Message);
                        continue;
                    }

                    if (rawData == null)
                    {
                        log.Warning("Data file {0} didn't load - Badly formatted?");
                        continue;
                    }

                    foreach (StatusEffectData sxd in rawData)
                    {
                        if (statusEffectData.ContainsKey(sxd.row_id))
                        {
                            log.Warning("Key {0} exists twice; accepted {1}, rejected {2}",
                                sxd.row_id, statusEffectData[sxd.row_id].status_name, sxd.status_name);
                            continue;
                        }
                        this.statusEffectData.Add(sxd.row_id, sxd);
                    }
                }
            }

            log.Debug("Data files Loaded: {0}", string.Join(", ", dataNames));
        }

        private unsafe void DrawUI()
        {
            this.WindowSystem.Draw();

            // show overlay if open
            AddonPartyList* apl = (AddonPartyList*)GameGui.GetAddonByName("_PartyList");
            if (apl == null) { return; }
            bool isvis = apl->AtkUnitBase.IsVisible;

            bool showOverlay = true;
            // Hide if not in combat
            if (Configuration.hideOutOfCombat && !Condition[ConditionFlag.InCombat])
                showOverlay = false;

            // But show if in dt
            if (Configuration.alwaysShowInDuty && Condition[ConditionFlag.BoundByDuty])
                showOverlay = true;
            
            if (isvis && Configuration.EnableOverlay && showOverlay)
                this.OverlayWindow.Draw();
        }

        public void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }
    }

}

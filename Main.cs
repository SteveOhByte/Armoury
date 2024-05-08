using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using LiteConfig;
using LSPD_First_Response.Mod.API;
using OhPluginEssentials;
using Rage;

namespace Armoury
{
    public class Main : Plugin
    {
        public static Logger Logger;
        private static string config = AppDomain.CurrentDomain.BaseDirectory + @"\plugins\LSPDFR\Armoury\config.lc";
        public static string defaultLoadout = string.Empty;
        public static bool disableRWS = true;
        public static Keys menuKey = Keys.I;
        public static Keys menuModifier = Keys.Alt;
        public static Keys rifleHotkey = Keys.D1;
        public static Keys rifleHotkeyModifier = Keys.Shift;
        public static Keys shotgunHotkey = Keys.D2;
        public static Keys shotgunHotkeyModifier = Keys.Shift;
        public static Keys restockHotkey = Keys.R;
        public static Keys restockHotkeyModifier = Keys.Shift;

        private static readonly Version assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
        private static readonly string version = $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";

        private static MenuHandler menuHandler;
        private static LoadoutHandler loadoutHandler;
        
        public override void Initialize()
        {
            Game.LogTrivial("Armoury Initializing...");
            Logger = new Logger();
            
            if (!File.Exists(config))
            {
                File.Create(config).Close();
                LC.WriteValue(config, "Default Loadout", defaultLoadout);
                LC.WriteValue(config, "Disable RWS", disableRWS);
                LC.WriteValue(config, "Open Menu", menuKey);
                LC.WriteValue(config, "Open Menu Modifier", menuModifier);
                LC.WriteValue(config, "Rifle Hotkey", $"{rifleHotkeyModifier} + {rifleHotkey}");
                LC.WriteValue(config, "Shotgun Hotkey", $"{shotgunHotkeyModifier} + {shotgunHotkey}");
                LC.WriteValue(config, "Restock Hotkey", $"{restockHotkeyModifier} + {restockHotkey}");
            }
            else
            {
                defaultLoadout = LC.ReadString(config, "Default Loadout");
                disableRWS = LC.ReadBool(config, "Disable RWS");
                menuKey = (Keys)Enum.Parse(typeof(Keys), LC.ReadString(config, "Open Menu"), true);
                menuModifier = (Keys)Enum.Parse(typeof(Keys), LC.ReadString(config, "Open Menu Modifier"), true);
                rifleHotkey = ParseKey("Rifle Hotkey", 1);
                rifleHotkeyModifier = ParseKey("Rifle Hotkey", 0);
                shotgunHotkey = ParseKey("Shotgun Hotkey", 1);
                shotgunHotkeyModifier = ParseKey("Shotgun Hotkey", 0);
                restockHotkey = ParseKey("Restock Hotkey", 1);
                restockHotkeyModifier = ParseKey("Restock Hotkey", 0);
            }

            if (disableRWS)
            {
                string stpConfig = AppDomain.CurrentDomain.BaseDirectory + @"\plugins\LSPDFR\StopThePed.ini";
                if (!File.Exists(stpConfig))
                    Logger.Error("StopThePed.ini does not exist - but was requested by Armoury");
                else
                {
                    string[] lines = File.ReadAllLines(stpConfig);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (!lines[i].StartsWith("EnabledOnStartup=yes")) continue;
                        
                        lines[i] = "EnabledOnStartup=no";
                        File.WriteAllLines(stpConfig, lines);
                        Prompt.DisplayPrompt("~y~R.W.S.~w~ has been ~r~disabled~w~", string.Empty,
                            TimeSpan.FromSeconds(8));
                        break;
                    }
                }
            }
            
            Logger.Log("Armoury initialized at " + DateTime.Now);
            
            // LSPDFR Boilerplate
            Functions.OnOnDutyStateChanged += OnOnDutyStateChangedHandler;
            
            Logger.Log($"Armoury v {version} has been initialized");
            Logger.Log("Go on duty to start using Armoury");
        }

        private Keys ParseKey(string configString, int index)
        {
            string[] parts = LC.ReadString(config, configString).Split('+');
            if (parts.Length > index)
            {
                string keyString = parts[index].Trim();
                if (string.IsNullOrEmpty(keyString) || keyString.Equals("None", StringComparison.OrdinalIgnoreCase))
                {
                    return Keys.None;
                }
                return (Keys)Enum.Parse(typeof(Keys), keyString, true);
            }
            return index == 0 ? Keys.None : Keys.None;  // Default to Keys.None if the part isn't available
        }
        
        public override void Finally()
        {
            Logger.Log($"Armoury v {version} has been cleaned up");
        }
        
        private static void OnOnDutyStateChangedHandler(bool onDuty)
        {
            if (onDuty)
            {
                Logger.Log($"Armoury v {version} has been loaded");

                Notification.DisplayNotification(TimeSpan.FromSeconds(15), Color.FromArgb(150, 0, 0, 0),
                    $"Armoury v {version}", "by SteveOhByte", "has been loaded");
                
                loadoutHandler = new LoadoutHandler();
                
                menuHandler = new MenuHandler();
                menuHandler.Start();
            }
            else
            {
                menuHandler.Stop();
            }
        }
    }
}
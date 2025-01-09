using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Windows.Forms;
using LiteConfig;
using LSPD_First_Response.Mod.API;
using Microsoft.Win32;
using OhPluginEssentials;
using Rage;

namespace Armoury
{
    public class Main : Plugin
    {
        public static Logger Logger;
        public static string DefaultLoadout = string.Empty;
        public static bool DisableRWS = true;
        public static bool HeliEnabled = true;
        public static Keys MenuKey = Keys.I;
        public static Keys MenuModifier = Keys.Alt;
        public static Keys RifleHotkey = Keys.K;
        public static Keys RifleHotkeyModifier = Keys.Alt;
        public static Keys ShotgunHotkey = Keys.L;
        public static Keys ShotgunHotkeyModifier = Keys.Alt;
        public static Keys LessLethalHotkey = Keys.M;
        public static Keys LessLethalHotkeyModifier = Keys.Alt;
        public static Keys RestockHotkey = Keys.R;
        public static Keys RestockHotkeyModifier = Keys.Shift;
        public static readonly List<string> AllowedVehicles = new List<string>();

        private static readonly string config = AppDomain.CurrentDomain.BaseDirectory + @"\plugins\LSPDFR\Armoury\config.lc";
        private static readonly Version assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
        private static readonly string version = $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";

        private static MenuHandler menuHandler;
        private static LoadoutHandler loadoutHandler;

        private Prompt associationPrompt;
        private const float associationWaitTime = 15.0f;
        private float associationWaitTimer = 0.0f;
        
        public override void Initialize()
        {
            Game.LogTrivial("Armoury Initializing...");
            Logger = new Logger();
            
            if (!File.Exists(config))
            {
                File.Create(config).Close();
                LC.WriteValue(config, "Default Loadout", DefaultLoadout);
                LC.WriteValue(config, "Disable RWS", DisableRWS);
                LC.WriteValue(config, "Allow Armoury on Helicopters", HeliEnabled);
                LC.WriteValue(config, "Open Menu", MenuKey);
                LC.WriteValue(config, "Open Menu Modifier", MenuModifier);
                LC.WriteValue(config, "Rifle Hotkey", $"{RifleHotkeyModifier} + {RifleHotkey}");
                LC.WriteValue(config, "Shotgun Hotkey", $"{ShotgunHotkeyModifier} + {ShotgunHotkey}");
                LC.WriteValue(config, "Less Lethal Hotkey", $"{LessLethalHotkeyModifier} + {LessLethalHotkey}");
                LC.WriteValue(config, "Restock Hotkey", $"{RestockHotkeyModifier} + {RestockHotkey}");
                string allowedVehiclesString = string.Join(",", AllowedVehicles);
                LC.WriteValue(config, "Armoury-Enabled Vehicles", allowedVehiclesString);
            }
            else
            {
                DefaultLoadout = LC.ReadString(config, "Default Loadout");
                DisableRWS = LC.ReadBool(config, "Disable RWS");
                HeliEnabled = LC.ReadBool(config, "Allow Armoury on Helicopters");
                MenuKey = (Keys)Enum.Parse(typeof(Keys), LC.ReadString(config, "Open Menu"), true);
                MenuModifier = (Keys)Enum.Parse(typeof(Keys), LC.ReadString(config, "Open Menu Modifier"), true);
                RifleHotkey = ParseKey("Rifle Hotkey", 1);
                RifleHotkeyModifier = ParseKey("Rifle Hotkey", 0);
                ShotgunHotkey = ParseKey("Shotgun Hotkey", 1);
                ShotgunHotkeyModifier = ParseKey("Shotgun Hotkey", 0);
                LessLethalHotkey = ParseKey("Less Lethal Hotkey", 1);
                LessLethalHotkeyModifier = ParseKey("Less Lethal Hotkey", 0);
                RestockHotkey = ParseKey("Restock Hotkey", 1);
                RestockHotkeyModifier = ParseKey("Restock Hotkey", 0);
                List<string> armouryEnabledVehicleStrings = LC.ReadList<string>(config, "Armoury-Enabled Vehicles");
                foreach (string vehicle in armouryEnabledVehicleStrings.Where(vehicle => Model.VehicleModels.Contains(vehicle)))
                    AllowedVehicles.Add(vehicle);
            }

            if (DisableRWS)
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
            
            Functions.OnOnDutyStateChanged += OnOnDutyStateChangedHandler;
            
            Logger.Log($"Armoury v {version} has been initialized");

            if (LCNotAssociated())
            {
                // Allow association choice for 10 seconds, then exit the checking loop
                associationPrompt = Prompt.DisplayPrompt("Press ~y~Y ~w~to connect .lc", "files to Notepad", TimeSpan.FromSeconds(associationWaitTime));
                GameFiber.StartNew(AssociateFiles);
            }
        }
        
        private void AssociateFiles()
        {
            while (associationWaitTimer < associationWaitTime)
            {
                associationWaitTimer += Game.FrameTime;
                GameFiber.Yield();
                
                if (Game.IsKeyDown(Keys.Y))
                {
                    try
                    {
                        CreateFileAssociation();
                    }
                    catch (SecurityException ex)
                    {
                        Logger.Log($"The plugin attempted to associate .lc files with Notepad, but was denied access: {ex.Message}");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Logger.Log($"The plugin attempted to associate .lc files with Notepad, but was denied access: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex.Message);
                    }
                    associationPrompt.Stop();
                    break;
                }
            }
        }

        private bool LCNotAssociated()
        {
            const string extension = ".lc";
            // Use HashSet to avoid adding duplicate keys.
            HashSet<string> registryBasePaths = new HashSet<string> {
                @"Software\Classes\" + extension,
            };

            foreach (string registryPath in registryBasePaths)
            {
                // Check Registry.CurrentUser
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryPath))
                {
                    if (key != null)
                    {
                        return false;  // Association exists
                    }
                }
        
                // Check Registry.LocalMachine
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath))
                {
                    if (key != null)
                    {
                        return false;  // Association exists
                    }
                }

                // Check Registry.ClassesRoot
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(registryPath))
                {
                    if (key != null)
                    {
                        return false;  // Association exists
                    }
                }
            }

            return true;  // No association found
        }
        
        private void CreateFileAssociation()
        {
            string extension = ".lc";
            string keyName = @"Software\Classes\" + extension;

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(keyName))
            {
                if (key != null)
                {
                    key.SetValue("", "LiteConfigFile");
                    using (RegistryKey subKey = key.CreateSubKey(@"shell\open\command"))
                    {
                        subKey.SetValue("", "notepad.exe \"%1\"");
                        Logger.Log($"{extension} files are now associated with Notepad.");
                    }
                }
                else
                {
                    Logger.Log("Failed to create file association.");
                }
            }
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
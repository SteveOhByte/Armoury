using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
            string extension = ".lc";
            Dictionary<string, RegistryKey> registryBasePaths = new Dictionary<string, RegistryKey> {
                { @"Software\Classes\" + extension, Registry.CurrentUser },
                { @"Software\Classes\" + extension, Registry.LocalMachine },
                { extension, Registry.ClassesRoot }
            };

            foreach (KeyValuePair<string, RegistryKey> item in registryBasePaths)
            {
                using (RegistryKey key = item.Value.OpenSubKey(item.Key))
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
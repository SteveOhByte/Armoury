using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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
        public static bool checkForUpdates = true;
        public static bool autoUpdate = true;
        public static string defaultLoadout = string.Empty;
        public static Keys menuKey = Keys.I;
        public static Keys menuModifier = Keys.Shift;

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
                LC.WriteValue(config, "Check for Updates", checkForUpdates);
                LC.WriteValue(config, "Auto Update", autoUpdate);
                LC.WriteValue(config, "Default Loadout", defaultLoadout);
                LC.WriteValue(config, "Open Menu", menuKey);
                LC.WriteValue(config, "Open Menu Modifier", menuModifier);
            }
            else
            {
                checkForUpdates = LC.ReadBool(config, "Check for Updates");
                autoUpdate = LC.ReadBool(config, "Auto Update");
                defaultLoadout = LC.ReadString(config, "Default Loadout");
                menuKey = (Keys)Enum.Parse(typeof(Keys), LC.ReadString(config, "Open Menu"), true);
                menuModifier = (Keys)Enum.Parse(typeof(Keys), LC.ReadString(config, "Open Menu Modifier"), true);
            }
            
            Logger.Log("Armoury initialized at" + DateTime.Now);
            
            // LSPDFR Boilerplate
            Functions.OnOnDutyStateChanged += OnOnDutyStateChangedHandler;
            
            Logger.Log($"Armoury v {version} has been initialized");
            Logger.Log("Go on duty to start using Armoury");
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

                if (checkForUpdates)
                {
                    Logger.Log("Checking for updates...");
                    UpdateChecker updateChecker = new UpdateChecker("https://www.dl.dropboxusercontent.com/scl/fi/neze4glu5vt38ifvl09ep/version.txt?rlkey=rvvoc2qhg3v46flcpxqnbcoc3&dl=0", version);
                    updateChecker.IsUpdateAvailable().ContinueWith(task =>
                    {
                        Logger.Log("Update available: " + task.Result);
                        if (!task.Result) return;
                        
                        if (autoUpdate)
                        {
                            Logger.Log("Updating...");
                            Updater updater = new Updater("https://www.dl.dropboxusercontent.com/scl/fi/5yfgra636cwdx28nwrhcg/Update.zip?rlkey=stwh9jtz42yep9er965i8fow9&dl=0", "plugins/LSPDFR");
                            updater.WasUpdateSuccessful().ContinueWith(t =>
                            {
                                Logger.Log(t.Result ? "Update successful" : "Update failed");
                                Notification.DisplayNotification(TimeSpan.FromSeconds(20),
                                    Color.FromArgb(150, 0, 0, 0),
                                    $"Armoury v {version}", "by SteveOhByte",
                                    t.Result ? "has been updated" : "failed to update");
                            });
                        }
                        else
                        {
                            Logger.Log("Update available notification sent");
                            Notification.DisplayNotification(TimeSpan.FromSeconds(20), Color.FromArgb(150, 0, 0, 0),
                                $"Armoury v {version}", "by SteveOhByte", "is out of date");
                        }
                    });
                }
                
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
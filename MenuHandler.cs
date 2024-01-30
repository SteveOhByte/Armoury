using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using LemonUI;
using LemonUI.Menus;
using Rage;
using Rage.Native;

namespace Armoury
{
    public class MenuHandler
    {
        private ObjectPool pool;
        private NativeMenu menu;
        private List<NativeItem> loadouts;
        private NativeItem replenishItem;
        private NativeItem rifleToggle, shotgunToggle;
        private NativeItem reloadItem;
        private bool menuEnabled = false;
        private bool onDuty = false;

        public void Start()
        {
            SetMenu();

            onDuty = true;
            
            GameFiber.StartNew(delegate
            {
                while (true)
                {
                    GameFiber.Yield();
                    
                    NativeFunction.Natives.DISABLE_PLAYER_VEHICLE_REWARDS(Game.LocalPlayer);

                    if (IsMenuToggleRequested()) menuEnabled = !menuEnabled;
                    if (Game.IsKeyDown(Keys.Back)) menuEnabled = false;

                    // Draw the menu
                    Draw();

                    if (ShouldTerminateLoop()) break;
                }
            });
        }

        private void SetMenu()
        {
            if (menu != null) menu.Visible = false;
            
            pool = new ObjectPool();
            menu = new NativeMenu("Armoury", "Loadouts");
            menu.Clear();
            loadouts = new List<NativeItem>();

            for (int i = 0; i < LoadoutHandler.Instance.loadouts.Count; i++)
                loadouts.Add(new NativeItem($"{i + 1}) {LoadoutHandler.Instance.loadouts[i].name}"));

            foreach (NativeItem item in loadouts)
                menu.Add(item);

            reloadItem = new NativeItem("Reload Loadouts");
            replenishItem = new NativeItem("Restock Ammo, Armour, and Health");
            menu.Add(reloadItem);

            if (LoadoutHandler.Instance.activeLoadout.rifle != null)
            {
                rifleToggle = new NativeItem("Get Rifle");
                menu.Add(rifleToggle);
            }
            
            if (LoadoutHandler.Instance.activeLoadout.shotgun != null)
            {
                shotgunToggle = new NativeItem("Get Shotgun");
                menu.Add(shotgunToggle);
            }
            
            menu.Add(replenishItem);
            menu.UseMouse = false;
            menu.ItemActivated += MenuOnItemActivated;

            pool.Add(menu);
        }

        private void MenuOnItemActivated(object sender, ItemActivatedArgs e)
        {
            if (e.Item.Title.StartsWith("Reload")) // Reloads Loadouts from Files
            {
                menuEnabled = false;
                LoadoutHandler.Instance.LoadLoadouts();
                SetMenu();
            }
            else if (e.Item.Title.StartsWith("Restock")) // Restocks Ammo, Armour, and Health
            {
                LoadoutHandler.Instance.activeLoadout.Activate();
                menuEnabled = false;
            }
            else if (e.Item.Title.StartsWith("Get Rifle")) // Gets Rifle for Ped
            {
                LoadoutHandler.Instance.activeLoadout.GetRifle();
                rifleToggle.Title = "Store Rifle";
                menuEnabled = false;
            }
            else if (e.Item.Title.StartsWith("Store Rifle")) // Stores Rifle for Ped
            {
                LoadoutHandler.Instance.activeLoadout.StoreRifle();
                rifleToggle.Title = "Get Rifle";
                menuEnabled = false;
            }
            else if (e.Item.Title.StartsWith("Get Shotgun")) // Gets Shotgun for Ped
            {
                LoadoutHandler.Instance.activeLoadout.GetShotgun();
                shotgunToggle.Title = "Store Shotgun";
                menuEnabled = false;
            }
            else if (e.Item.Title.StartsWith("Store Shotgun")) // Stores Shotgun for Ped
            {
                LoadoutHandler.Instance.activeLoadout.StoreShotgun();
                shotgunToggle.Title = "Get Shotgun";
                menuEnabled = false;
            }
            else // Activates Loadout
            {
                int i = int.Parse(Regex.Match(e.Item.Title, @"\d+").Value);

                if (i > LoadoutHandler.Instance.loadouts.Count)
                {
                    Main.Logger.Error($"Loadout {i} does not exist");
                    return;
                }

                if (LoadoutHandler.Instance.loadouts.Count > 0)
                {
                    LoadoutHandler.Instance.activeLoadout = LoadoutHandler.Instance.loadouts[i - 1];
                    LoadoutHandler.Instance.loadouts[i - 1].Activate();
                }

                menuEnabled = false;
                SetMenu();
            }
        }

        public void Stop()
        {
            onDuty = false;
        }

        private bool IsMenuToggleRequested()
        {
            switch (Main.menuModifier)
            {
                case Keys.Shift:
                    return Game.IsShiftKeyDownRightNow && Game.IsKeyDown(Main.menuKey);
                case Keys.Control:
                    return Game.IsControlKeyDownRightNow && Game.IsKeyDown(Main.menuKey);
                case Keys.Alt:
                    return Game.IsAltKeyDownRightNow && Game.IsKeyDown(Main.menuKey);
                default:
                    return Game.IsKeyDown(Main.menuKey);
            }
        }

        private bool ShouldTerminateLoop()
        {
            return !onDuty;
        }

        private void Draw()
        {
            replenishItem.Enabled = LoadoutHandler.Instance.activeLoadout != null;
            menu.Visible = menuEnabled;
            if (menuEnabled)
            {
                foreach (NativeItem item in menu.Items)
                {
                    item.Draw();
                }
            }

            replenishItem.Enabled = ProximityCheck();
            rifleToggle.Enabled = ProximityCheck();
            shotgunToggle.Enabled = ProximityCheck();

            foreach (NativeItem item in loadouts) item.Enabled = ProximityCheck();

            pool.Process();
        }
        
        private bool ProximityCheck()
        {
            Vehicle[] nearbyVehicles = Game.LocalPlayer.Character.GetNearbyVehicles(1);
            Vehicle vehicle = nearbyVehicles.Length > 0 ? nearbyVehicles[0] : null;

            return Game.LocalPlayer.Character.IsInAnyPoliceVehicle || (vehicle != null && vehicle.IsPoliceVehicle &&
                   !(vehicle.RearPosition.DistanceTo(Game.LocalPlayer.Character) > 2f));
        }
    }
}
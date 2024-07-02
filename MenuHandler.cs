using System.Linq;
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
        private NativeListItem<string> loadout;
        private NativeItem replenishItem;
        private NativeItem rifleToggle, shotgunToggle, fireExtinguisherToggle;
        private NativeItem reloadItem;
        private bool menuEnabled = false;
        private bool onDuty = false;
        private int activeLoadoutIndex = 0;

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

                    if (IsMenuToggleRequested())
                    {
                        menuEnabled = !menuEnabled;
                        if (menuEnabled) loadout.SelectedIndex = activeLoadoutIndex;
                    }
                    if (Game.IsKeyDown(Keys.Back)) menuEnabled = false;
                    
                    if (IsRifleHotkeyPressed() && ProximityCheck())
                    {
                        if (LoadoutHandler.Instance.activeLoadout.rifle == null) continue;
                        
                        if (rifleToggle.Title.StartsWith("Get"))
                        {
                            LoadoutHandler.Instance.activeLoadout.GetRifle();
                            rifleToggle.Title = "Store Rifle";
                            menuEnabled = false;
                        }
                        else
                        {
                            LoadoutHandler.Instance.activeLoadout.StoreRifle();
                            rifleToggle.Title = "Get Rifle";
                            menuEnabled = false;
                        }
                    }
                    
                    if (IsShotgunHotkeyPressed() && ProximityCheck())
                    {
                        if (LoadoutHandler.Instance.activeLoadout.shotgun == null) continue;
                        
                        if (shotgunToggle.Title.StartsWith("Get"))
                        {
                            LoadoutHandler.Instance.activeLoadout.GetShotgun();
                            shotgunToggle.Title = "Store Shotgun";
                            menuEnabled = false;
                        }
                        else
                        {
                            LoadoutHandler.Instance.activeLoadout.StoreShotgun();
                            shotgunToggle.Title = "Get Shotgun";
                            menuEnabled = false;
                        }
                    }
                    
                    if (IsRestockHotkeyPressed() && ProximityCheck())
                    {
                        LoadoutHandler.Instance.activeLoadout.Activate();
                        menuEnabled = false;
                    }

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
            menu = new NativeMenu("Armoury", "Loadouts")
            {
                HeaderBehavior = HeaderBehavior.AlwaysHide
            };
            menu.Clear();
            string[] loadoutTitles = new string[LoadoutHandler.Instance.loadouts.Count];

            for (int i = 0; i < LoadoutHandler.Instance.loadouts.Count; i++)
                loadoutTitles[i] = LoadoutHandler.Instance.loadouts[i].name;
            
            loadout = new NativeListItem<string>("Loadout", loadoutTitles);

            menu.Add(loadout);

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

            if (LoadoutHandler.Instance.activeLoadout.fireExtinguisher != null)
            {
                fireExtinguisherToggle = new NativeItem("Get Fire Extinguisher");
                menu.Add(fireExtinguisherToggle);
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
            else if (e.Item.Title.StartsWith("Get Fire Extinguisher")) // Gets Fire Extinguisher for Ped
            {
                LoadoutHandler.Instance.activeLoadout.GetFireExtinguisher();
                fireExtinguisherToggle.Title = "Store Fire Extinguisher";
                menuEnabled = false;
            }
            else if (e.Item.Title.StartsWith("Store Fire Extinguisher")) // Stores Fire Extinguisher for Ped
            {
                LoadoutHandler.Instance.activeLoadout.StoreFireExtinguisher();
                fireExtinguisherToggle.Title = "Get Fire Extinguisher";
                menuEnabled = false;
            }
            else // Activates Loadout
            {
                foreach (Loadout instanceLoadout in LoadoutHandler.Instance.loadouts
                             .Where(instanceLoadout => instanceLoadout.name == loadout.SelectedItem && LoadoutHandler.Instance.loadouts.Count > 0))
                {
                    LoadoutHandler.Instance.activeLoadout = LoadoutHandler.Instance.loadouts[LoadoutHandler.Instance.loadouts.IndexOf(instanceLoadout)];
                    LoadoutHandler.Instance.loadouts[LoadoutHandler.Instance.loadouts.IndexOf(instanceLoadout)].Activate();

                    activeLoadoutIndex = loadout.SelectedIndex;
                    
                    menuEnabled = false;
                    SetMenu();
                    return;
                }
                
                Main.Logger.Error($"Loadout \"{loadout.SelectedItem}\" does not exist");
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

        private bool IsRifleHotkeyPressed()
        {
            switch (Main.rifleHotkeyModifier)
            {
                case Keys.Shift:
                    return Game.IsShiftKeyDownRightNow && Game.IsKeyDown(Main.rifleHotkey);
                case Keys.Control:
                    return Game.IsControlKeyDownRightNow && Game.IsKeyDown(Main.rifleHotkey);
                case Keys.Alt:
                    return Game.IsAltKeyDownRightNow && Game.IsKeyDown(Main.rifleHotkey);
                default:
                    return Game.IsKeyDown(Main.rifleHotkey);
            }
        }
        
        private bool IsShotgunHotkeyPressed()
        {
            switch (Main.shotgunHotkeyModifier)
            {
                case Keys.Shift:
                    return Game.IsShiftKeyDownRightNow && Game.IsKeyDown(Main.shotgunHotkey);
                case Keys.Control:
                    return Game.IsControlKeyDownRightNow && Game.IsKeyDown(Main.shotgunHotkey);
                case Keys.Alt:
                    return Game.IsAltKeyDownRightNow && Game.IsKeyDown(Main.shotgunHotkey);
                default:
                    return Game.IsKeyDown(Main.shotgunHotkey);
            }
        }
        
        private bool IsRestockHotkeyPressed()
        {
            switch (Main.restockHotkeyModifier)
            {
                case Keys.Shift:
                    return Game.IsShiftKeyDownRightNow && Game.IsKeyDown(Main.restockHotkey);
                case Keys.Control:
                    return Game.IsControlKeyDownRightNow && Game.IsKeyDown(Main.restockHotkey);
                case Keys.Alt:
                    return Game.IsAltKeyDownRightNow && Game.IsKeyDown(Main.restockHotkey);
                default:
                    return Game.IsKeyDown(Main.restockHotkey);
            }
        }

        private bool ShouldTerminateLoop()
        {
            return !onDuty;
        }

        private void Draw()
        {
            if (replenishItem != null)
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
            }

            if (rifleToggle != null) rifleToggle.Enabled = ProximityCheck();
            if (shotgunToggle != null) shotgunToggle.Enabled = ProximityCheck();
            if (fireExtinguisherToggle != null) fireExtinguisherToggle.Enabled = ProximityCheck();

            loadout.Enabled = ProximityCheck();

            pool.Process();
        }
        
        private bool ProximityCheck()
        {
            Vehicle[] nearbyVehicles = Game.LocalPlayer.Character.GetNearbyVehicles(1);
            Vehicle vehicle = nearbyVehicles.Length > 0 ? nearbyVehicles[0] : null;

            if (vehicle == null) return false;
            
            bool inOrBehindPoliceVehicle = (Game.LocalPlayer.Character.IsInAnyPoliceVehicle || (Main.heliEnabled && Game.LocalPlayer.Character.IsInHelicopter)) || (vehicle != null && vehicle.IsPoliceVehicle &&
                   !(vehicle.RearPosition.DistanceTo(Game.LocalPlayer.Character) > 2f));
            
            bool nearFrontDoor = (from door in vehicle.GetDoors() let left = vehicle.LeftPosition let right = vehicle.RightPosition 
                where (door.Index == 0 && left.DistanceTo(Game.LocalPlayer.Character) < 2f) || (door.Index == 1 && right.DistanceTo(Game.LocalPlayer.Character) < 2f) 
                select door).Any(door => door.IsOpen);
            
            return inOrBehindPoliceVehicle || nearFrontDoor;
        }
    }
}
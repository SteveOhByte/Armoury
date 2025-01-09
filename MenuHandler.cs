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
        private NativeItem rifleToggle, shotgunToggle, lessLethalToggle, fireExtinguisherToggle;
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
                    
                    if (IsRifleHotkeyPressed())
                    {
                        (bool IsInVehicle, bool IsNearDoor, bool IsBehindVehicle) result = ProximityCheck();
                        if (!result.IsBehindVehicle && !result.IsNearDoor && !result.IsInVehicle) continue;
                        if (LoadoutHandler.Instance.ActiveLoadout.Rifle == null) continue;
                        
                        if (rifleToggle.Title.StartsWith("Get"))
                        {
                            LoadoutHandler.Instance.ActiveLoadout.GetRifle(result.IsBehindVehicle);
                            rifleToggle.Title = $"Store {LoadoutHandler.Instance.ActiveLoadout.RifleTitle}";
                        }
                        else
                        {
                            LoadoutHandler.Instance.ActiveLoadout.StoreRifle(result.IsBehindVehicle);
                            rifleToggle.Title = $"Get {LoadoutHandler.Instance.ActiveLoadout.RifleTitle}";
                        }

                        menuEnabled = false;
                    }
                    
                    if (IsShotgunHotkeyPressed())
                    {
                        (bool IsInVehicle, bool IsNearDoor, bool IsBehindVehicle) result = ProximityCheck();
                        if (!result.IsBehindVehicle && !result.IsNearDoor && !result.IsInVehicle) continue;
                        if (LoadoutHandler.Instance.ActiveLoadout.Shotgun == null) continue;
                        
                        if (shotgunToggle.Title.StartsWith("Get"))
                        {
                            LoadoutHandler.Instance.ActiveLoadout.GetShotgun(result.IsBehindVehicle);
                            shotgunToggle.Title = $"Store {LoadoutHandler.Instance.ActiveLoadout.ShotgunTitle}";
                        }
                        else
                        {
                            LoadoutHandler.Instance.ActiveLoadout.StoreShotgun(result.IsBehindVehicle);
                            shotgunToggle.Title = $"Get {LoadoutHandler.Instance.ActiveLoadout.ShotgunTitle}";
                        }

                        menuEnabled = false;
                    }
                    
                    if (IsLessLethalHotkeyPressed())
                    {
                        (bool IsInVehicle, bool IsNearDoor, bool IsBehindVehicle) result = ProximityCheck();
                        if (!result.IsBehindVehicle && !result.IsNearDoor && !result.IsInVehicle) continue;
                        if (LoadoutHandler.Instance.ActiveLoadout.LessLethal == null) continue;
                        
                        if (lessLethalToggle.Title.StartsWith("Get"))
                        {
                            LoadoutHandler.Instance.ActiveLoadout.GetLessLethal(result.IsBehindVehicle);
                            lessLethalToggle.Title = $"Store {LoadoutHandler.Instance.ActiveLoadout.LessLethalTitle}";
                        }
                        else
                        {
                            LoadoutHandler.Instance.ActiveLoadout.StoreLessLethal(result.IsBehindVehicle);
                            lessLethalToggle.Title = $"Get {LoadoutHandler.Instance.ActiveLoadout.LessLethalTitle}";
                        }

                        menuEnabled = false;
                    }
                    
                    if (IsRestockHotkeyPressed() && ProximityCheck(true))
                    {
                        LoadoutHandler.Instance.ActiveLoadout.Activate();
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
            string[] loadoutTitles = new string[LoadoutHandler.Instance.Loadouts.Count];

            for (int i = 0; i < LoadoutHandler.Instance.Loadouts.Count; i++)
                loadoutTitles[i] = LoadoutHandler.Instance.Loadouts[i].Name;
            
            loadout = new NativeListItem<string>("Loadout", loadoutTitles);

            menu.Add(loadout);

            reloadItem = new NativeItem("Reload Loadouts");
            replenishItem = new NativeItem("Restock Ammo, Armour, and Health");
            menu.Add(reloadItem);

            if (LoadoutHandler.Instance.ActiveLoadout.Rifle != null)
            {
                rifleToggle = new NativeItem($"Get {LoadoutHandler.Instance.ActiveLoadout.RifleTitle}");
                menu.Add(rifleToggle);
            }
            
            if (LoadoutHandler.Instance.ActiveLoadout.Shotgun != null)
            {
                shotgunToggle = new NativeItem($"Get {LoadoutHandler.Instance.ActiveLoadout.ShotgunTitle}");
                menu.Add(shotgunToggle);
            }
            
            if (LoadoutHandler.Instance.ActiveLoadout.LessLethal != null)
            {
                lessLethalToggle = new NativeItem($"Get {LoadoutHandler.Instance.ActiveLoadout.LessLethalTitle}");
                menu.Add(lessLethalToggle);
            }

            if (LoadoutHandler.Instance.ActiveLoadout.FireExtinguisher != null)
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
            (bool IsInVehicle, bool IsNearDoor, bool IsBehindVehicle) result = ProximityCheck();
            
            if (e.Item.Title.StartsWith("Reload")) // Reloads Loadouts from Files
            {
                menuEnabled = false;
                LoadoutHandler.Instance.LoadLoadouts();
                SetMenu();
            }
            else if (e.Item.Title.StartsWith("Restock")) // Restocks Ammo, Armour, and Health
            {
                LoadoutHandler.Instance.ActiveLoadout.Activate();
                menuEnabled = false;
            }
            else if (e.Item.Title.StartsWith($"Get {LoadoutHandler.Instance.ActiveLoadout.RifleTitle}")) // Gets Rifle for Ped
            {
                LoadoutHandler.Instance.ActiveLoadout.GetRifle(result.IsBehindVehicle);
                rifleToggle.Title = $"Store {LoadoutHandler.Instance.ActiveLoadout.RifleTitle}";
                menuEnabled = false;
            }
            else if (e.Item.Title.StartsWith($"Store {LoadoutHandler.Instance.ActiveLoadout.RifleTitle}")) // Stores Rifle for Ped
            {
                LoadoutHandler.Instance.ActiveLoadout.StoreRifle(result.IsBehindVehicle);
                rifleToggle.Title = $"Get {LoadoutHandler.Instance.ActiveLoadout.RifleTitle}";
                menuEnabled = false;
            }
            else if (e.Item.Title.StartsWith($"Get {LoadoutHandler.Instance.ActiveLoadout.ShotgunTitle}")) // Gets Shotgun for Ped
            {
                LoadoutHandler.Instance.ActiveLoadout.GetShotgun(result.IsBehindVehicle);
                shotgunToggle.Title = $"Store {LoadoutHandler.Instance.ActiveLoadout.ShotgunTitle}";
                menuEnabled = false;
            }
            else if (e.Item.Title.StartsWith($"Store {LoadoutHandler.Instance.ActiveLoadout.ShotgunTitle}")) // Stores Shotgun for Ped
            {
                LoadoutHandler.Instance.ActiveLoadout.StoreShotgun(result.IsBehindVehicle);
                shotgunToggle.Title = $"Get {LoadoutHandler.Instance.ActiveLoadout.ShotgunTitle}";
                menuEnabled = false;
            }
            else if (e.Item.Title.StartsWith($"Get {LoadoutHandler.Instance.ActiveLoadout.LessLethalTitle}")) // Gets Less Lethal for Ped
            {
                LoadoutHandler.Instance.ActiveLoadout.GetLessLethal(result.IsBehindVehicle);
                lessLethalToggle.Title = $"Store {LoadoutHandler.Instance.ActiveLoadout.LessLethalTitle}";
                menuEnabled = false;
            }
            else if (e.Item.Title.StartsWith($"Store {LoadoutHandler.Instance.ActiveLoadout.LessLethalTitle}")) // Stores Less Lethal for Ped
            {
                LoadoutHandler.Instance.ActiveLoadout.StoreLessLethal(result.IsBehindVehicle);
                lessLethalToggle.Title = $"Get {LoadoutHandler.Instance.ActiveLoadout.LessLethalTitle}";
                menuEnabled = false;
            }
            else if (e.Item.Title.StartsWith("Get Fire Extinguisher")) // Gets Fire Extinguisher for Ped
            {
                LoadoutHandler.Instance.ActiveLoadout.GetFireExtinguisher();
                fireExtinguisherToggle.Title = "Store Fire Extinguisher";
                menuEnabled = false;
            }
            else if (e.Item.Title.StartsWith("Store Fire Extinguisher")) // Stores Fire Extinguisher for Ped
            {
                LoadoutHandler.Instance.ActiveLoadout.StoreFireExtinguisher();
                fireExtinguisherToggle.Title = "Get Fire Extinguisher";
                menuEnabled = false;
            }
            else // Activates Loadout
            {
                foreach (Loadout instanceLoadout in LoadoutHandler.Instance.Loadouts
                             .Where(instanceLoadout => instanceLoadout.Name == loadout.SelectedItem && LoadoutHandler.Instance.Loadouts.Count > 0))
                {
                    LoadoutHandler.Instance.ActiveLoadout = LoadoutHandler.Instance.Loadouts[LoadoutHandler.Instance.Loadouts.IndexOf(instanceLoadout)];
                    LoadoutHandler.Instance.Loadouts[LoadoutHandler.Instance.Loadouts.IndexOf(instanceLoadout)].Activate();

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
            switch (Main.MenuModifier)
            {
                case Keys.Shift:
                    return Game.IsShiftKeyDownRightNow && Game.IsKeyDown(Main.MenuKey);
                case Keys.Control:
                    return Game.IsControlKeyDownRightNow && Game.IsKeyDown(Main.MenuKey);
                case Keys.Alt:
                    return Game.IsAltKeyDownRightNow && Game.IsKeyDown(Main.MenuKey);
                default:
                    return Game.IsKeyDown(Main.MenuKey);
            }
        }

        private bool IsRifleHotkeyPressed()
        {
            switch (Main.RifleHotkeyModifier)
            {
                case Keys.Shift:
                    return Game.IsShiftKeyDownRightNow && Game.IsKeyDown(Main.RifleHotkey);
                case Keys.Control:
                    return Game.IsControlKeyDownRightNow && Game.IsKeyDown(Main.RifleHotkey);
                case Keys.Alt:
                    return Game.IsAltKeyDownRightNow && Game.IsKeyDown(Main.RifleHotkey);
                default:
                    return Game.IsKeyDown(Main.RifleHotkey);
            }
        }
        
        private bool IsShotgunHotkeyPressed()
        {
            switch (Main.ShotgunHotkeyModifier)
            {
                case Keys.Shift:
                    return Game.IsShiftKeyDownRightNow && Game.IsKeyDown(Main.ShotgunHotkey);
                case Keys.Control:
                    return Game.IsControlKeyDownRightNow && Game.IsKeyDown(Main.ShotgunHotkey);
                case Keys.Alt:
                    return Game.IsAltKeyDownRightNow && Game.IsKeyDown(Main.ShotgunHotkey);
                default:
                    return Game.IsKeyDown(Main.ShotgunHotkey);
            }
        }

        private bool IsLessLethalHotkeyPressed()
        {
            switch (Main.LessLethalHotkeyModifier)
            {
                case Keys.Shift:
                    return Game.IsShiftKeyDownRightNow && Game.IsKeyDown(Main.LessLethalHotkey);
                case Keys.Control:
                    return Game.IsControlKeyDownRightNow && Game.IsKeyDown(Main.LessLethalHotkey);
                case Keys.Alt:
                    return Game.IsAltKeyDownRightNow && Game.IsKeyDown(Main.LessLethalHotkey);
                default:
                    return Game.IsKeyDown(Main.LessLethalHotkey);
            }
        }
        
        private bool IsRestockHotkeyPressed()
        {
            switch (Main.RestockHotkeyModifier)
            {
                case Keys.Shift:
                    return Game.IsShiftKeyDownRightNow && Game.IsKeyDown(Main.RestockHotkey);
                case Keys.Control:
                    return Game.IsControlKeyDownRightNow && Game.IsKeyDown(Main.RestockHotkey);
                case Keys.Alt:
                    return Game.IsAltKeyDownRightNow && Game.IsKeyDown(Main.RestockHotkey);
                default:
                    return Game.IsKeyDown(Main.RestockHotkey);
            }
        }

        private bool ShouldTerminateLoop()
        {
            return !onDuty;
        }

        private void Draw()
        {
            bool proximity = false;
            
            if (replenishItem != null)
            {
                replenishItem.Enabled = LoadoutHandler.Instance.ActiveLoadout != null;
                menu.Visible = menuEnabled;
                if (menuEnabled)
                {
                    foreach (NativeItem item in menu.Items)
                    {
                        item.Draw();
                    }
                }

                proximity = ProximityCheck(true);
                replenishItem.Enabled = proximity;
            }

            proximity = ProximityCheck(true);
            
            if (rifleToggle != null) rifleToggle.Enabled = proximity;
            if (shotgunToggle != null) shotgunToggle.Enabled = proximity;
            if (lessLethalToggle != null) lessLethalToggle.Enabled = proximity;
            if (fireExtinguisherToggle != null) fireExtinguisherToggle.Enabled = proximity;

            loadout.Enabled = proximity;

            pool.Process();
        }

        private bool ProximityCheck(bool singleResult = true)
        {
            (bool IsInVehicle, bool IsNearDoor, bool IsBehindVehicle) result = ProximityCheck();
            return result.IsInVehicle || result.IsNearDoor || result.IsBehindVehicle;
        }
        
        private (bool IsInVehicle, bool IsNearDoor, bool IsBehindVehicle) ProximityCheck()
        {
            // Initialize result tuple
            (bool IsInVehicle, bool IsNearDoor, bool IsBehindVehicle) result = (IsInVehicle: false, IsNearDoor: false, IsBehindVehicle: false);

            // Get the nearest vehicle within 1 unit distance
            Vehicle nearestVehicle = Game.LocalPlayer.Character.GetNearbyVehicles(1).FirstOrDefault();

            // If no nearby vehicle, return the default result
            if (nearestVehicle == null) return result;

            bool isInAddedVehicle = Main.AllowedVehicles
                .Any(vehicle => Game.LocalPlayer.Character.CurrentVehicle != null && Game.LocalPlayer.Character.CurrentVehicle.Model.Name == vehicle);

            // Check if player is in a police vehicle or helicopter
            result.IsInVehicle = isInAddedVehicle || Game.LocalPlayer.Character.IsInAnyPoliceVehicle || 
                                 (Main.HeliEnabled && Game.LocalPlayer.Character.IsInHelicopter);

            // Check if player is behind a police vehicle
            if (nearestVehicle.IsPoliceVehicle || (Main.AllowedVehicles.Count > 0 && Main.AllowedVehicles.Contains(nearestVehicle.Model.Name)))
                result.IsBehindVehicle = nearestVehicle.RearPosition.DistanceTo(Game.LocalPlayer.Character) <= 2f;

            // Check if player is near an open front door
            result.IsNearDoor = nearestVehicle.GetDoors()
                .Where(door => door.Index <= 1) // Only check front doors
                .Any(door => 
                    door.IsOpen && 
                    (door.Index == 0 ? nearestVehicle.LeftPosition : nearestVehicle.RightPosition)
                    .DistanceTo(Game.LocalPlayer.Character) < 2f
                );

            return result;
        }
    }
}
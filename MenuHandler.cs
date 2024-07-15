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
                        if (LoadoutHandler.Instance.activeLoadout.rifle == null) continue;
                        
                        if (rifleToggle.Title.StartsWith("Get"))
                        {
                            LoadoutHandler.Instance.activeLoadout.GetRifle(result.IsBehindVehicle);
                            rifleToggle.Title = $"Store {LoadoutHandler.Instance.activeLoadout.rifleTitle}";
                            menuEnabled = false;
                        }
                        else
                        {
                            LoadoutHandler.Instance.activeLoadout.StoreRifle(result.IsBehindVehicle);
                            rifleToggle.Title = $"Get {LoadoutHandler.Instance.activeLoadout.rifleTitle}";
                            menuEnabled = false;
                        }
                    }
                    
                    if (IsShotgunHotkeyPressed())
                    {
                        (bool IsInVehicle, bool IsNearDoor, bool IsBehindVehicle) result = ProximityCheck();
                        if (!result.IsBehindVehicle && !result.IsNearDoor && !result.IsInVehicle) continue;
                        if (LoadoutHandler.Instance.activeLoadout.shotgun == null) continue;
                        
                        if (shotgunToggle.Title.StartsWith("Get"))
                        {
                            LoadoutHandler.Instance.activeLoadout.GetShotgun(result.IsBehindVehicle);
                            shotgunToggle.Title = $"Store {LoadoutHandler.Instance.activeLoadout.shotgunTitle}";
                            menuEnabled = false;
                        }
                        else
                        {
                            LoadoutHandler.Instance.activeLoadout.StoreShotgun(result.IsBehindVehicle);
                            shotgunToggle.Title = $"Get {LoadoutHandler.Instance.activeLoadout.shotgunTitle}";
                            menuEnabled = false;
                        }
                    }
                    
                    if (IsLessLethalHotkeyPressed())
                    {
                        (bool IsInVehicle, bool IsNearDoor, bool IsBehindVehicle) result = ProximityCheck();
                        if (!result.IsBehindVehicle && !result.IsNearDoor && !result.IsInVehicle) continue;
                        if (LoadoutHandler.Instance.activeLoadout.lessLethal == null) continue;
                        
                        if (lessLethalToggle.Title.StartsWith("Get"))
                        {
                            LoadoutHandler.Instance.activeLoadout.GetLessLethal(result.IsBehindVehicle);
                            lessLethalToggle.Title = $"Store {LoadoutHandler.Instance.activeLoadout.lessLethalTitle}";
                            menuEnabled = false;
                        }
                        else
                        {
                            LoadoutHandler.Instance.activeLoadout.StoreLessLethal(result.IsBehindVehicle);
                            lessLethalToggle.Title = $"Get {LoadoutHandler.Instance.activeLoadout.lessLethalTitle}";
                            menuEnabled = false;
                        }
                    }
                    
                    if (IsRestockHotkeyPressed() && ProximityCheck(true))
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
                rifleToggle = new NativeItem($"Get {LoadoutHandler.Instance.activeLoadout.rifleTitle}");
                menu.Add(rifleToggle);
            }
            
            if (LoadoutHandler.Instance.activeLoadout.shotgun != null)
            {
                shotgunToggle = new NativeItem($"Get {LoadoutHandler.Instance.activeLoadout.shotgunTitle}");
                menu.Add(shotgunToggle);
            }
            
            if (LoadoutHandler.Instance.activeLoadout.lessLethal != null)
            {
                lessLethalToggle = new NativeItem($"Get {LoadoutHandler.Instance.activeLoadout.lessLethalTitle}");
                menu.Add(lessLethalToggle);
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
            (bool IsInVehicle, bool IsNearDoor, bool IsBehindVehicle) result = ProximityCheck();
            
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
            else if (e.Item.Title.StartsWith($"Get {LoadoutHandler.Instance.activeLoadout.rifleTitle}")) // Gets Rifle for Ped
            {
                LoadoutHandler.Instance.activeLoadout.GetRifle(result.IsBehindVehicle);
                rifleToggle.Title = $"Store {LoadoutHandler.Instance.activeLoadout.rifleTitle}";
                menuEnabled = false;
            }
            else if (e.Item.Title.StartsWith($"Store {LoadoutHandler.Instance.activeLoadout.rifleTitle}")) // Stores Rifle for Ped
            {
                LoadoutHandler.Instance.activeLoadout.StoreRifle(result.IsBehindVehicle);
                rifleToggle.Title = $"Get {LoadoutHandler.Instance.activeLoadout.rifleTitle}";
                menuEnabled = false;
            }
            else if (e.Item.Title.StartsWith($"Get {LoadoutHandler.Instance.activeLoadout.shotgunTitle}")) // Gets Shotgun for Ped
            {
                LoadoutHandler.Instance.activeLoadout.GetShotgun(result.IsBehindVehicle);
                shotgunToggle.Title = $"Store {LoadoutHandler.Instance.activeLoadout.shotgunTitle}";
                menuEnabled = false;
            }
            else if (e.Item.Title.StartsWith($"Store {LoadoutHandler.Instance.activeLoadout.shotgunTitle}")) // Stores Shotgun for Ped
            {
                LoadoutHandler.Instance.activeLoadout.StoreShotgun(result.IsBehindVehicle);
                shotgunToggle.Title = $"Get {LoadoutHandler.Instance.activeLoadout.shotgunTitle}";
                menuEnabled = false;
            }
            else if (e.Item.Title.StartsWith($"Get {LoadoutHandler.Instance.activeLoadout.lessLethalTitle}")) // Gets Less Lethal for Ped
            {
                LoadoutHandler.Instance.activeLoadout.GetLessLethal(result.IsBehindVehicle);
                lessLethalToggle.Title = $"Store {LoadoutHandler.Instance.activeLoadout.lessLethalTitle}";
                menuEnabled = false;
            }
            else if (e.Item.Title.StartsWith($"Store {LoadoutHandler.Instance.activeLoadout.lessLethalTitle}")) // Stores Less Lethal for Ped
            {
                LoadoutHandler.Instance.activeLoadout.StoreLessLethal(result.IsBehindVehicle);
                lessLethalToggle.Title = $"Get {LoadoutHandler.Instance.activeLoadout.lessLethalTitle}";
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

        private bool IsLessLethalHotkeyPressed()
        {
            switch (Main.lessLethalHotkeyModifier)
            {
                case Keys.Shift:
                    return Game.IsShiftKeyDownRightNow && Game.IsKeyDown(Main.lessLethalHotkey);
                case Keys.Control:
                    return Game.IsControlKeyDownRightNow && Game.IsKeyDown(Main.lessLethalHotkey);
                case Keys.Alt:
                    return Game.IsAltKeyDownRightNow && Game.IsKeyDown(Main.lessLethalHotkey);
                default:
                    return Game.IsKeyDown(Main.lessLethalHotkey);
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

                replenishItem.Enabled = ProximityCheck(true);
            }

            if (rifleToggle != null) rifleToggle.Enabled = ProximityCheck(true);
            if (shotgunToggle != null) shotgunToggle.Enabled = ProximityCheck(true);
            if (lessLethalToggle != null) lessLethalToggle.Enabled = ProximityCheck(true);
            if (fireExtinguisherToggle != null) fireExtinguisherToggle.Enabled = ProximityCheck(true);

            loadout.Enabled = ProximityCheck(true);

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

            bool isInAddedVehicle = Main.allowedVehicles
                .Any(vehicle => Game.LocalPlayer.Character.CurrentVehicle != null && Game.LocalPlayer.Character.CurrentVehicle.Model.Name == vehicle);

            // Check if player is in a police vehicle or helicopter
            result.IsInVehicle = isInAddedVehicle || Game.LocalPlayer.Character.IsInAnyPoliceVehicle || 
                                 (Main.heliEnabled && Game.LocalPlayer.Character.IsInHelicopter);

            // Check if player is behind a police vehicle
            if (nearestVehicle.IsPoliceVehicle || (Main.allowedVehicles.Count > 0 && Main.allowedVehicles.Contains(nearestVehicle.Model.Name)))
            {
                result.IsBehindVehicle = nearestVehicle.RearPosition.DistanceTo(Game.LocalPlayer.Character) <= 2f;
            }

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
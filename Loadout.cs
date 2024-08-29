using System;
using System.Collections.Generic;
using System.Linq;
using Rage;
using Rage.Native;

namespace Armoury
{
    public class Loadout
    {
        public string name;
        public List<Weapon> weapons;
        public bool armour;
        public bool medkit;
        public Weapon rifle = null;
        public Weapon shotgun = null;
        public Weapon lessLethal = null;
        public Weapon fireExtinguisher = null;
        public string rifleTitle = "Rifle";
        public string shotgunTitle = "Shotgun";
        public string lessLethalTitle = "Less Lethal";

        private const int MaxArmour = 100;
        
        private bool rifleEquipped = false;
        private bool shotgunEquipped = false;
        private bool lessLethalEquipped = false;
        private bool fireExtinguisherEquipped = false;

        public void Activate()
        {
            if (Game.LocalPlayer.Character.IsInAnyPoliceVehicle || (Main.heliEnabled && Game.LocalPlayer.Character.IsInHelicopter))
            {
                RunLoadout();
                return;
            }
            
            Vehicle vehicle = Game.LocalPlayer.Character.GetNearbyVehicles(1)[0];

            if (vehicle != null && (vehicle.IsPoliceVehicle || (Main.heliEnabled && Game.LocalPlayer.Character.IsInHelicopter)))
            {
                bool isPlayerNearTrunk = vehicle.RearPosition.DistanceTo(Game.LocalPlayer.Character) <= 2f;
                
                bool isPlayerNearDoor = (from door in vehicle.GetDoors() let left = vehicle.LeftPosition let right = vehicle.RightPosition 
                    where (door.Index == 0 && left.DistanceTo(Game.LocalPlayer.Character) < 2f) || (door.Index == 1 && right.DistanceTo(Game.LocalPlayer.Character) < 2f) 
                    select door).Any(door => door.IsOpen);

                if (isPlayerNearDoor)
                {
                    RunLoadout();
                    return;
                }
                
                if (!isPlayerNearTrunk) return;
            }

            AnimateTrunkAction(vehicle, RunLoadout);
        }

        private void RunLoadout()
        {
            Game.LocalPlayer.Character.Inventory.Weapons.Clear();
            
            foreach (Weapon weapon in weapons)
            {
                WeaponAsset asset = weapon.asset;
                if (!asset.IsValid)
                {
                    Main.Logger.Error($"Invalid weapon hash \"{weapon.name}\" in loadout \"{name}\"");
                    continue;
                }
                
                if (weapon == rifle || weapon == shotgun || weapon == lessLethal) continue;

                Game.LocalPlayer.Character.Inventory.GiveNewWeapon(asset, weapon.ammo, false);
                
                try // Set tint
                {
                    NativeFunction.Natives.SET_PED_WEAPON_TINT_INDEX(Game.LocalPlayer.Character, asset.Hash,
                        weapon.tintIndex);
                }
                catch
                {
                    Main.Logger.Error(
                        $"Failed to set tint index for weapon \"{weapon.name}\" in loadout \"{name}\"");
                }
                
                foreach (string component in weapon.components)
                    Game.LocalPlayer.Character.Inventory.AddComponentToWeapon(asset, component);
            }
            
            if (armour) Game.LocalPlayer.Character.Armor = MaxArmour;
            if (medkit)
            {
                Game.LocalPlayer.Character.Health = Game.LocalPlayer.Character.MaxHealth;
                Game.LocalPlayer.Character.ClearBlood();
            }
            
            if (rifleEquipped) GetRifle(false);
            if (shotgunEquipped) GetShotgun(false);
            if (lessLethalEquipped) GetLessLethal(false);
            if (fireExtinguisherEquipped) GetFireExtinguisher();
        }

        private void AnimateTrunkAction(Vehicle vehicle, Action action)
        {
            Vector3 pos = vehicle.RearPosition;
            // add an offset to the position so the player doesn't get stuck in the trunk, offset must be based on the vehicle's direction
            pos += vehicle.ForwardVector * -0.5f;
            Game.LocalPlayer.Character.Position = pos;
            Game.LocalPlayer.Character.Face(vehicle);
            vehicle.CollisionIgnoredEntity = Game.LocalPlayer.Character;
            
            GameFiber.StartNew(delegate
            {
                Game.LocalPlayer.Character.Tasks.PlayAnimation((AnimationDictionary) "rcmnigel3_trunk", "out_trunk_trevor", 2.5f, AnimationFlags.None);
                GameFiber.Sleep(1000);
                try
                {
                    vehicle.GetDoors()[5].Open(false, false);
                }
                catch (IndexOutOfRangeException)
                {
                    int lastIndex = vehicle.GetDoors().Length - 1;
                    for (int i = lastIndex; i >= 0; i--)
                    {
                        try
                        {
                            vehicle.GetDoors()[i].Open(false, false);
                            break;
                        }
                        catch (IndexOutOfRangeException)
                        {
                            Main.Logger.Error($"Failed to open trunk door at index {i}");
                        }
                    }
                }
                GameFiber.Sleep(1000);
                Game.LocalPlayer.Character.Tasks.PlayAnimation((AnimationDictionary) "rcmepsilonism8", "bag_handler_close_trunk_walk_left", 1f, AnimationFlags.None);
                GameFiber.Sleep(1250);
                action?.Invoke();
                GameFiber.Sleep(800);
                vehicle.GetDoors()[vehicle.GetDoors().Length - 1].Close(false);
                GameFiber.Sleep(750);
                Game.LocalPlayer.Character.Tasks.ClearImmediately();
                vehicle.CollisionIgnoredEntity = null;
            });
        }

        public void GetRifle(bool fromTrunk)
        {
            if (rifle == null) return;
            
            WeaponAsset asset = rifle.asset;
            if (!asset.IsValid)
            {
                Main.Logger.Error($"Invalid weapon hash \"{rifle.name}\" in loadout \"{name}\"");
                return;
            }

            if (rifle.ammo == -1)
            {
                Main.Logger.Error($"Invalid ammo value for weapon \"{rifle.name}\" in loadout \"{name}\"");
                return;
            }

            Action action = () =>
            {
                Game.LocalPlayer.Character.Inventory.GiveNewWeapon(asset, rifle.ammo, true);

                try // Set tint
                {
                    NativeFunction.Natives.SET_PED_WEAPON_TINT_INDEX(Game.LocalPlayer.Character, asset.Hash,
                        rifle.tintIndex);
                }
                catch
                {
                    Main.Logger.Error(
                        $"Failed to set tint index for weapon \"{rifle.name}\" in loadout \"{name}\"");
                }
                
                foreach (string component in rifle.components)
                    Game.LocalPlayer.Character.Inventory.AddComponentToWeapon(asset, component);

                rifleEquipped = true;
            };
            
            if (fromTrunk)
                AnimateTrunkAction(Game.LocalPlayer.Character.GetNearbyVehicles(1)[0], action);
            else
                action.Invoke();
        }
        
        public void StoreRifle(bool inTrunk)
        {
            if (rifle == null) return;
            
            WeaponAsset asset = rifle.asset;
            if (!asset.IsValid)
            {
                Main.Logger.Error($"Invalid weapon hash \"{rifle.name}\" in loadout \"{name}\"");
                return;
            }

            if (rifle.ammo == -1)
            {
                Main.Logger.Error($"Invalid ammo value for weapon \"{rifle.name}\" in loadout \"{name}\"");
                return;
            }

            Action action = () =>
            {
                foreach (WeaponDescriptor descriptor in Game.LocalPlayer.Character.Inventory.Weapons)
                {
                    if (descriptor.Asset == asset)
                        descriptor.Ammo = 0;
                }
                Game.LocalPlayer.Character.Inventory.Weapons.Remove(asset);

                rifleEquipped = false;
            };
            
            if (inTrunk)
                AnimateTrunkAction(Game.LocalPlayer.Character.GetNearbyVehicles(1)[0], action);
            else
                action.Invoke();
        }
        
        public void GetShotgun(bool fromTrunk)
        {
            if (shotgun == null) return;
            
            WeaponAsset asset = shotgun.asset;
            if (!asset.IsValid)
            {
                Main.Logger.Error($"Invalid weapon hash \"{shotgun.name}\" in loadout \"{name}\"");
                return;
            }

            if (shotgun.ammo == -1)
            {
                Main.Logger.Error($"Invalid ammo value for weapon \"{shotgun.name}\" in loadout \"{name}\"");
                return;
            }

            Action action = () =>
            {
                Game.LocalPlayer.Character.Inventory.GiveNewWeapon(asset, shotgun.ammo, true);

                try // Set tint
                {
                    NativeFunction.Natives.SET_PED_WEAPON_TINT_INDEX(Game.LocalPlayer.Character, asset.Hash,
                        shotgun.tintIndex);
                }
                catch
                {
                    Main.Logger.Error(
                        $"Failed to set tint index for weapon \"{shotgun.name}\" in loadout \"{name}\"");
                }
                
                foreach (string component in shotgun.components)
                    Game.LocalPlayer.Character.Inventory.AddComponentToWeapon(asset, component);

                shotgunEquipped = true;
            };
            
            if (fromTrunk)
                AnimateTrunkAction(Game.LocalPlayer.Character.GetNearbyVehicles(1)[0], action);
            else
                action.Invoke();
        }
        
        public void StoreShotgun(bool inTrunk)
        {
            if (shotgun == null) return;
            
            WeaponAsset asset = shotgun.asset;
            if (!asset.IsValid)
            {
                Main.Logger.Error($"Invalid weapon hash \"{shotgun.name}\" in loadout \"{name}\"");
                return;
            }

            if (shotgun.ammo == -1)
            {
                Main.Logger.Error($"Invalid ammo value for weapon \"{shotgun.name}\" in loadout \"{name}\"");
                return;
            }

            Action action = () =>
            {
                foreach (WeaponDescriptor descriptor in Game.LocalPlayer.Character.Inventory.Weapons)
                {
                    if (descriptor.Asset == asset)
                        descriptor.Ammo = 0;
                }
                
                Game.LocalPlayer.Character.Inventory.Weapons.Remove(asset);

                shotgunEquipped = false;
            };

            if (inTrunk)
                AnimateTrunkAction(Game.LocalPlayer.Character.GetNearbyVehicles(1)[0], action);
            else
                action.Invoke();
        }

        public void GetLessLethal(bool fromTrunk)
        {
            if (lessLethal == null) return;
            
            WeaponAsset asset = lessLethal.asset;
            if (!asset.IsValid)
            {
                Main.Logger.Error($"Invalid weapon hash \"{lessLethal.name}\" in loadout \"{name}\"");
                return;
            }

            if (lessLethal.ammo == -1)
            {
                Main.Logger.Error($"Invalid ammo value for weapon \"{lessLethal.name}\" in loadout \"{name}\"");
                return;
            }

            Action action = () =>
            {
                Game.LocalPlayer.Character.Inventory.GiveNewWeapon(asset, lessLethal.ammo, true);

                try // Set tint
                {
                    NativeFunction.Natives.SET_PED_WEAPON_TINT_INDEX(Game.LocalPlayer.Character, asset.Hash,
                        lessLethal.tintIndex);
                }
                catch
                {
                    Main.Logger.Error(
                        $"Failed to set tint index for weapon \"{lessLethal.name}\" in loadout \"{name}\"");
                }
                
                foreach (string component in lessLethal.components)
                    Game.LocalPlayer.Character.Inventory.AddComponentToWeapon(asset, component);

                lessLethalEquipped = true;
            };
            
            if (fromTrunk)
                AnimateTrunkAction(Game.LocalPlayer.Character.GetNearbyVehicles(1)[0], action);
            else
                action.Invoke();
        }

        public void StoreLessLethal(bool inTrunk)
        {
            if (lessLethal == null) return;
            
            WeaponAsset asset = lessLethal.asset;
            if (!asset.IsValid)
            {
                Main.Logger.Error($"Invalid weapon hash \"{lessLethal.name}\" in loadout \"{name}\"");
                return;
            }

            if (lessLethal.ammo == -1)
            {
                Main.Logger.Error($"Invalid ammo value for weapon \"{lessLethal.name}\" in loadout \"{name}\"");
                return;
            }

            Action action = () =>
            {
                foreach (WeaponDescriptor descriptor in Game.LocalPlayer.Character.Inventory.Weapons)
                {
                    if (descriptor.Asset == asset)
                        descriptor.Ammo = 0;
                }
                
                Game.LocalPlayer.Character.Inventory.Weapons.Remove(asset);

                lessLethalEquipped = false;
            };
            
            if (inTrunk)
                AnimateTrunkAction(Game.LocalPlayer.Character.GetNearbyVehicles(1)[0], action);
            else
                action.Invoke();
        }

        public void GetFireExtinguisher()
        {
            if (fireExtinguisher == null) return;
            
            WeaponAsset asset = fireExtinguisher.asset;
            if (!asset.IsValid)
            {
                Main.Logger.Error($"Invalid weapon hash \"{fireExtinguisher.name}\" in loadout \"{name}\"");
                return;
            }

            Game.LocalPlayer.Character.Inventory.GiveNewWeapon(asset, fireExtinguisher.ammo, true);
            
            fireExtinguisherEquipped = true;
        }
        
        public void StoreFireExtinguisher()
        {
            if (fireExtinguisher == null) return;
            
            WeaponAsset asset = fireExtinguisher.asset;
            if (!asset.IsValid)
            {
                Main.Logger.Error($"Invalid weapon hash \"{fireExtinguisher.name}\" in loadout \"{name}\"");
                return;
            }

            Game.LocalPlayer.Character.Inventory.Weapons.Remove(asset);
            
            fireExtinguisherEquipped = false;
        }
    }
}
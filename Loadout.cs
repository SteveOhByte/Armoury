using System;
using System.Collections.Generic;
using System.Linq;
using Rage;
using Rage.Native;

namespace Armoury
{
    public class Loadout
    {
        public string Name;
        public List<Weapon> Weapons;
        public bool Armour;
        public bool Medkit;
        public Weapon Rifle = null;
        public Weapon Shotgun = null;
        public Weapon LessLethal = null;
        public Weapon FireExtinguisher = null;
        public string RifleTitle = "Rifle";
        public string ShotgunTitle = "Shotgun";
        public string LessLethalTitle = "Less Lethal";

        private const int maxArmour = 100;
        
        private bool rifleEquipped = false;
        private bool shotgunEquipped = false;
        private bool lessLethalEquipped = false;
        private bool fireExtinguisherEquipped = false;

        public void Activate()
        {
            if (Game.LocalPlayer.Character.IsInAnyPoliceVehicle || (Main.HeliEnabled && Game.LocalPlayer.Character.IsInHelicopter))
            {
                RunLoadout();
                return;
            }
            
            Vehicle vehicle = Game.LocalPlayer.Character.GetNearbyVehicles(1)[0];

            if (vehicle != null && (vehicle.IsPoliceVehicle || (Main.HeliEnabled && Game.LocalPlayer.Character.IsInHelicopter)))
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
            
            foreach (Weapon weapon in Weapons)
            {
                WeaponAsset asset = weapon.Asset;
                if (!asset.IsValid)
                {
                    Main.Logger.Error($"Invalid weapon hash \"{weapon.Name}\" in loadout \"{Name}\"");
                    continue;
                }
                
                if (weapon == Rifle || weapon == Shotgun || weapon == LessLethal) continue;

                Game.LocalPlayer.Character.Inventory.GiveNewWeapon(asset, weapon.Ammo, false);
                
                try // Set tint
                {
                    NativeFunction.Natives.SET_PED_WEAPON_TINT_INDEX(Game.LocalPlayer.Character, asset.Hash,
                        weapon.TintIndex);
                }
                catch
                {
                    Main.Logger.Error(
                        $"Failed to set tint index for weapon \"{weapon.Name}\" in loadout \"{Name}\"");
                }
                
                foreach (string component in weapon.Components)
                    Game.LocalPlayer.Character.Inventory.AddComponentToWeapon(asset, component);
            }
            
            if (Armour) Game.LocalPlayer.Character.Armor = maxArmour;
            if (Medkit)
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
            if (Rifle == null) return;
            
            WeaponAsset asset = Rifle.Asset;
            if (!asset.IsValid)
            {
                Main.Logger.Error($"Invalid weapon hash \"{Rifle.Name}\" in loadout \"{Name}\"");
                return;
            }

            if (Rifle.Ammo == -1)
            {
                Main.Logger.Error($"Invalid ammo value for weapon \"{Rifle.Name}\" in loadout \"{Name}\"");
                return;
            }

            Action action = () =>
            {
                GetWeapon(asset, Rifle);

                rifleEquipped = true;
            };
            
            if (fromTrunk)
                AnimateTrunkAction(Game.LocalPlayer.Character.GetNearbyVehicles(1)[0], action);
            else
                action.Invoke();
        }

        public void StoreRifle(bool inTrunk)
        {
            if (Rifle == null) return;
            
            WeaponAsset asset = Rifle.Asset;
            if (!asset.IsValid)
            {
                Main.Logger.Error($"Invalid weapon hash \"{Rifle.Name}\" in loadout \"{Name}\"");
                return;
            }

            if (Rifle.Ammo == -1)
            {
                Main.Logger.Error($"Invalid ammo value for weapon \"{Rifle.Name}\" in loadout \"{Name}\"");
                return;
            }

            Action action = () =>
            {
                rifleEquipped = false;
                RunLoadout();
            };
            
            if (inTrunk)
                AnimateTrunkAction(Game.LocalPlayer.Character.GetNearbyVehicles(1)[0], action);
            else
                action.Invoke();
        }

        public void GetShotgun(bool fromTrunk)
        {
            if (Shotgun == null) return;
            
            WeaponAsset asset = Shotgun.Asset;
            if (!asset.IsValid)
            {
                Main.Logger.Error($"Invalid weapon hash \"{Shotgun.Name}\" in loadout \"{Name}\"");
                return;
            }

            if (Shotgun.Ammo == -1)
            {
                Main.Logger.Error($"Invalid ammo value for weapon \"{Shotgun.Name}\" in loadout \"{Name}\"");
                return;
            }

            Action action = () =>
            {
                GetWeapon(asset, Shotgun);

                shotgunEquipped = true;
            };
            
            if (fromTrunk)
                AnimateTrunkAction(Game.LocalPlayer.Character.GetNearbyVehicles(1)[0], action);
            else
                action.Invoke();
        }
        
        public void StoreShotgun(bool inTrunk)
        {
            if (Shotgun == null) return;
            
            WeaponAsset asset = Shotgun.Asset;
            if (!asset.IsValid)
            {
                Main.Logger.Error($"Invalid weapon hash \"{Shotgun.Name}\" in loadout \"{Name}\"");
                return;
            }

            if (Shotgun.Ammo == -1)
            {
                Main.Logger.Error($"Invalid ammo value for weapon \"{Shotgun.Name}\" in loadout \"{Name}\"");
                return;
            }

            Action action = () =>
            {
                shotgunEquipped = false;
                RunLoadout();
            };

            if (inTrunk)
                AnimateTrunkAction(Game.LocalPlayer.Character.GetNearbyVehicles(1)[0], action);
            else
                action.Invoke();
        }

        public void GetLessLethal(bool fromTrunk)
        {
            if (LessLethal == null) return;
            
            WeaponAsset asset = LessLethal.Asset;
            if (!asset.IsValid)
            {
                Main.Logger.Error($"Invalid weapon hash \"{LessLethal.Name}\" in loadout \"{Name}\"");
                return;
            }

            if (LessLethal.Ammo == -1)
            {
                Main.Logger.Error($"Invalid ammo value for weapon \"{LessLethal.Name}\" in loadout \"{Name}\"");
                return;
            }

            Action action = () =>
            {
                GetWeapon(asset, LessLethal);

                lessLethalEquipped = true;
            };
            
            if (fromTrunk)
                AnimateTrunkAction(Game.LocalPlayer.Character.GetNearbyVehicles(1)[0], action);
            else
                action.Invoke();
        }

        public void StoreLessLethal(bool inTrunk)
        {
            if (LessLethal == null) return;
            
            WeaponAsset asset = LessLethal.Asset;
            if (!asset.IsValid)
            {
                Main.Logger.Error($"Invalid weapon hash \"{LessLethal.Name}\" in loadout \"{Name}\"");
                return;
            }

            if (LessLethal.Ammo == -1)
            {
                Main.Logger.Error($"Invalid ammo value for weapon \"{LessLethal.Name}\" in loadout \"{Name}\"");
                return;
            }

            Action action = () =>
            {
                lessLethalEquipped = false;
                RunLoadout();
            };
            
            if (inTrunk)
                AnimateTrunkAction(Game.LocalPlayer.Character.GetNearbyVehicles(1)[0], action);
            else
                action.Invoke();
        }

        public void GetFireExtinguisher()
        {
            if (FireExtinguisher == null) return;
            
            WeaponAsset asset = FireExtinguisher.Asset;
            if (!asset.IsValid)
            {
                Main.Logger.Error($"Invalid weapon hash \"{FireExtinguisher.Name}\" in loadout \"{Name}\"");
                return;
            }

            Game.LocalPlayer.Character.Inventory.GiveNewWeapon(asset, FireExtinguisher.Ammo, true);
            
            fireExtinguisherEquipped = true;
        }
        
        public void StoreFireExtinguisher()
        {
            if (FireExtinguisher == null) return;
            
            WeaponAsset asset = FireExtinguisher.Asset;
            if (!asset.IsValid)
            {
                Main.Logger.Error($"Invalid weapon hash \"{FireExtinguisher.Name}\" in loadout \"{Name}\"");
                return;
            }

            Game.LocalPlayer.Character.Inventory.Weapons.Remove(asset);
            
            fireExtinguisherEquipped = false;
        }
        
        private void GetWeapon(WeaponAsset weaponAsset, Weapon weapon)
        {
            Game.LocalPlayer.Character.Inventory.GiveNewWeapon(weaponAsset, weapon.Ammo, true);
            
            try // Set tint
            {
                NativeFunction.Natives.SET_PED_WEAPON_TINT_INDEX(Game.LocalPlayer.Character, weaponAsset.Hash,
                    weapon.TintIndex);
            }
            catch
            {
                Main.Logger.Error(
                    $"Failed to set tint index for weapon \"{weapon.Name}\" in loadout \"{Name}\"");
            }
                
            foreach (string component in weapon.Components)
                Game.LocalPlayer.Character.Inventory.AddComponentToWeapon(weaponAsset, component);
        }
    }
}
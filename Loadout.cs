using System.Collections.Generic;
using System.Linq;
using Rage;

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

        private const int MaxArmour = 100;
        
        private bool rifleEquipped = false;
        private bool shotgunEquipped = false;

        public void Activate()
        {
            if (Game.LocalPlayer.Character.IsInAnyPoliceVehicle)
            {
                RunLoadout();
                return;
            }
            
            Vehicle vehicle = Game.LocalPlayer.Character.GetNearbyVehicles(1)[0];

            if (vehicle != null && vehicle.IsPoliceVehicle)
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

            Vector3 pos = vehicle.RearPosition;
            Game.LocalPlayer.Character.Position = pos;
            Game.LocalPlayer.Character.Face(vehicle);
            Game.LocalPlayer.HasControl = false;
            vehicle.CollisionIgnoredEntity = Game.LocalPlayer.Character;
            
            GameFiber.StartNew(delegate
            {
                vehicle.GetDoors()[vehicle.GetDoors().Length - 1].Open(true, true);
                Game.LocalPlayer.Character.Tasks.PlayAnimation("anim@gangops@morgue@table@", "player_search", 1f, AnimationFlags.None);
                GameFiber.Sleep(4000);
                vehicle.GetDoors()[vehicle.GetDoors().Length - 1].Close(false);
                Game.LocalPlayer.HasControl = true;
                vehicle.CollisionIgnoredEntity = null;
                RunLoadout();
            });
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
                
                if (weapon == rifle || weapon == shotgun) continue;

                Game.LocalPlayer.Character.Inventory.GiveNewWeapon(asset, weapon.ammo, false);
                
                foreach (string component in weapon.components)
                    Game.LocalPlayer.Character.Inventory.AddComponentToWeapon(asset, component);
            }
            
            if (armour) Game.LocalPlayer.Character.Armor = MaxArmour;
            if (medkit)
            {
                Game.LocalPlayer.Character.Health = Game.LocalPlayer.Character.MaxHealth;
                Game.LocalPlayer.Character.ClearBlood();
            }
            
            if (rifleEquipped) GetRifle();
            if (shotgunEquipped) GetShotgun();
        }

        public void GetRifle()
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

            Game.LocalPlayer.Character.Inventory.GiveNewWeapon(asset, rifle.ammo, true);
                
            foreach (string component in rifle.components)
                Game.LocalPlayer.Character.Inventory.AddComponentToWeapon(asset, component);
            
            rifleEquipped = true;
        }
        
        public void StoreRifle()
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

            Game.LocalPlayer.Character.Inventory.Weapons.Remove(asset);
            
            rifleEquipped = false;
        }
        
        public void GetShotgun()
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

            Game.LocalPlayer.Character.Inventory.GiveNewWeapon(asset, shotgun.ammo, true);
                
            foreach (string component in shotgun.components)
                Game.LocalPlayer.Character.Inventory.AddComponentToWeapon(asset, component);
            
            shotgunEquipped = true;
        }
        
        public void StoreShotgun()
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

            Game.LocalPlayer.Character.Inventory.Weapons.Remove(asset);
            
            shotgunEquipped = false;
        }
    }
}
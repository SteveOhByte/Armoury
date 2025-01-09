using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteConfig;
using Rage;

namespace Armoury
{
    public class LoadoutHandler
    {
        public static LoadoutHandler Instance => lazy.Value;

        public readonly List<Loadout> Loadouts;
        public Loadout ActiveLoadout;
        
        private static readonly Lazy<LoadoutHandler> lazy = new Lazy<LoadoutHandler>(() => new LoadoutHandler());
        private readonly string folderPath = AppDomain.CurrentDomain.BaseDirectory + @"\plugins\LSPDFR\Armoury\Loadouts";

        public LoadoutHandler()
        {
            Loadouts = new List<Loadout>();

            if (!Directory.Exists(folderPath))
            {
                Main.Logger.Error($"No loadouts were found because \"{folderPath}\" does not exist");
                return;
            }

            LoadLoadouts();
        }

        public void LoadLoadouts()
        {
            Game.LocalPlayer.Character.Inventory.Weapons.Clear();
            Loadouts.Clear();
            
            foreach (string file in Directory.GetFiles(folderPath).Where(path => path.EndsWith(".lc")))
            {
                List<string> hashes = LC.ReadList<string>(file, "Equipment");
                List<Weapon> weapons = new List<Weapon>();
                bool armour = false;
                bool medkit = false;
                bool fireExtinguisherBoolean = false;
                Weapon rifle = null;
                Weapon shotgun = null;
                Weapon lessLethal = null;
                Weapon fireExtinguisher = null;
                string rifleTitle = "Rifle";
                string shotgunTitle = "Shotgun";
                string lessLethalTitle = "Less Lethal";
                
                foreach (string hash in hashes)
                {
                    if (hash == "weapon_fireextinguisher") continue;
                    
                    WeaponAsset asset = new WeaponAsset(hash);
                    if (!asset.IsValid)
                    {
                        Main.Logger.Error($"Invalid weapon hash \"{hash}\" in loadout file \"{file}\"");
                        continue;
                    }

                    short ammo = 120;
                    try
                    {
                        ammo = LC.ReadValue<short>(file, $"{hash}_ammo");
                    }
                    catch
                    {
                        // ignored
                    }


                    List<string> components = new List<string>();
                    try
                    {
                        components = LC.ReadList<string>(file, $"{hash}_components");
                    }
                    catch
                    {
                        // ignored
                    }
                    
                    string tint = string.Empty;
                    try
                    {
                        tint = LC.ReadString(file, $"{hash}_tint");
                    }
                    catch
                    {
                        // ignored
                    }
                    int tintIndex = 0;
                    if (!string.IsNullOrEmpty(tint))
                    {
                        // The user may have given a number, such as 21, 5, 3, etc
                        // Or, they may have passed a name of some WeaponTint or Mk2WeaponTint enum such as GREEN, CLASSIC_WHITE, etc
                        
                        // First check if it's a number
                        if (int.TryParse(tint, out int result))
                        {
                            tintIndex = result;
                        }
                        // If it's not a number, check if it's a WeaponTint or Mk2WeaponTint
                        else if (Enum.TryParse(tint, out WeaponTint weaponTint))
                        {
                            tintIndex = (int)weaponTint;
                        }
                        else if (Enum.TryParse(tint, out Mk2WeaponTint mk2WeaponTint))
                        {
                            tintIndex = (int)mk2WeaponTint;
                        }
                        
                        // If it's not a number or a valid enum, then it's invalid and we should ignore it
                        if (tintIndex == 0)
                        {
                            continue;
                        }
                    }

                    try
                    {
                        armour = LC.ReadBool(file, "Armour");
                    }
                    catch
                    {
                        // ignored
                    }
                    
                    try
                    {
                        medkit = LC.ReadBool(file, "Medkit");
                    }
                    catch
                    {
                        // ignored
                    }

                    try
                    {
                        fireExtinguisherBoolean = LC.ReadBool(file, "Fire Extinguisher");
                    }
                    catch
                    {
                        // ignored
                    }

                    string rifleString = string.Empty;
                    try
                    {
                        rifleString = LC.ReadString(file, "Rifle");
                    }
                    catch
                    {
                        // ignored
                    }
                    
                    string shotgunString = string.Empty;
                    try
                    {
                        shotgunString = LC.ReadString(file, "Shotgun");
                    }
                    catch
                    {
                        // ignored
                    }

                    string lessLethalString = string.Empty;
                    try
                    {
                        lessLethalString = LC.ReadString(file, "Less Lethal");
                    }
                    catch
                    {
                        // ignored
                    }

                    try
                    {
                        lessLethalTitle = LC.ReadString(file, "Less Lethal Title");
                    }
                    catch
                    {
                        // ignored
                    }
                    
                    try
                    {
                        shotgunTitle = LC.ReadString(file, "Shotgun Title");
                    }
                    catch
                    {
                        // ignored
                    }
                    
                    try
                    {
                        rifleTitle = LC.ReadString(file, "Rifle Title");
                    }
                    catch
                    {
                        // ignored
                    }
                    
                    Weapon weapon = new Weapon(hash, asset, ammo, components, tintIndex);
                    if (string.Equals(weapon.Name, rifleString, StringComparison.CurrentCultureIgnoreCase))
                        rifle = weapon;
                    else if (string.Equals(weapon.Name, shotgunString, StringComparison.CurrentCultureIgnoreCase))
                        shotgun = weapon;
                    else if (string.Equals(weapon.Name, lessLethalString, StringComparison.CurrentCultureIgnoreCase))
                        lessLethal = weapon;
                    
                    weapons.Add(weapon);
                }

                string fileName = Path.GetFileNameWithoutExtension(file);
                string name = string.Concat(fileName.Where(c => !char.IsWhiteSpace(c) && !char.IsNumber(c)));

                if (fireExtinguisherBoolean)
                    fireExtinguisher = new Weapon("weapon_fireextinguisher", new WeaponAsset("weapon_fireextinguisher"), -1, new List<string>(), 0);
                
                Loadouts.Add(new Loadout { Name = name, Weapons = weapons, Armour = armour, Medkit = medkit, Rifle = rifle, Shotgun = shotgun, LessLethal = lessLethal, RifleTitle = rifleTitle, ShotgunTitle = shotgunTitle, LessLethalTitle = lessLethalTitle, FireExtinguisher = fireExtinguisher });
            }

            if (Main.DefaultLoadout != string.Empty)
            {
                Loadout defaultLoadout = Loadouts.FirstOrDefault(l => l.Name == Main.DefaultLoadout);
                if (defaultLoadout != default)
                {
                    ActiveLoadout = defaultLoadout;
                    return;
                }

                Main.Logger.Error($"Default loadout \"{Main.DefaultLoadout}\" does not exist");
            }

            ActiveLoadout = Loadouts[0];
        }
    }
}
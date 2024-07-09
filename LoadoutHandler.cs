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

        private static readonly Lazy<LoadoutHandler> lazy = new Lazy<LoadoutHandler>(() => new LoadoutHandler());

        public List<Loadout> loadouts;
        public Loadout activeLoadout;
        private string folderPath = AppDomain.CurrentDomain.BaseDirectory + @"\plugins\LSPDFR\Armoury\Loadouts";

        public LoadoutHandler()
        {
            loadouts = new List<Loadout>();

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
            loadouts.Clear();
            
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
                
                foreach (string hash in hashes)
                {
                    if (hash == "weapon_fireextinguisher") continue;
                    
                    WeaponAsset asset = new WeaponAsset(hash);
                    if (!asset.IsValid)
                    {
                        Main.Logger.Error($"Invalid weapon hash \"{hash}\" in loadout file \"{file}\"");
                        continue;
                    }

                    short ammo = -1;
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
                    
                    Weapon weapon = new Weapon(hash, asset, ammo, components);
                    if (string.Equals(weapon.name, rifleString, StringComparison.CurrentCultureIgnoreCase))
                    {
                        rifle = weapon;
                    }
                    else if (string.Equals(weapon.name, shotgunString, StringComparison.CurrentCultureIgnoreCase))
                    {
                        shotgun = weapon;
                    }
                    else if (string.Equals(weapon.name, lessLethalString, StringComparison.CurrentCultureIgnoreCase))
                    {
                        lessLethal = weapon;
                    }
                    
                    weapons.Add(weapon);
                }

                string fileName = Path.GetFileNameWithoutExtension(file);
                string name = string.Concat(fileName.Where(c => !char.IsWhiteSpace(c) && !char.IsNumber(c)));

                if (fireExtinguisherBoolean)
                    fireExtinguisher = new Weapon("weapon_fireextinguisher", new WeaponAsset("weapon_fireextinguisher"), -1, new List<string>());
                
                loadouts.Add(new Loadout { name = name, weapons = weapons, armour = armour, medkit = medkit, rifle = rifle, shotgun = shotgun, lessLethal = lessLethal, fireExtinguisher = fireExtinguisher });
            }

            if (Main.defaultLoadout != string.Empty)
            {
                Loadout defaultLoadout = loadouts.FirstOrDefault(l => l.name == Main.defaultLoadout);
                if (defaultLoadout != default)
                {
                    activeLoadout = defaultLoadout;
                    return;
                }

                Main.Logger.Error($"Default loadout \"{Main.defaultLoadout}\" does not exist");
                activeLoadout = loadouts[0];
            }
            else
            {
                activeLoadout = loadouts[0];
            }
        }
    }
}
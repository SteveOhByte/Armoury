using System.Collections.Generic;
using Rage;

namespace Armoury
{
    public class Weapon
    {
        public string name;
        public WeaponAsset asset;
        public short ammo;
        public List<string> components;

        public Weapon(string name, WeaponAsset asset, short ammo, List<string> components)
        {
            this.name = name;
            this.asset = asset;
            this.ammo = ammo;
            this.components = components;
        }
    }
}
using System.Collections.Generic;
using Rage;

namespace Armoury
{
    public enum WeaponTint
    {
        BLACK = 0,
        GREEN = 1,
        GOLD = 2,
        PINK = 3,
        ARMY = 4,
        LSPD = 5,
        ORANGE = 6,
        PLATINUM = 7,
    }

    public enum Mk2WeaponTint
    {
        BLACK = 0,
        CLASSIC_GRAY = 1,
        CLASSIC_TWO_TONE = 2,
        CLASSIC_WHITE = 3,
        CLASSIC_BEIGE = 4,
        CLASSIC_GREEN = 5,
        CLASSIC_BLUE = 6,
        CLASSIC_EARTH = 7,
        CLASSIC_BROWN_AND_BLACK = 8,
        RED_CONTRAST = 9,
        BLUE_CONTRAST = 10,
        YELLOW_CONTRAST = 11,
        ORANGE_CONTRAST = 12,
        BOLD_PINK = 13,
        BOLD_PURPLE_AND_YELLOW = 14,
        BOLD_ORANGE = 15,
        BOLD_GREEN_AND_PURPLE = 16,
        BOLD_RED_FEATURES = 17,
        BOLD_GREEN_FEATURES = 18,
        BOLD_CYAN_FEATURES = 19,
        BOLD_YELLOW_FEATURES = 20,
        BOLD_RED_AND_WHITE = 21,
        BOLD_BLUE_AND_WHITE = 22,
        METALLIC_GOLD = 23,
        METALLIC_PLATINUM = 24,
        METALLIC_GRAY_AND_LILAC = 25,
        METALLIC_PURPLE_AND_LIME = 26,
        METALLIC_RED = 27,
        METALLIC_GREEN = 28,
        METALLIC_BLUE = 29,
        METALLIC_WHITE_AND_AQUA = 30,
        METALLIC_RED_AND_YELLOW = 31,
    }

    public class Weapon
    {
        public readonly string Name;
        public WeaponAsset Asset;
        public readonly short Ammo;
        public readonly List<string> Components;
        public readonly int TintIndex = 0;

        public Weapon(string name, WeaponAsset asset, short ammo, List<string> components, int tintIndex)
        {
            Name = name;
            Asset = asset;
            Ammo = ammo;
            Components = components;
            TintIndex = tintIndex;
        }
    }
}
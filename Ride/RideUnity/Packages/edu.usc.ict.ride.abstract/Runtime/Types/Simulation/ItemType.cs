namespace Ride.Entities
{
    /// <summary>Identifies item resources that can be created or equipped by simulation systems.</summary>
    /// <remarks>
    /// <see cref="IResourceLoaderSystem"/> resolves these values to registered item resources, while
    /// <see cref="IEquipmentSystem"/> uses them to add and equip items. The start and end markers are
    /// range sentinels retained for category checks and are not concrete items.
    /// </remarks>
    public enum ItemType
    {
        /// <summary>Marks the beginning of the weapon range.</summary>
        START_WEAPONS,
        /// <summary>M4 rifle.</summary>
        m4,
        /// <summary>AK-47 rifle.</summary>
        ak47,
        /// <summary>M1114 vehicle-mounted weapon.</summary>
        HmvM1114_Gun,
        /// <summary>M2 Browning machine gun.</summary>
        m2Browning,
        /// <summary>M240 machine gun.</summary>
        m240,
        /// <summary>M24 rifle.</summary>
        m24,
        /// <summary>M256A1 tank gun.</summary>
        M256A1_Gun,
        /// <summary>Placeholder artillery weapon.</summary>
        PlaceholderArtillery,
        /// <summary>Legacy zombie bite weapon.</summary>
        zombiteBite,
        /// <summary>Marks the end of the weapon range.</summary>
        END_WEAPONS,
        /// <summary>Marks the beginning of the ammunition range.</summary>
        START_AMMUNITION,
        /// <summary>Magazine for the M4 rifle.</summary>
        m4_Magazine,
        /// <summary>Magazine for the AK-47 rifle.</summary>
        ak47_Magazine,
        /// <summary>Magazine for the M1114 vehicle-mounted weapon.</summary>
        HmvM1114_Magazine,
        /// <summary>Magazine for the M2 Browning machine gun.</summary>
        m2Browning_Magazine,
        /// <summary>Magazine for the M240 machine gun.</summary>
        m240_Magazine,
        /// <summary>Magazine for the M24 rifle.</summary>
        m24_Magazine,
        /// <summary>M830A1 high-explosive anti-tank ammunition.</summary>
        M830A1_Heat_Magazine,
        /// <summary>Magazine for the placeholder artillery weapon.</summary>
        PlaceholderArtillery_Magazine,
        /// <summary>Ammunition for the legacy zombie bite weapon.</summary>
        zombieBite_Magazine,
        /// <summary>Marks the end of the ammunition range.</summary>
        END_AMMUNITION,
        /// <summary>Smoke grenade.</summary>
        Smoke_Grenade,
        /// <summary>M1918 Browning Automatic Rifle.</summary>
        M1918BAR,
        /// <summary>Magazine for the M1918 Browning Automatic Rifle.</summary>
        M1918BAR_Magazine,
        /// <summary>MP 40 submachine gun.</summary>
        MP40,
        /// <summary>Magazine for the MP 40.</summary>
        MP40_Magazine,
        /// <summary>Breda Model 30 machine gun.</summary>
        Breda30,
        /// <summary>Magazine for the Breda Model 30.</summary>
        Breda30_Magazine,
        /// <summary>M1 Thompson submachine gun.</summary>
        M1Thompson,
        /// <summary>Magazine for the M1 Thompson.</summary>
        M1Thompson_Magazine,
        /// <summary>M7 grenade launcher.</summary>
        M7,
        /// <summary>Grenade ammunition for the M7 launcher.</summary>
        M7_Grenade,
        /// <summary>M67 fragmentation grenade.</summary>
        M67_Grenade,
        /// <summary>8.8 cm KwK 36 tank gun.</summary>
        TankGun88mmKwk36,
        /// <summary>Magazine for the 8.8 cm KwK 36 tank gun.</summary>
        TankGun88mmKwk36_Magazine,
        /// <summary>75 mm tank gun.</summary>
        TankGun75mm,
        /// <summary>Magazine for the 75 mm tank gun.</summary>
        TankGun75mm_Magazine,
        /// <summary>122 mm tank gun.</summary>
        TankGun122mm,
        /// <summary>Magazine for the 122 mm tank gun.</summary>
        TankGun122mm_Magazine,
        /// <summary>MG 42 machine gun.</summary>
        mg42,
        /// <summary>Magazine for the MG 42.</summary>
        mg42_Magazine,
        /// <summary>Mounted MG 42 machine gun.</summary>
        mg42Mounted,
        /// <summary>M203 grenade launcher.</summary>
        M203,
        /// <summary>Magazine for the M203 grenade launcher.</summary>
        M203_Magazine,
        /// <summary>BMP 2A70 gun.</summary>
        BMP_2A70_Gun,
        /// <summary>Magazine for the BMP 2A70 gun.</summary>
        BMP_2A70_Magazine,
        /// <summary>M242 Bushmaster chain gun.</summary>
        M242Bushmaster_Gun,
        /// <summary>M791 armor-piercing sabot ammunition.</summary>
        M791_APSabot_Magazine,
        /// <summary>D-81TM 2A46 tank gun.</summary>
        D81TM_2A46_Gun,
        /// <summary>3VBM3 ammunition for the T-80U.</summary>
        T80U_3VBM3_Magazine,
        /// <summary>Proximity-triggered improvised explosive device.</summary>
        ProximityIED
    }

    /// <summary>Describes whether an item is available for use.</summary>
    public enum ItemUsage
    {
        /// <summary>The item is available for use.</summary>
        FreeToUse,
        /// <summary>The item is currently being used.</summary>
        InUse,
        /// <summary>The item has been destroyed and cannot be used.</summary>
        Destroyed
    }

    /// <summary>Describes how an item is associated with an owner.</summary>
    public enum ItemStatus
    {
        /// <summary>The item has no owner.</summary>
        Unowned,
        /// <summary>The item is equipped and available to its owner.</summary>
        Equipped,
        /// <summary>The item is owned but stowed.</summary>
        Stowed
    }
}

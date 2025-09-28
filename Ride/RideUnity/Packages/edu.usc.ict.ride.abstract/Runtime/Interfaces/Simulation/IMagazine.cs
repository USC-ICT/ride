namespace Ride.Entities
{
    public enum AmmunitionSize
    {
        size556x45mm,
        size762x39mm,
        size762x51mm,
        size50cal,
        size120mm,
        size155mm,
        sizeZombieMouth,
        size30cal,          // BAR (Sicily project)
        size9x19mm,         // MP 40 (Sicily project)
        size650x52mm,       // Breda 30 (Sicily project)
        size22mmmGrenade,   // M7 Grenade Launcher (Sicily project)
        size88mm,           // 88mm shell used by WW2 German armament (Sicily project)
        size75mm,           // 75mm shell used by US Sherman Tank (Sicily project)
        size122mm,          // 122mm shell used by WW2 Soviet armament (Sicily project)
        size792x57mm,       // 7.92x57mm round used by the MG-42 machine gun
        size40mmGrenade,    // 40mm grenade
        size100mm,          // 100mm HE shell used by the 2A70 BMP3 gun
        size25x137mm,       // 25x137mm shell used by the M242 Bushmaster gun (used by the Bradley fighting vehicle)
        size125mm,          // 125mm smoothbore gun (used by T80U tanks)
    }

    public interface IMagazine : IItem
    {
        AmmunitionSize ammoSize { get; }

        int capacity { get; }

        int ammoCount { get; set; }

        float roundMass { get; }

        float armorPiercingRating { get; }
    }
}

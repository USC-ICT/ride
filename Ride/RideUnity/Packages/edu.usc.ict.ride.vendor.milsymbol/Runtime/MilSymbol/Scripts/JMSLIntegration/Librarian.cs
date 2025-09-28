using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using JointMilitarySymbologyLibrary;

namespace Integration.JMSL
{

    public class Librarian
    {

        private Library m_Library;

        private SymbolSet[] m_SymbolSets;

        public Library Library { get {return m_Library; } }

        public SymbolSet[] SymbolSets { get { return m_SymbolSets; } }
        
        public Librarian(Library library, SymbolSet[] symbolSets)
        {
            m_Library = library;
            m_SymbolSets = symbolSets;
        }

        public LibraryStandardIdentity StandardIdentity(ushort code)
        {
            return m_Library.StandardIdentities
                .Single(si => si.StandardIdentityCode == code);
        }

        public LibraryHQTFDummy HQTFDummy(ushort code)
        {
            return m_Library.HQTFDummies.SingleOrDefault(h => h.HQTFDummyCode == code);
        }

        public LibraryAmplifierGroup AmplifierGroup(ushort code)
        {
            return m_Library.AmplifierGroups.SingleOrDefault(ag => ag.AmplifierGroupCode == code);

        }

        public LibraryAmplifier SymbolAmplifier(string id)
        {
            return m_Library.Amplifiers.SingleOrDefault(amp => amp.ID == id);
        }

        public LibraryAmplifierGroupAmplifier Amplifier(LibraryAmplifierGroup group, ushort code)
        {
            if (group != null && group != default(LibraryAmplifierGroup))
            {
                return group.Amplifiers.SingleOrDefault(amp => amp.AmplifierCode == code);
            }
            return null;
        }

        public LibraryVersion Version(ushort codeOne, ushort codeTwo)
        {
            return m_Library.Versions
                .SingleOrDefault(v => v.VersionCode.DigitOne == codeOne
                && v.VersionCode.DigitTwo == codeTwo);
        }

        public LibraryAffiliation Affiliation(LibraryContext context, LibraryDimension dimension, LibraryStandardIdentity standardIdentity)
        {
            return m_Library.Affiliations
                    .SingleOrDefault(la => la.ContextID == context.ID
                    && la.DimensionID == dimension.ID
                    && la.StandardIdentityID == standardIdentity.ID);
        }

        public LibraryStatus Status(ushort code)
        {
            return m_Library.Statuses
                .SingleOrDefault(s => s.StatusCode == code);
        }

        public LibraryContext Context(ushort code)
        {
            return m_Library.Contexts.SingleOrDefault(c => c.ContextCode == code);
        }

        public LibraryDimension Dimension(string id)
        {
            return m_Library.Dimensions.SingleOrDefault(d => d.ID == id);
        }

        public LibraryDimension DimensionBySymbolSet(string symbolSetId)
        {
            return m_Library.Dimensions
                .SingleOrDefault(d => d.SymbolSets.Count(ss => ss.ID == symbolSetId) > 0);
        }
        
        public LibraryStandardIdentity StandardIdentity(string id)
        {
            return m_Library.StandardIdentities.SingleOrDefault(ident => ident.ID == id);
        }

        public LibraryStandardIdentityGroup StandardIdentityGroup(LibraryStandardIdentity ident)
        {
            LibraryStandardIdentityGroup retObj = null;

            foreach (LibraryStandardIdentityGroup lObj in m_Library.StandardIdentityGroups)
            {
                foreach (string id in lObj.StandardIdentityIDs.Split(' '))
                {
                    if (ident.ID == id)
                    {
                        return lObj;
                    }
                }
            }

            return retObj;
        }

        public SymbolSetEntity Entity (SymbolSet symbolSet, ushort entityCodeOne, ushort entityCodeTwo)
        {
            if (symbolSet != null && symbolSet != default(SymbolSet))
            {
                return symbolSet.Entities
                    .SingleOrDefault(ent => 
                    ent.EntityCode.DigitOne == entityCodeOne 
                    && ent.EntityCode.DigitTwo == entityCodeTwo);
            }
            return null;
        }

        public SymbolSetEntityEntityType EntityType(SymbolSetEntity entity, ushort codeOne, ushort codeTwo)
        {
            if (entity != null && entity != default(SymbolSetEntity))
            {
                if (entity.EntityTypes != null)
                {
                    return entity.EntityTypes
                        .SingleOrDefault(et => et.EntityTypeCode.DigitOne == codeOne
                        && et.EntityTypeCode.DigitTwo == codeTwo);
                }
            }
            return null;
        }

        public EntitySubTypeType EntitySubType(SymbolSetEntityEntityType entityType, ushort codeOne, ushort codeTwo)
        {
            if (entityType != null && entityType != default(SymbolSetEntityEntityType))
            {
                if (entityType.EntitySubTypes != null)
                {
                    entityType.EntitySubTypes
                        .SingleOrDefault(sub => sub.EntitySubTypeCode.DigitOne == codeOne
                        && sub.EntitySubTypeCode.DigitTwo == codeTwo);
                }
            }
            return null;
        }

        public SymbolSet SymbolSet(ushort codeOne, ushort codeTwo)
        {
            return m_SymbolSets
                .SingleOrDefault(ss => ss.SymbolSetCode.DigitOne == codeOne 
                && ss.SymbolSetCode.DigitTwo == codeTwo);
        }


        public ModifiersTypeModifier ModifierOne(SymbolSet symbolSet, ushort modifierCodeOne, ushort modifierCodeTwo)
        {

            if (symbolSet != null && symbolSet != default(SymbolSet))
            {
                if (symbolSet.SectorOneModifiers != null)
                {
                    return symbolSet.SectorOneModifiers
                        .SingleOrDefault(m =>
                        m.ModifierCode.DigitOne == modifierCodeOne
                        && m.ModifierCode.DigitTwo == modifierCodeTwo);
                }
            }
            return default(ModifiersTypeModifier);
        }

        public ModifiersTypeModifier ModifierTwo(SymbolSet symbolSet, ushort modifierCodeOne, ushort modifierCodeTwo)
        {

            if (symbolSet != null && symbolSet != default(SymbolSet))
            {
                if (symbolSet.SectorOneModifiers != null)
                {
                    return symbolSet.SectorTwoModifiers
                        .SingleOrDefault(m =>
                        m.ModifierCode.DigitOne == modifierCodeOne
                        && m.ModifierCode.DigitTwo == modifierCodeTwo);
                }
            }
            return default(ModifiersTypeModifier);
        }
    }
}
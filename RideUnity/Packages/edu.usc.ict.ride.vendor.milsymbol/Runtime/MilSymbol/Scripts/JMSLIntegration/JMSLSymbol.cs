using System.Collections;
using System.Collections.Generic;
using Convert = System.Convert;
using System.Linq;
using UnityEngine;

using JointMilitarySymbologyLibrary;

namespace Integration.JMSL
{

    [System.Serializable]
    public class JMSLSymbol {

        private Librarian m_Librarian;

        private Library m_Library;

        private LibraryVersion m_Version;

        private LibraryContext m_Context;

        private LibraryDimension m_Dimension;

        private LibraryStandardIdentity m_StandardIdentity;

        private LibraryStandardIdentityGroup m_StandardIdentityGroup;

        private LibraryStatus m_Status;

        private LibraryAffiliation m_Affiliation;

        private LibraryHQTFDummy m_HQTFDummy;

        private LibraryAmplifierGroup m_AmplifierGroup;

        private LibraryAmplifierGroupAmplifier m_Amplifier;

        private SymbolSet m_SymbolSet;

        private SymbolSetEntity m_Entity;

        private SymbolSetEntityEntityType m_EntityType;

        private EntitySubTypeType m_EntitySubType;

        private ModifiersTypeModifier m_Modifier1;

        private ModifiersTypeModifier m_Modifier2;

        private SIDC m_Sidc = new SIDC();

        public LibraryStandardIdentity StandardIdentity
        {
            get { return m_StandardIdentity; }
            set
            {
                m_StandardIdentity = value;
                UpdateSIDC();
            }
        }

        public SymbolSet SymbolSet
        {
            get { return m_SymbolSet; }
            set
            {
                m_SymbolSet = value;
                UpdateSIDC();
            }
        }

        public SymbolSetEntity Entity
        {
            get { return m_Entity; }
            set
            {
                m_Entity = value;
                EntityType = null;
                UpdateSIDC();
            }
        }

        public SymbolSetEntityEntityType EntityType
        {
            get { return m_EntityType; }
            set
            {
                m_EntityType = value;
                EntitySubType = null;
                UpdateSIDC();
            }
        }

        public EntitySubTypeType EntitySubType
        {
            get { return m_EntitySubType; }
            set
            {
                m_EntitySubType = value;
                UpdateSIDC();
            }
        }

        public ModifiersTypeModifier Modifier1
        {
            get { return m_Modifier1; }
            set
            {
                m_Modifier1 = value;
                UpdateSIDC();
            }
        }

        public ModifiersTypeModifier Modifier2
        {
            get { return m_Modifier2; }
            set
            {
                m_Modifier2 = value;
                UpdateSIDC();
            }
        }

        public SIDC SIDC
        {
            get { return m_Sidc; }
        }


        public JMSLSymbol(Librarian librarian)
        {
            m_Librarian = librarian;
            m_Library = m_Librarian.Library;

            m_Version = m_Library.Versions[0];
            m_Context = m_Library.Contexts.Single(c => c.ID == "REALITY");
            m_Status = m_Library.Statuses.Single(s => s.Name == "PRESENT");
            m_HQTFDummy = m_Library.HQTFDummies.Single(s => s.Name == "NA");
            m_AmplifierGroup = m_Library.AmplifierGroups.Single(g => g.Name == "NA");
            m_Amplifier = m_AmplifierGroup.Amplifiers.Single(a => a.Name == "NA");
        }

        public void SetSIDC(string firstTen, string secondTen)
        {
            m_Sidc.PartAString = firstTen;
            m_Sidc.PartBString = secondTen;

            UpdateFromSIDC(m_Sidc);
        }


        private void UpdateSIDC()
        {
            // Builds a current (2525D) SIDC from the JMSML Library elements currently associated 
            // with this symbol.

            if (m_Version != null && m_Context != null &&
               m_StandardIdentity != null && m_SymbolSet != null && m_Status != null &&
               m_HQTFDummy != null && m_AmplifierGroup != null && m_Amplifier != null)
            {
                m_Sidc.PartAUInt = (uint)m_Version.VersionCode.DigitOne * 1000000000 +
                                    (uint)m_Version.VersionCode.DigitTwo * 100000000 +
                                    (uint)m_Context.ContextCode * 10000000 +
                                    (uint)m_StandardIdentity.StandardIdentityCode * 1000000 +
                                    (uint)m_SymbolSet.SymbolSetCode.DigitOne * 100000 +
                                    (uint)m_SymbolSet.SymbolSetCode.DigitTwo * 10000 +
                                    (uint)m_Status.StatusCode * 1000 +
                                    (uint)m_HQTFDummy.HQTFDummyCode * 100 +
                                    (uint)m_AmplifierGroup.AmplifierGroupCode * 10 +
                                    (uint)m_Amplifier.AmplifierCode;
            }

            if (m_Entity != null)
            {
                m_Sidc.PartBUInt = (uint)m_Entity.EntityCode.DigitOne * 1000000000 +
                                     (uint)m_Entity.EntityCode.DigitTwo * 100000000;
            }

            if (m_EntityType != null)
            {
                m_Sidc.PartBUInt = m_Sidc.PartBUInt + (uint)m_EntityType.EntityTypeCode.DigitOne * 10000000 +
                                                          (uint)m_EntityType.EntityTypeCode.DigitTwo * 1000000;
            }

            if (m_EntitySubType != null)
            {
                var subtypeCode = m_EntitySubType.EntitySubTypeCode;
                m_Sidc.PartBUInt = m_Sidc.PartBUInt + (uint)subtypeCode.DigitOne * 100000 +
                                                          (uint)subtypeCode.DigitTwo * 10000;
            }

            if (m_Modifier1 != null)
            {
                var modCode = m_Modifier1.ModifierCode;
                m_Sidc.PartBUInt = m_Sidc.PartBUInt + (uint)modCode.DigitOne * 1000 +
                                                          (uint)modCode.DigitTwo * 100;
            }

            if (m_Modifier2 != null)
            {
                var modCode = m_Modifier2.ModifierCode;
                m_Sidc.PartBUInt = m_Sidc.PartBUInt + (uint)modCode.DigitOne * 10 +
                                                          (uint)modCode.DigitTwo;
            }
        }

        private void UpdateFromSIDC(SIDC sidc)
        {
            // Search for the appropriate JMSML Library elements, given the current (2525D)
            // SIDC for this Symbol.

            string first10 = sidc.PartAString;
            string second10 = sidc.PartBString;

            ushort v0 = Convert.ToUInt16(first10.Substring(0, 1));
            ushort v1 = Convert.ToUInt16(first10.Substring(1, 1));
            m_Version = m_Librarian.Version(v0, v1);

            ushort c0 = Convert.ToUInt16(first10.Substring(2, 1));
            m_Context = m_Librarian.Context(c0);

            ushort standardIdentId = Convert.ToUInt16(first10.Substring(3, 1));
            m_StandardIdentity = m_Librarian.StandardIdentity(standardIdentId);

            m_StandardIdentityGroup = m_Librarian.StandardIdentityGroup(m_StandardIdentity);
            ushort ss0 = Convert.ToUInt16(first10.Substring(4, 1));
            ushort ss1 = Convert.ToUInt16(first10.Substring(5, 1));

            m_SymbolSet = m_Librarian.SymbolSet(ss0, ss1);
            
            if (m_SymbolSet != null && m_SymbolSet != default(SymbolSet)) 
            {
                m_Dimension = m_Librarian.DimensionBySymbolSet(m_SymbolSet.ID);
            }

            if (m_Context != default(LibraryContext)
                && m_Dimension != default(LibraryDimension)
                && m_StandardIdentity != default(LibraryStandardIdentity))
            {
                m_Affiliation = m_Librarian.Affiliation(m_Context, m_Dimension, m_StandardIdentity);
            }

            ushort statusCode = Convert.ToUInt16(first10.Substring(6, 1));
            m_Status = m_Librarian.Status(statusCode);

            ushort hqtfDummyCode = Convert.ToUInt16(first10.Substring(7, 1));
            m_HQTFDummy = m_Librarian.HQTFDummy(hqtfDummyCode);

            ushort amplifierGroupCode = Convert.ToUInt16(first10.Substring(8, 1));
            m_AmplifierGroup = m_Librarian.AmplifierGroup(amplifierGroupCode);

            if (m_AmplifierGroup != null && m_AmplifierGroup != default(LibraryAmplifierGroup))
            {
                ushort amplifierCode = Convert.ToUInt16(first10.Substring(9, 1));
                m_Amplifier = m_Librarian.Amplifier(m_AmplifierGroup, amplifierCode);
            }

            if (m_SymbolSet != null && m_SymbolSet != default(SymbolSet))
            {
                m_Entity = m_Librarian.Entity(m_SymbolSet, Convert.ToUInt16(second10.Substring(0, 1)), Convert.ToUInt16(second10.Substring(1, 1)));

                if (m_Entity != null && m_Entity != default(SymbolSetEntity))
                {
                    ushort d0 = Convert.ToUInt16(second10.Substring(2, 1));
                    ushort d1 = Convert.ToUInt16(second10.Substring(3, 1));
                    m_EntityType = m_Librarian.EntityType(m_Entity, d0, d1);
                }

                if (m_EntityType != null && m_EntityType != default(SymbolSetEntityEntityType))
                {
                    ushort d0 = Convert.ToUInt16(second10.Substring(4, 1));
                    ushort d1 = Convert.ToUInt16(second10.Substring(5, 1));
                    m_EntitySubType = m_Librarian.EntitySubType(m_EntityType, d0, d1);
                }

                m_Modifier1 = m_Librarian.ModifierOne(m_SymbolSet, Convert.ToUInt16(second10.Substring(6, 1)), Convert.ToUInt16(second10.Substring(7, 1)));
                m_Modifier2 = m_Librarian.ModifierTwo(m_SymbolSet, Convert.ToUInt16(second10.Substring(8, 1)), Convert.ToUInt16(second10.Substring(9, 1)));

                //_legacySymbol = _librarian.LegacySymbol(_symbolSet, _entity, _entityType, _entitySubType, _modifierOne, _modifierTwo);
            }

            //_librarian.LogConversionResult(_sidc.PartAString + ", " + _sidc.PartBString);

            //_ValidateStatus();
        }

    }
}

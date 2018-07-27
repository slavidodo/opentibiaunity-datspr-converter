using System;
using System.Collections.Generic;

namespace OpenTibiaUnity.Core.Sprites
{
    using ThingTypesDict = Dictionary<ushort, ThingType>;

    public enum ThingCategory : byte
    {
        Item = 0,
        Creature,
        Effect,
        Missile,
        InvalidCategory,
        LastCategory = InvalidCategory
    };

    public sealed class ContentData
    {
        Net.InputMessage m_BinaryReader;

        public uint DatSignature { get; private set; }
        public ushort ContentRevision { get; private set; }
        public ThingTypesDict[] ThingTypes { get; private set; } = new ThingTypesDict[(int)ThingCategory.LastCategory];

        public ContentData(byte[] buffer) {
            m_BinaryReader = new Net.InputMessage(buffer);
        }

        public void Parse() {
            DatSignature = m_BinaryReader.GetU32();
            ContentRevision = (ushort)DatSignature;

            int[] counts = new int[(int)ThingCategory.LastCategory];
            for (int category = 0; category < (int)ThingCategory.LastCategory; category++) {
                int count = m_BinaryReader.GetU16() + 1;
                counts[category] = count;
            }

            for (int category = 0; category < (int)ThingCategory.LastCategory; category++) {
                ushort firstId = 1;
                if (category == (int)ThingCategory.Item) {
                    firstId = 100;
                }
                
                ThingTypes[category] = new ThingTypesDict();
                for (ushort id = firstId; id < counts[category]; id++) {
                    ThingType thingType = ThingType.Serialize(id, (ThingCategory)category, ref m_BinaryReader);
                    ThingTypes[category][id] = thingType;
                }
            }
        }
    }
}
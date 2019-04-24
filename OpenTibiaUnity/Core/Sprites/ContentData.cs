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
        int m_ClientVersion;

        public uint DatSignature { get; private set; }
        public ushort ContentRevision { get; private set; }
        public ThingTypesDict[] ThingTypeDictionaries { get; private set; } = new ThingTypesDict[(int)ThingCategory.LastCategory];

        public ContentData(byte[] buffer, int clientVersion) {
            m_BinaryReader = new Net.InputMessage(buffer);
            m_ClientVersion = clientVersion;
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
                
                ThingTypeDictionaries[category] = new ThingTypesDict();
                for (ushort id = firstId; id < counts[category]; id++) {
                    ThingType thingType = ThingType.Unserialize(id, (ThingCategory)category, m_BinaryReader, m_ClientVersion);
                    ThingTypeDictionaries[category][id] = thingType;
                }
            }
        }
    }
}
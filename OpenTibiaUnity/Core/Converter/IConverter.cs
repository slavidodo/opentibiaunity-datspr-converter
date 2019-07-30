using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenTibiaUnity.Core.Converter
{
    public class SpriteTypeImpl
    {
        public string File;
        public int SpriteType;
        public uint FirstSpriteID;
        public uint LastSpriteID;
    }

    interface IConverter
    {
        Task<bool> BeginProcessing();

        List<SpriteTypeImpl> GetSpriteSheet();
    }
}

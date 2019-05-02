using System;
using System.Drawing;

namespace OpenTibiaUnity.Core.Sprites
{
    public class ContentSprites
    {
        Net.InputMessage m_BinaryReader;
        bool m_UseAlpha = false;

        int m_ClientVersion = 0;

        public uint Signature { get; private set; } = 0;
        public uint SpritesCount { get; private set; } = 0;
        public int SpritesOffset { get; private set; } = 0;
        
        public ContentSprites(byte[] buffer, int clientVersion, bool alpha = false) {
            m_BinaryReader = new Net.InputMessage(buffer);
            m_ClientVersion = clientVersion;
            m_UseAlpha = alpha;
        }

        public static uint ClientVersionToSprSignature(int version) {
            switch (version) {
                case 770: return 0x439852be;
                case 1098: return 0x57bbd603;
                default: return 0;
            }
        }

        public byte[] ConvertTo(int newVersion) {
            var binaryWriter = new Net.OutputMessage();
            binaryWriter.AddU32(ClientVersionToSprSignature(newVersion));

            int padding = 0;
            if (newVersion >= 960) {
                binaryWriter.AddU32(SpritesCount);
                padding = m_ClientVersion < 960 ? 2 : 0;
            } else {
                binaryWriter.AddU16((ushort)SpritesCount);
                padding = m_ClientVersion >= 960 ? -2 : 0;
            }

            m_BinaryReader.Seek(SpritesOffset);
            for (uint i = 0; i < SpritesCount; i++) {
                var spriteAddress = m_BinaryReader.GetU32();
                if (spriteAddress == 0)
                    binaryWriter.AddU32(0);
                else
                    binaryWriter.AddU32((uint)(spriteAddress + padding));
            }

            var pixels = m_BinaryReader.GetUnreadBuffer();
            binaryWriter.AddBytes(pixels);

            return binaryWriter.GetBufferArray();
        }

        public void Parse() {
            Signature = m_BinaryReader.GetU32();
            Console.WriteLine("Spr Signature: 0x" + Signature.ToString("x"));
            SpritesCount = m_ClientVersion >= 960 ? m_BinaryReader.GetU32() : m_BinaryReader.GetU16();
            SpritesOffset = m_BinaryReader.Tell();
        }

        public Bitmap GetSprite(uint id) {
            lock (m_BinaryReader)
                return RawGetSprite(id);
        }

        private Bitmap RawGetSprite(uint id) {
            if (id == 0 || m_BinaryReader == null)
                return null;
            
            m_BinaryReader.Seek((int)((id-1) * 4) + SpritesOffset);

            uint spriteAddress = m_BinaryReader.GetU32();
            if (spriteAddress == 0)
                return null;

            m_BinaryReader.Seek((int)spriteAddress);
            m_BinaryReader.SkipBytes(3); // color values

            ushort pixelDataSize = m_BinaryReader.GetU16();

            int writePos = 0;
            int read = 0;
            byte channels = (byte)(m_UseAlpha ? 4 : 3);

            Bitmap bitmap = new Bitmap(32, 32);
            Color transparentColor = Color.Transparent;

            while (read < pixelDataSize && writePos < 4096) {
                ushort transparentPixels = m_BinaryReader.GetU16();
                ushort coloredPixels = m_BinaryReader.GetU16();

                for (int i = 0; i < transparentPixels && writePos < 4096; i++) {
                    int pixel = writePos / 4;
                    int x = pixel % 32;
                    int y = pixel / 32;

                    bitmap.SetPixel(x, y, transparentColor);
                    writePos += 4;
                }

                for (int i = 0; i < coloredPixels && writePos < 4096; i++) {
                    int r = m_BinaryReader.GetU8();
                    int g = m_BinaryReader.GetU8();
                    int b = m_BinaryReader.GetU8();
                    int a = m_UseAlpha ? m_BinaryReader.GetU8() : 0xFF;

                    int pixel = writePos / 4;
                    int x = pixel % 32;
                    int y = pixel / 32;

                    bitmap.SetPixel(x, y, Color.FromArgb(a, r, g, b));
                    writePos += 4;
                }

                read += 4 + (channels * coloredPixels);
            }

            while (writePos < 4096) {
                int pixel = writePos / 4;
                int x = pixel % 32;
                int y = pixel / 32;

                bitmap.SetPixel(x, y, transparentColor);
                writePos += 4;
            }

            return bitmap;
        }
    }
}

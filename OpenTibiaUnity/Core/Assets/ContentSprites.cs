using System;
using System.Drawing;

namespace OpenTibiaUnity.Core.Assets
{
    public class ContentSprites
    {
        IO.BinaryStream m_BinaryReader;
        bool m_UseAlpha = false;

        int m_ClientVersion = 0;

        public uint Signature { get; private set; } = 0;
        public uint SpritesCount { get; private set; } = 0;
        public long SpritesOffset { get; private set; } = 0;
        
        public ContentSprites(byte[] buffer, int clientVersion, bool alpha = false) {
            m_BinaryReader = new IO.BinaryStream(buffer);
            m_ClientVersion = clientVersion;
            m_UseAlpha = alpha;

            Parse();
        }

        public static uint ClientVersionToSprSignature(int version) {
            switch (version) {
                case 770: return 0x439852be;
                case 1098: return 0x57bbd603;
                default: return 0;
            }
        }

        public byte[] ConvertTo(int newVersion) {
            var binaryWriter = new IO.BinaryStream();
            binaryWriter.WriteUnsignedInt(ClientVersionToSprSignature(newVersion));

            int padding = 0;
            if (newVersion >= 960) {
                binaryWriter.WriteUnsignedInt(SpritesCount);
                padding = m_ClientVersion < 960 ? 2 : 0;
            } else {
                binaryWriter.WriteUnsignedShort((ushort)SpritesCount);
                padding = m_ClientVersion >= 960 ? -2 : 0;
            }

            m_BinaryReader.Seek(SpritesOffset, System.IO.SeekOrigin.Begin);
            for (uint i = 0; i < SpritesCount; i++) {
                var spriteAddress = m_BinaryReader.ReadUnsignedInt();
                if (spriteAddress == 0)
                    binaryWriter.WriteUnsignedInt(0);
                else
                    binaryWriter.WriteUnsignedInt((uint)(spriteAddress + padding));
            }

            var pixels = m_BinaryReader.ReadRemaining();
            binaryWriter.Write(pixels, 0, pixels.Length);

            return binaryWriter.GetBuffer();
        }

        private void Parse() {
            Signature = m_BinaryReader.ReadUnsignedInt();
            SpritesCount = m_ClientVersion >= 960 ? m_BinaryReader.ReadUnsignedInt() : m_BinaryReader.ReadUnsignedShort();
            SpritesOffset = m_BinaryReader.Position;
        }

        public Bitmap GetSprite(uint id) {
            lock (m_BinaryReader)
                return RawGetSprite(id);
        }

        private Bitmap RawGetSprite(uint id) {
            if (id == 0 || m_BinaryReader == null)
                return null;
            
            m_BinaryReader.Seek((int)((id-1) * 4) + SpritesOffset, System.IO.SeekOrigin.Begin);

            uint spriteAddress = m_BinaryReader.ReadUnsignedInt();
            if (spriteAddress == 0)
                return null;

            m_BinaryReader.Seek((int)spriteAddress, System.IO.SeekOrigin.Begin);
            m_BinaryReader.Skip(3); // color values

            ushort pixelDataSize = m_BinaryReader.ReadUnsignedShort();

            int writePos = 0;
            int read = 0;
            byte channels = (byte)(m_UseAlpha ? 4 : 3);

            Bitmap bitmap = new Bitmap(32, 32);
            Color transparentColor = Color.Transparent;

            while (read < pixelDataSize && writePos < 4096) {
                ushort transparentPixels = m_BinaryReader.ReadUnsignedShort();
                ushort coloredPixels = m_BinaryReader.ReadUnsignedShort();

                for (int i = 0; i < transparentPixels && writePos < 4096; i++) {
                    int pixel = writePos / 4;
                    int x = pixel % 32;
                    int y = pixel / 32;

                    bitmap.SetPixel(x, y, transparentColor);
                    writePos += 4;
                }

                for (int i = 0; i < coloredPixels && writePos < 4096; i++) {
                    int r = m_BinaryReader.ReadUnsignedByte();
                    int g = m_BinaryReader.ReadUnsignedByte();
                    int b = m_BinaryReader.ReadUnsignedByte();
                    int a = m_UseAlpha ? m_BinaryReader.ReadUnsignedByte() : 0xFF;

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

using System;
using System.Text;

namespace OpenTibiaUnity.Core.IO
{
    public class BinaryStream : System.IO.MemoryStream
    {
        public BinaryStream() : base() { }
        public BinaryStream(byte[] buffer) : base(buffer) { }

        public byte ReadUnsignedByte() {
            return (byte)ReadByte();
        }

        public sbyte ReadSignedByte() {
            return (sbyte)ReadByte();
        }

        public ushort ReadUnsignedShort() {
            var buf = new byte[sizeof(ushort)];
            Read(buf, 0, buf.Length);
            return BitConverter.ToUInt16(buf, 0);
        }

        public short ReadShort() {
            var buf = new byte[sizeof(short)];
            Read(buf, 0, buf.Length);
            return BitConverter.ToInt16(buf, 0);
        }

        public uint ReadUnsignedInt() {
            var buf = new byte[sizeof(uint)];
            Read(buf, 0, buf.Length);
            return BitConverter.ToUInt32(buf, 0);
        }

        public int ReadInt() {
            var buf = new byte[sizeof(int)];
            Read(buf, 0, buf.Length);
            return BitConverter.ToInt32(buf, 0);
        }

        public ulong ReadUnsignedLong() {
            var buf = new byte[sizeof(ulong)];
            Read(buf, 0, buf.Length);
            return BitConverter.ToUInt64(buf, 0);
        }

        public long ReadLong() {
            var buf = new byte[sizeof(long)];
            Read(buf, 0, buf.Length);
            return BitConverter.ToInt64(buf, 0);
        }

        public string ReadString() {
            int length = ReadUnsignedShort();
            var buf = new byte[length];
            Read(buf, 0, length);
            return Encoding.ASCII.GetString(buf);
        }

        public byte[] ReadRemaining() {
            long remaining = Length - Position;
            var buf = new byte[(int)remaining];
            Read(buf, 0, buf.Length);
            return buf;
        }

        public void WriteUnsignedByte(byte b) {
            Write(new byte[] { b }, 0, 1);
        }

        public void WriteSignedByte(sbyte b) {
            Write(new byte[] { (byte)b }, 0, 1);
        }

        public void WriteUnsignedShort(ushort v) {
            var buf = BitConverter.GetBytes(v);
            Write(buf, 0, buf.Length);
        }

        public void WriteShort(short v) {
            var buf = BitConverter.GetBytes(v);
            Write(buf, 0, buf.Length);
        }

        public void WriteUnsignedInt(uint v) {
            var buf = BitConverter.GetBytes(v);
            Write(buf, 0, buf.Length);
        }

        public void WriteInt(int v) {
            var buf = BitConverter.GetBytes(v);
            Write(buf, 0, buf.Length);
        }

        public void WriteUnsignedLong(ulong v) {
            var buf = BitConverter.GetBytes(v);
            Write(buf, 0, buf.Length);
        }

        public void WriteLong(long v) {
            var buf = BitConverter.GetBytes(v);
            Write(buf, 0, buf.Length);
        }

        public void WriteString(string v) {
            WriteUnsignedShort((ushort)v.Length);
            var buf = Encoding.ASCII.GetBytes(v);
            Write(buf, 0, buf.Length);
        }

        public void Skip(int n) {
            Position += n;
        }
    }
}
﻿


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Peach.Core.IO;
using System.IO;

namespace Peach.Core.Test
{
	[TestFixture] 
	[Peach]
	[Quick]
	public class BitStreamTest
	{
		[Test]
		public void Length()
		{
			BitStream bs = new BitStream();

			Assert.AreEqual(0, bs.LengthBits);

			bs.WriteBit(0);
			Assert.AreEqual(1, bs.LengthBits);

			bs = new BitStream();
			for (int i = 1; i < 10000; i++)
			{
				bs.WriteBit(0);
				Assert.AreEqual(i, bs.LengthBits);
			}

			bs = new BitStream();
			bs.WriteByte(1);
			Assert.AreEqual(8, bs.LengthBits);
			Assert.AreEqual(1, bs.Length);

			bs = new BitStream(new byte[] { 1, 2, 3, 4, 5 });
			Assert.AreEqual(5, bs.Length);
			Assert.AreEqual(5 * 8, bs.LengthBits);
		}

		[Test]
		public void ReadingBites()
		{
			var bs = new BitStream(new byte[] { 0x41, 0x41 });

			bs.SeekBits(0, System.IO.SeekOrigin.Begin);

			Assert.AreEqual(0, bs.ReadBit()); // 0
			Assert.AreEqual(1, bs.ReadBit()); // 1
			Assert.AreEqual(0, bs.ReadBit()); // 2
			Assert.AreEqual(0, bs.ReadBit()); // 3
			Assert.AreEqual(0, bs.ReadBit()); // 4
			Assert.AreEqual(0, bs.ReadBit()); // 5
			Assert.AreEqual(0, bs.ReadBit()); // 6
			Assert.AreEqual(1, bs.ReadBit()); // 7

			Assert.AreEqual(0, bs.ReadBit()); // 0
			Assert.AreEqual(1, bs.ReadBit()); // 1
			Assert.AreEqual(0, bs.ReadBit()); // 2
			Assert.AreEqual(0, bs.ReadBit()); // 3
			Assert.AreEqual(0, bs.ReadBit()); // 4
			Assert.AreEqual(0, bs.ReadBit()); // 5
			Assert.AreEqual(0, bs.ReadBit()); // 6
			Assert.AreEqual(1, bs.ReadBit()); // 7
		}

		[Test]
		public void ReadMixed()
		{
			var bs = new BitStream(new byte[] { 0x01, 0x00 });

			bs.Seek(0, SeekOrigin.Begin);

			Assert.AreEqual(0, bs.ReadBit()); // 0
			Assert.AreEqual(0, bs.ReadBit()); // 1
			Assert.AreEqual(0, bs.ReadBit()); // 2
			Assert.AreEqual(0, bs.ReadBit()); // 3
			Assert.AreEqual(0, bs.ReadBit()); // 4
			Assert.AreEqual(0, bs.ReadBit()); // 5
			Assert.AreEqual(0, bs.ReadBit()); // 6

			int val = bs.ReadByte();
			Assert.AreEqual(0x80, val);
		}

		[Test]
		public void ReadWriteBits()
		{
			BitStream bs = new BitStream();

			bs.WriteBit(0);
			bs.WriteBit(0);
			bs.WriteBit(0);
			bs.WriteBit(1);
			bs.WriteBit(0);
			bs.WriteBit(1);
			bs.WriteBit(1);
			bs.WriteBit(1);
			bs.WriteBit(1);

			bs.WriteBit(0);
			bs.WriteBit(0);
			bs.WriteBit(0);
			bs.WriteBit(1);
			bs.WriteBit(0);
			bs.WriteBit(1);

			bs.SeekBits(0, System.IO.SeekOrigin.Begin);

			Assert.AreEqual(0, bs.ReadBit());
			Assert.AreEqual(0, bs.ReadBit());
			Assert.AreEqual(0, bs.ReadBit());
			Assert.AreEqual(1, bs.ReadBit());
			Assert.AreEqual(0, bs.ReadBit());
			Assert.AreEqual(1, bs.ReadBit());
			Assert.AreEqual(1, bs.ReadBit());
			Assert.AreEqual(1, bs.ReadBit());
			Assert.AreEqual(1, bs.ReadBit());

			Assert.AreEqual(0, bs.ReadBit());
			Assert.AreEqual(0, bs.ReadBit());
			Assert.AreEqual(0, bs.ReadBit());
			Assert.AreEqual(1, bs.ReadBit());
			Assert.AreEqual(0, bs.ReadBit());
			Assert.AreEqual(1, bs.ReadBit());
		}
		protected static string Int2String(int b)
		{
			string ret = "";

			for (int i = 0; i < 32; i++)
			{
				int bit = (b >> 31 - i) & 1;
				ret += bit == 0 ? "0" : "1";
			}

			return ret;
		}

		protected static string Byte2String(byte b)
		{
			string ret = "";

			for (int i = 0; i < 8; i++)
			{
				int bit = (b >> 7 - i) & 1;
				ret += bit == 0 ? "0" : "1";
			}

			return ret;
		}

		protected static string Short2String(short b)
		{
			string ret = "";

			for (int i = 0; i < 16; i++)
			{
				int bit = (b >> 15 - i) & 1;
				ret += bit == 0 ? "0" : "1";
			}

			return ret;
		}

		[Test]
		public void ReadWriteNumbers()
		{
			BitStream bs = new BitStream();
			BitWriter w = new BitWriter(bs);
			BitReader r = new BitReader(bs);

			w.LittleEndian();
			r.LittleEndian();

			//Max
			w.WriteSByte(sbyte.MaxValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(sbyte.MaxValue, r.ReadSByte());

			bs.SetLength(0);
			w.WriteInt16(short.MaxValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(short.MaxValue, r.ReadInt16());

			bs.SetLength(0);
			w.WriteInt32(67305985);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(67305985, r.ReadInt32());

			bs.SetLength(0);
			w.WriteInt32(Int32.MaxValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(Int32.MaxValue, r.ReadInt32());

			bs.SetLength(0);
			w.WriteInt64(Int64.MaxValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(Int64.MaxValue, r.ReadInt64());

			bs.SetLength(0);
			w.WriteByte(byte.MaxValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(byte.MaxValue, r.ReadByte());

			bs.SetLength(0);
			w.WriteUInt16(ushort.MaxValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(ushort.MaxValue, r.ReadUInt16());

			bs.SetLength(0);
			w.WriteUInt32(UInt32.MaxValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(UInt32.MaxValue, r.ReadUInt32());

			bs.SetLength(0);
			w.WriteUInt64(UInt64.MaxValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(UInt64.MaxValue, r.ReadUInt64());

			//Min
			bs.SetLength(0);
			w.WriteSByte(sbyte.MinValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(sbyte.MinValue, r.ReadSByte());

			bs.SetLength(0);
			w.WriteInt16(short.MinValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(short.MinValue, r.ReadInt16());

			bs.SetLength(0);
			w.WriteInt32(Int32.MinValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(Int32.MinValue, r.ReadInt32());

			bs.SetLength(0);
			w.WriteInt64(Int64.MinValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(Int64.MinValue, r.ReadInt64());

			// BIG ENDIAN //////////////////////////////////////////

			bs.SetLength(0);
			w.LittleEndian();
			r.LittleEndian();

			//Max
			w.WriteSByte(sbyte.MaxValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(sbyte.MaxValue, r.ReadSByte());

			bs.SetLength(0);
			w.WriteInt16(short.MaxValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(short.MaxValue, r.ReadInt16());

			bs.SetLength(0);
			w.WriteInt32(67305985);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(67305985, r.ReadInt32());

			bs.SetLength(0);
			w.WriteInt32(Int32.MaxValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(Int32.MaxValue, r.ReadInt32());

			bs.SetLength(0);
			w.WriteInt64(Int64.MaxValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(Int64.MaxValue, r.ReadInt64());

			bs.SetLength(0);
			w.WriteByte(byte.MaxValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(byte.MaxValue, r.ReadByte());

			bs.SetLength(0);
			w.WriteUInt16(ushort.MaxValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(ushort.MaxValue, r.ReadUInt16());

			bs.SetLength(0);
			w.WriteUInt32(UInt32.MaxValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(UInt32.MaxValue, r.ReadUInt32());

			bs.SetLength(0);
			w.WriteUInt64(UInt64.MaxValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(UInt64.MaxValue, r.ReadUInt64());

			//Min
			bs.SetLength(0);
			w.WriteSByte(sbyte.MinValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(sbyte.MinValue, r.ReadSByte());

			bs.SetLength(0);
			w.WriteInt16(short.MinValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(short.MinValue, r.ReadInt16());

			bs.SetLength(0);
			w.WriteInt32(Int32.MinValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(Int32.MinValue, r.ReadInt32());

			bs.SetLength(0);
			w.WriteInt64(Int64.MinValue);
			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(Int64.MinValue, r.ReadInt64());
		}

		[Test]
		public void ExponentialAdd()
		{
			int remain = 10000;
			//int remain = 6;
			var value = new BitStream(Encoding.ASCII.GetBytes("H"));

			//var lst = new BitStreamList();
			//lst.Add(value);

			var foo = new Stack<Tuple<long, bool>>();
			foo.Push(null);

			while (remain > 1)
			{
				bool carry = remain % 2 == 1;
				remain /= 2;
				foo.Push(new Tuple<long, bool>(remain, carry));
			}

			var asElem = (BitwiseStream)value;

			var item = foo.Pop();

			while (item != null)
			{
				var newList = new BitStreamList();
				newList.Add(asElem);
				newList.Add(asElem);
				if (item.Item2)
					newList.Add(value);

				asElem = newList;
				item = foo.Pop();
			}


			var len = asElem.Length;
			Assert.AreEqual(10000, len);
		}

		[Test]
		public void ReadWriteNumbersOddOffset()
		{
			BitStream bs = new BitStream();
			BitWriter w = new BitWriter(bs);
			BitReader r = new BitReader(bs);

			w.LittleEndian();
			r.LittleEndian();

			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);

			Assert.AreEqual(3, bs.LengthBits);
			Assert.AreEqual(3, bs.PositionBits);

			//Max
			w.WriteSByte(sbyte.MaxValue);

			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(0, r.ReadBit());
			Assert.AreEqual(1, r.ReadBit());
			Assert.AreEqual(1, r.ReadBit());
			Assert.AreEqual(1, r.ReadBit());
			Assert.AreEqual(1, r.ReadBit());
			Assert.AreEqual(1, r.ReadBit());
			Assert.AreEqual(1, r.ReadBit());

			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(sbyte.MaxValue, r.ReadSByte());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteInt16(short.MaxValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(short.MaxValue, r.ReadInt16());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteInt32(67305985);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(67305985, r.ReadInt32());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteInt32(Int32.MaxValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(Int32.MaxValue, r.ReadInt32());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteInt64(Int64.MaxValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(Int64.MaxValue, r.ReadInt64());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteByte(byte.MaxValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(byte.MaxValue, r.ReadByte());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteUInt16(ushort.MaxValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(ushort.MaxValue, r.ReadUInt16());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteUInt32(UInt32.MaxValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(UInt32.MaxValue, r.ReadUInt32());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteUInt64(UInt64.MaxValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(UInt64.MaxValue, r.ReadUInt64());


			//Min
			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteSByte(sbyte.MinValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(sbyte.MinValue, r.ReadSByte());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteInt16(short.MinValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(short.MinValue, r.ReadInt16());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteInt32(Int32.MinValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(Int32.MinValue, r.ReadInt32());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteInt64(Int64.MinValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(Int64.MinValue, r.ReadInt64());

			// BIG ENDIAN //////////////////////////////////////////

			bs.SetLength(0);
			r.BigEndian();
			w.BigEndian();

			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);

			//Max
			w.WriteSByte(sbyte.MaxValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(sbyte.MaxValue, r.ReadSByte());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteInt16(short.MaxValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(short.MaxValue, r.ReadInt16());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteInt32(67305985);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(67305985, r.ReadInt32());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteInt32(Int32.MaxValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(Int32.MaxValue, r.ReadInt32());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteInt64(Int64.MaxValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(Int64.MaxValue, r.ReadInt64());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteByte(byte.MaxValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(byte.MaxValue, r.ReadByte());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteUInt16(ushort.MaxValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(ushort.MaxValue, r.ReadUInt16());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteUInt32(UInt32.MaxValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(UInt32.MaxValue, r.ReadUInt32());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteUInt64(UInt64.MaxValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(UInt64.MaxValue, r.ReadUInt64());

			//Min
			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteSByte(sbyte.MinValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(sbyte.MinValue, r.ReadSByte());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteInt16(short.MinValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(short.MinValue, r.ReadInt16());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteInt32(Int32.MinValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(Int32.MinValue, r.ReadInt32());

			bs.SetLength(0);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteBit(1);
			w.WriteInt64(Int64.MinValue);
			bs.SeekBits(3, System.IO.SeekOrigin.Begin);
			Assert.AreEqual(Int64.MinValue, r.ReadInt64());
		}

		[Test]
		public void TestSeek()
		{
			MemoryStream ms = new MemoryStream();
			Assert.AreEqual(0, ms.Length);
			Assert.AreEqual(0, ms.Position);
			ms.Seek(10, SeekOrigin.Begin);
			Assert.AreEqual(0, ms.Length);
			Assert.AreEqual(10, ms.Position);
			ms.WriteByte(1);
			Assert.AreEqual(11, ms.Length);
			Assert.AreEqual(11, ms.Position);

			BitStream bs = new BitStream();
			Assert.AreEqual(0, bs.LengthBits);
			Assert.AreEqual(0, bs.Length);
			Assert.AreEqual(0, bs.PositionBits);
			Assert.AreEqual(0, bs.Position);
			Assert.AreEqual(0, bs.BaseStream.Length);
			Assert.AreEqual(0, bs.BaseStream.Position);

			bs.SeekBits(10, SeekOrigin.Begin);
			Assert.AreEqual(0, bs.LengthBits);
			Assert.AreEqual(0, bs.Length);
			Assert.AreEqual(10, bs.PositionBits);
			Assert.AreEqual(1, bs.Position);
			Assert.AreEqual(0, bs.BaseStream.Length);
			Assert.AreEqual(1, bs.BaseStream.Position);

			bs.WriteBit(1);
			Assert.AreEqual(11, bs.LengthBits);
			Assert.AreEqual(1, bs.Length);
			Assert.AreEqual(11, bs.PositionBits);
			Assert.AreEqual(1, bs.Position);
			Assert.AreEqual(2, bs.BaseStream.Length);
			Assert.AreEqual(1, bs.BaseStream.Position);
		}

		class BitStreamWriter
		{
			Endian endian;

			public BitStream Stream = new BitStream();

			public BitStreamWriter(Endian endian)
			{
				this.endian = endian;
			}

			public void Write(long value, int bits)
			{
				Stream.WriteBits(endian.GetBits(value, bits), bits);
			}

		}

		[Test]
		public void BitConverter()
		{
			/*
			 * Unsigned, BE, 12bit "A B C" -> 0x0ABC ->  2748
			 * Signed  , BE, 12bit "A B C" -> 0xFABC -> -1348
			 * Unsigned, LE, 12bit "B C A" -> 0x0ABC ->  2748
			 * Signed  , LE, 12bit "B C A" -> 0xFABC -> -1348
			 */

			byte[] val = null;

			val = Endian.Big.GetBytes(0xABC, 0);
			Assert.AreEqual(new byte[] { }, val);
			Assert.AreEqual(0, Endian.Big.GetUInt64(val, 0));
			Assert.AreEqual(0, Endian.Big.GetInt64(val, 0));

			val = Endian.Big.GetBytes(0xABC, 12);
			Assert.AreEqual(new byte[] { 0xab, 0xc0 }, val);
			Assert.AreEqual(2748, Endian.Big.GetUInt64(val, 12));
			Assert.AreEqual(-1348, Endian.Big.GetInt64(val, 12));

			val = Endian.Little.GetBytes(0xABC, 0);
			Assert.AreEqual(new byte[] { }, val);
			Assert.AreEqual(0, Endian.Little.GetUInt64(val, 0));
			Assert.AreEqual(0, Endian.Little.GetInt64(val, 0));

			val = Endian.Little.GetBytes(0xABC, 12);
			Assert.AreEqual(new byte[] { 0xbc, 0xa0 }, val);
			Assert.AreEqual(2748, Endian.Little.GetUInt64(val, 12));
			Assert.AreEqual(-1348, Endian.Little.GetInt64(val, 12));

			ulong bits = 0;

			bits = Endian.Big.GetBits(0xABC, 12);
			Assert.AreEqual(0xABC, bits);
			Assert.AreEqual(2748, Endian.Big.GetUInt64(bits, 12));
			Assert.AreEqual(-1348, Endian.Big.GetInt64(bits, 12));

			bits = Endian.Little.GetBits(0xABC, 12);
			Assert.AreEqual(0xBCA, bits);
			Assert.AreEqual(2748, Endian.Little.GetUInt64(bits, 12));
			Assert.AreEqual(-1348, Endian.Little.GetInt64(bits, 12));
		}

		[Test]
		public void BitStreamBits()
		{
			byte[] expected = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89 };
			BitStream bs = new BitStream(expected);
			ulong test;
			bs.ReadBits(out test, 40);
			Assert.AreEqual(0x0123456789, test);

			bs = new BitStream();
			bs.WriteBits(test, 40);
			Assert.AreEqual(40, bs.LengthBits);
			MemoryStream ms = bs.BaseStream as MemoryStream;
			Assert.NotNull(ms);
			Assert.AreEqual(ms.Length, 5);

			for (int i = 0; i < expected.Length; ++i)
				Assert.AreEqual(expected[i], ms.GetBuffer()[i]);
		}

		[Test]
		public void BitwiseNumbers()
		{
			/*
			 * Unsigned, BE, 12bit "A B C" -> 0x0ABC ->  2748
			 * Signed  , BE, 12bit "A B C" -> 0xFABC -> -1348
			 * Unsigned, LE, 12bit "B C A" -> 0x0ABC ->  2748
			 * Signed  , LE, 12bit "B C A" -> 0xFABC -> -1348
			 */

			var w = new BitStreamWriter(Endian.Big);
			w.Write(0, 32);
			w.Write(0x00, 1);
			w.Write(0x01, 1);
			w.Write(0x00, 1);
			w.Write(-1, 5);
			w.Write(0x0f, 4);
			w.Write(0x123456789A, 40);

			Assert.AreEqual(84, w.Stream.LengthBits);
			Assert.AreEqual(10, w.Stream.Length);
			Assert.AreEqual(11, w.Stream.BaseStream.Length);
			byte[] exp1 = new byte[] { 0x0, 0x0, 0x0, 0x0, 0x5f, 0xf1, 0x23, 0x45, 0x67, 0x89, 0xa0 };
			Assert.AreEqual(exp1, w.Stream.ToArray());

			var w1 = new BitStreamWriter(Endian.Little);
			w1.Write(0x12345678, 32);
			w1.Write(0xabc, 12);

			Assert.AreEqual(44, w1.Stream.LengthBits);
			Assert.AreEqual(5, w1.Stream.Length);
			Assert.AreEqual(6, w1.Stream.BaseStream.Length);
			byte[] exp2 = new byte[] { 0x78, 0x56, 0x34, 0x12, 0xbc, 0xa0 };
			Assert.AreEqual(exp2, w1.Stream.ToArray());

			var w2 = new BitStreamWriter(Endian.Little);
			w2.Write(1, 1);
			w2.Write(0, 1);
			w2.Write(0xffff, 6);

			Assert.AreEqual(8, w2.Stream.LengthBits);
			Assert.AreEqual(1, w2.Stream.Length);
			Assert.AreEqual(1, w2.Stream.BaseStream.Length);
			byte[] exp3 = new byte[] { 0xbf };
			Assert.AreEqual(exp3, w2.Stream.ToArray());
		}

		[Test]
		public void ReadBitStream()
		{
			var bs = new BitStream();
			bs.Write(new byte[] { 0x11, 0x27, 0x33, 0x44, 0x55 }, 0, 5);
			bs.SeekBits(0, SeekOrigin.Begin);
			var in1 = bs.SliceBits(8 + 4);
			var in2 = bs.SliceBits(2);
			var in3 = bs.SliceBits(2 + 16 + 4);
			var in4 = in3.SliceBits(16);

			Assert.AreEqual(new byte[] { 0x11, 0x20 }, in1.ToArray());
			Assert.AreEqual(new byte[] { 0x40 }, in2.ToArray());
			Assert.AreEqual(new byte[] { 0xcc, 0xd1, 0x14 }, in3.ToArray());
			Assert.AreEqual(new byte[] { 0xcc, 0xd1 }, in4.ToArray());

			bs.Seek(1, SeekOrigin.Begin);
			var s1 = bs.SliceBits(24);

			var b1 = s1.ReadByte();
			Assert.AreEqual(0x27, b1);

			var b2 = s1.ReadByte();
			Assert.AreEqual(0x33, b2);

			var b3 = s1.ReadByte();
			Assert.AreEqual(0x44, b3);

			var b4 = s1.ReadByte();
			Assert.AreEqual(-1, b4);

			s1.SeekBits(-1, SeekOrigin.End);

			var b5 = s1.ReadBit();
			Assert.AreNotEqual(-1, b5);

			var b6 = s1.ReadBit();
			Assert.AreEqual(-1, b6);

			s1.SeekBits(0, SeekOrigin.End);

			ulong bits;
			var cnt = s1.ReadBits(out bits, 1);
			Assert.AreEqual(0, cnt);
		}

		[Test]
		public void TestSlice()
		{
			var bs = new BitStream();
			bs.SetLength(100);

			Assert.AreEqual(0, bs.Position);
			bs.Position = 75;

			var bs2 = bs.SliceBits(25 * 8);
			Assert.AreEqual(100, bs.Position);
			Assert.AreEqual(0, bs2.Position);
			Assert.AreEqual(25, bs2.Length);

			bs2.Position = 20;

			var bs3 = bs2.SliceBits(5 * 8);
			Assert.AreEqual(25, bs2.Position);
			Assert.AreEqual(0, bs3.Position);
			Assert.AreEqual(5, bs3.Length);

		}

		[Test]
		public void ReadString()
		{
			var bs = new BitStream();
			var writer = new BitWriter(bs);
			var reader = new BitReader(bs);

			writer.WriteString("Hello");
			Assert.AreEqual(5, bs.Length);
			Assert.AreEqual(5, bs.Position);
			bs.Seek(1, SeekOrigin.Begin);

			var str = reader.ReadString();
			Assert.AreEqual("ello", str);

			bs.Seek(5, SeekOrigin.Begin);
			writer.WriteBits(0xf, 4);
			bs.Seek(0, SeekOrigin.Begin);

			// 4-bits of trailer is bad
			try
			{
				reader.ReadString();
				Assert.Fail("should throw");
			}
			catch (IOException ex)
			{
				Assert.AreEqual("Couldn't convert last 4 bits into string.", ex.Message);
			}

			bs.Seek(0, SeekOrigin.Begin);
			writer.WriteString("Hello", Encoding.UTF32);
			Assert.AreEqual(20, bs.Length);
			Assert.AreEqual(20, bs.Position);

			bs.Seek(0, SeekOrigin.Begin);
			bs.SetLength(bs.Length - 1);

			// 3-bytes of trailer is bad
			try
			{
				reader.ReadString(Encoding.UTF32);
				Assert.Fail("should throw");
			}
			catch (IOException ex)
			{
				Assert.AreEqual("Couldn't convert last 3 bytes into string.", ex.Message);
			}
		}

		[Test]
		public void ReadWriteBit()
		{
			var bs = new BitStream();
			bs.Write(new byte[] { 0, 0, 0, 0 }, 0, 4);

			bs.Seek(0, SeekOrigin.Begin);

			for (int i = 0; i < bs.LengthBits; ++i)
			{
				int b = bs.ReadBit();
				Assert.AreEqual(0, b);
				Assert.AreEqual(i + 1, bs.PositionBits);
				Assert.AreEqual(bs.PositionBits / 8, bs.Position);
			}

			bs.Seek(0, SeekOrigin.Begin);

			for (int i = 0; i < bs.LengthBits; ++i)
			{
				var bit = bs.ReadBit();
				Assert.AreNotEqual(-1, bit);

				bs.SeekBits(-1, SeekOrigin.Current);
				bs.WriteBit(1);

				Assert.AreEqual(i + 1, bs.PositionBits);
				Assert.AreEqual(bs.PositionBits / 8, bs.Position);

				bs.Seek(i / 8, SeekOrigin.Begin);

				var b = bs.ReadByte();

				bs.SeekBits(i + 1, SeekOrigin.Begin);

				var shift = 7 - (i % 8);
				var exp = 0xff & (~((1 << shift) - 1));

				// Bits should start filling in, 1 at a time, left to right
				Assert.AreEqual(exp, b);
			}

			bs.SeekBits(46, SeekOrigin.Begin);

			bs.WriteBit(1);

			Assert.AreEqual(47, bs.PositionBits);
			Assert.AreEqual(47, bs.LengthBits);
			Assert.AreEqual(47/8, bs.Position);

			bs.WriteBit(1);

			Assert.AreEqual(48, bs.PositionBits);
			Assert.AreEqual(48, bs.LengthBits);
			Assert.AreEqual(6, bs.Position);
			Assert.AreEqual(6, bs.Length);

			bs.Seek(-1, SeekOrigin.Current);

			var b1 = bs.ReadByte();
			Assert.AreEqual(3, b1);

			var lst = new BitStreamList();
			lst.Add(new BitStream(new MemoryStream(new byte[] { 0xff })));
			lst.Add(new BitStream(new MemoryStream(new byte[] { 0xff })));
			lst.Add(new BitStream(new MemoryStream(new byte[] { 0xff })));
			lst.Add(new BitStream(new MemoryStream(new byte[] { 0xff })));

			for (int i = 0; i < lst.LengthBits; ++i)
			{
				var b = lst.ReadBit();
				Assert.AreEqual(1, b);
				Assert.AreEqual(i + 1, lst.PositionBits);
			}

			Assert.AreEqual(lst.LengthBits, lst.PositionBits);
		}
	}
}

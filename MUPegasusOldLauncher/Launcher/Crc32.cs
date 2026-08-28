using System;
using System.Security.Cryptography;

// Token: 0x02000002 RID: 2
public class Crc32 : HashAlgorithm
{
	// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
	public Crc32()
	{
		this.table = Crc32.InitializeTable(3988292384U);
		this.seed = uint.MaxValue;
		this.Initialize();
	}

	// Token: 0x06000002 RID: 2 RVA: 0x00002075 File Offset: 0x00000275
	public Crc32(uint polynomial, uint seed)
	{
		this.table = Crc32.InitializeTable(polynomial);
		this.seed = seed;
		this.Initialize();
	}

	// Token: 0x06000003 RID: 3 RVA: 0x00002096 File Offset: 0x00000296
	public override void Initialize()
	{
		this.hash = this.seed;
	}

	// Token: 0x06000004 RID: 4 RVA: 0x000020A4 File Offset: 0x000002A4
	protected override void HashCore(byte[] buffer, int start, int length)
	{
		this.hash = Crc32.CalculateHash(this.table, this.hash, buffer, start, length);
	}

	// Token: 0x06000005 RID: 5 RVA: 0x000020C0 File Offset: 0x000002C0
	protected override byte[] HashFinal()
	{
		byte[] array = this.UInt32ToBigEndianBytes(~this.hash);
		this.HashValue = array;
		return array;
	}

	// Token: 0x17000001 RID: 1
	// (get) Token: 0x06000006 RID: 6 RVA: 0x000020E3 File Offset: 0x000002E3
	public override int HashSize
	{
		get
		{
			return 32;
		}
	}

	// Token: 0x06000007 RID: 7 RVA: 0x000020E7 File Offset: 0x000002E7
	public static uint Compute(byte[] buffer)
	{
		return ~Crc32.CalculateHash(Crc32.InitializeTable(3988292384U), uint.MaxValue, buffer, 0, buffer.Length);
	}

	// Token: 0x06000008 RID: 8 RVA: 0x000020FF File Offset: 0x000002FF
	public static uint Compute(uint seed, byte[] buffer)
	{
		return ~Crc32.CalculateHash(Crc32.InitializeTable(3988292384U), seed, buffer, 0, buffer.Length);
	}

	// Token: 0x06000009 RID: 9 RVA: 0x00002117 File Offset: 0x00000317
	public static uint Compute(uint polynomial, uint seed, byte[] buffer)
	{
		return ~Crc32.CalculateHash(Crc32.InitializeTable(polynomial), seed, buffer, 0, buffer.Length);
	}

	// Token: 0x0600000A RID: 10 RVA: 0x0000212C File Offset: 0x0000032C
	private static uint[] InitializeTable(uint polynomial)
	{
		if (polynomial == 3988292384U && Crc32.defaultTable != null)
		{
			return Crc32.defaultTable;
		}
		uint[] array = new uint[256];
		for (int i = 0; i < 256; i++)
		{
			uint num = (uint)i;
			for (int j = 0; j < 8; j++)
			{
				if ((num & 1U) == 1U)
				{
					num = (num >> 1 ^ polynomial);
				}
				else
				{
					num >>= 1;
				}
			}
			array[i] = num;
		}
		if (polynomial == 3988292384U)
		{
			Crc32.defaultTable = array;
		}
		return array;
	}

	// Token: 0x0600000B RID: 11 RVA: 0x0000219C File Offset: 0x0000039C
	private static uint CalculateHash(uint[] table, uint seed, byte[] buffer, int start, int size)
	{
		uint num = seed;
		for (int i = start; i < size; i++)
		{
			num = (num >> 8 ^ table[(int)((uint)buffer[i] ^ (num & 255U))]);
		}
		return num;
	}

	// Token: 0x0600000C RID: 12 RVA: 0x000021CB File Offset: 0x000003CB
	private byte[] UInt32ToBigEndianBytes(uint x)
	{
		return new byte[]
		{
			(byte)(x >> 24 & 255U),
			(byte)(x >> 16 & 255U),
			(byte)(x >> 8 & 255U),
			(byte)(x & 255U)
		};
	}

	// Token: 0x04000001 RID: 1
	public const uint DefaultPolynomial = 3988292384U;

	// Token: 0x04000002 RID: 2
	public const uint DefaultSeed = 4294967295U;

	// Token: 0x04000003 RID: 3
	private uint hash;

	// Token: 0x04000004 RID: 4
	private uint seed;

	// Token: 0x04000005 RID: 5
	private uint[] table;

	// Token: 0x04000006 RID: 6
	private static uint[] defaultTable;
}

using System.Runtime.InteropServices;

namespace SEClient.Tcp;

/// <summary>
/// Convenient byte-level manipulation of 16b integers,
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal struct ItoH16
{
    [FieldOffset(0)]
    public byte B0;
    [FieldOffset(1)]
    public byte B1;
    [FieldOffset(0)]
    public short Int;
    [FieldOffset(0)]
    public ushort UInt;

    public ItoH16(ushort value)
    {
        B0 = 0;
        B1 = 0;
        Int = 0;
        UInt = value;
    }

    public ItoH16(byte[] bytes)
    {
        Int = 0;
        UInt = 0;
        B0 = bytes[1];
        B1 = bytes[0];
    }

    public readonly byte[] AsArray => new byte[] { B0, B1 };
}

/// <summary>
/// Convenient byte-level manipulation of 32b integers and floats
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal struct ItoH32
{
    [FieldOffset(0)]
    public byte B0;
    [FieldOffset(1)]
    public byte B1;
    [FieldOffset(2)]
    public byte B2;
    [FieldOffset(3)]
    public byte B3;
    [FieldOffset(0)]
    public ushort W0;
    [FieldOffset(2)]
    public ushort W1;
    [FieldOffset(0)]
    public int Int;
    [FieldOffset(0)]
    public uint UInt;
    [FieldOffset(0)]
    public float Float;

    public ItoH32(double value) : this((float)value) { }

    public ItoH32(float value)
    {
        B0 = 0;
        B1 = 0;
        B2 = 0;
        B3 = 0;
        W0 = 0;
        W1 = 0;
        Int = 0;
        UInt = 0;
        Float = value;
    }

    public ItoH32(uint value)
    {
        B0 = 0;
        B1 = 0;
        B2 = 0;
        B3 = 0;
        W0 = 0;
        W1 = 0;
        Float = 0;
        Int = 0;
        UInt = value;
    }

    public ItoH32(byte[] bytes)
    {
        W0 = 0;
        W1 = 0;
        Int = 0;
        UInt = 0;
        Float = 0;
        B0 = bytes[3];
        B1 = bytes[2];
        B2 = bytes[1];
        B3 = bytes[0];
    }

    public readonly byte[] AsArray => new byte[] { B0, B1, B2, B3 };
}


/// <summary>
/// Convenient byte-level manipulation of 64b integers and doubles
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal struct ItoH64
{
    [FieldOffset(0)]
    public byte B0;
    [FieldOffset(1)]
    public byte B1;
    [FieldOffset(2)]
    public byte B2;
    [FieldOffset(3)]
    public byte B3;
    [FieldOffset(4)]
    public byte B4;
    [FieldOffset(5)]
    public byte B5;
    [FieldOffset(6)]
    public byte B6;
    [FieldOffset(7)]
    public byte B7;
    [FieldOffset(0)]
    public ushort W0;
    [FieldOffset(2)]
    public ushort W1;
    [FieldOffset(4)]
    public ushort W2;
    [FieldOffset(6)]
    public ushort W3;
    [FieldOffset(0)]
    public uint D0;
    [FieldOffset(4)]
    public uint D1;
    [FieldOffset(0)]
    public long Int;
    [FieldOffset(0)]
    public ulong UInt;
    [FieldOffset(0)]
    public double Float;

    public ItoH64(double value)
    {
        B0 = 0;
        B1 = 0;
        B2 = 0;
        B3 = 0;
        B4 = 0;
        B5 = 0;
        B6 = 0;
        B7 = 0;
        W0 = 0;
        W1 = 0;
        W2 = 0;
        W3 = 0;
        D0 = 0;
        D1 = 0;
        Int = 0;
        UInt = 0;
        Float = value;
    }

    public ItoH64(ulong value)
    {
        B0 = 0;
        B1 = 0;
        B2 = 0;
        B3 = 0;
        B4 = 0;
        B5 = 0;
        B6 = 0;
        B7 = 0;
        W0 = 0;
        W1 = 0;
        W2 = 0;
        W3 = 0;
        D0 = 0;
        D1 = 0;
        Float = 0;
        Int = 0;
        UInt = value;
    }

    public ItoH64(byte[] bytes)
    {
        W0 = 0;
        W1 = 0;
        W2 = 0;
        W3 = 0;
        D0 = 0;
        D1 = 0;
        Float = 0;
        Int = 0;
        UInt = 0;
        B0 = bytes[7];
        B1 = bytes[6];
        B2 = bytes[5];
        B3 = bytes[4];
        B4 = bytes[3];
        B5 = bytes[2];
        B6 = bytes[1];
        B7 = bytes[0];
    }

    public readonly byte[] AsArray => new byte[] { B0, B1, B2, B3, B4, B5, B6, B7 };
}

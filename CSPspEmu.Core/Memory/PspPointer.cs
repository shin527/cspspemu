﻿using System;
using System.Runtime.InteropServices;

namespace CSPspEmu.Core.Memory
{
	[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
	public unsafe struct PspPointer
	{
		public uint Address;

		public uint Low24
		{
			set
			{
				Address = (Address & 0xFF000000) | (value & 0x00FFFFFF);
			}
		}

		public uint High8
		{
			set
			{
				Address = (Address & 0x00FFFFFF) | (value & 0xFF000000);
			}
		}

		public PspPointer(uint Address)
		{
			this.Address = Address;
		}

		public static implicit operator uint(PspPointer that)
		{
			return that.Address;
		}

		public static implicit operator PspPointer(uint that)
		{
			return new PspPointer()
			{
				Address = that,
			};
		}

		public override string ToString()
		{
			return String.Format("PspPointer(0x{0:X})", Address);
		}

		public bool IsNull { get { return Address == 0; } }

		public unsafe void* GetPointer(PspMemory pspMemory, int Size)
		{
			return pspMemory.PspPointerToPointerSafe(this, Size);
		}

		public unsafe void* GetPointer<TType>(PspMemory pspMemory)
		{
			return pspMemory.PspPointerToPointerSafe(this, Marshal.SizeOf(typeof(TType)));
		}

		public unsafe void* GetPointerNotNull<TType>(PspMemory pspMemory)
		{
			var Pointer = this.GetPointer<TType>(pspMemory);
			if (Pointer == null) throw(new NullReferenceException(String.Format("Pointer for {0} can't be null", typeof(TType))));
			return Pointer;
		}

		/*
		public unsafe PspMemoryPointer<TType> GetPspMemoryPointer<TType>(PspMemory pspMemory)
		{
			return new PspMemoryPointer<TType>(pspMemory, Address);
		}
		*/
	}

	/*
	public class Reference<TType>
	{
		public TType Value;
	}

	public class PspMemoryPointer<TType>
	{
		public PspMemory PspMemory;
		public uint Address;

		public PspMemoryPointer(PspMemory PspMemory, uint Address)
		{
			this.PspMemory = PspMemory;
			this.Address = Address;
		}

		public void UpdateIndex(Action<Reference<TType>> Action)
		{
		}
	}
	*/

	/*
	public struct PspPointer<TType>
	{
		public uint Address;

		public PspPointer(uint Address)
		{
			this.Address = Address;
		}
	}
	*/
}

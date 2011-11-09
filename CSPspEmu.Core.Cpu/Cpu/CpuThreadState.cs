﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using CSharpUtils.Extensions;
using CSPspEmu.Core.Cpu.Cpu;
using CSharpUtils.Threading;

namespace CSPspEmu.Core.Cpu
{
	unsafe sealed public class CpuThreadState
	{
		public Processor Processor;
		public object ModuleObject;

		public int StepInstructionCount;
		public long TotalInstructionCount;

		public uint PC;
		//public uint nPC;

		public int LO, HI;

		public bool BranchFlag;

		public uint GPR0, GPR1, GPR2, GPR3, GPR4, GPR5, GPR6, GPR7, GPR8, GPR9, GPR10, GPR11, GPR12, GPR13, GPR14, GPR15, GPR16, GPR17, GPR18, GPR19, GPR20, GPR21, GPR22, GPR23, GPR24, GPR25, GPR26, GPR27, GPR28, GPR29, GPR30, GPR31;
		public float FPR0, FPR1, FPR2, FPR3, FPR4, FPR5, FPR6, FPR7, FPR8, FPR9, FPR10, FPR11, FPR12, FPR13, FPR14, FPR15, FPR16, FPR17, FPR18, FPR19, FPR20, FPR21, FPR22, FPR23, FPR24, FPR25, FPR26, FPR27, FPR28, FPR29, FPR30, FPR31;

		// http://msdn.microsoft.com/en-us/library/ms253512(v=vs.80).aspx
		// http://logos.cs.uic.edu/366/notes/mips%20quick%20tutorial.htm

		/// <summary>
		/// Points to the middle of the 64K block of memory in the static data segment.
		/// </summary>
		public uint GP
		{
			get { return GPR28; }
			set { GPR28 = value; }
		}

		/// <summary>
		/// Points to last location on the stack.
		/// </summary>
		public uint SP
		{
			get { return GPR29; }
			set { GPR29 = value; }
		}

		/// <summary>
		/// saved value / frame pointer
		/// Preserved across procedure calls
		/// </summary>
		public uint FP
		{
			get { return GPR30; }
			set { GPR30 = value; }
		}

		/// <summary>
		/// Return Address
		/// </summary>
		public uint RA
		{
			get { return GPR31; }
			set { GPR31 = value; }
		}

		/*
		public struct FixedRegisters
		{
			public fixed uint GPR[32];
			public fixed float FPR[32];
		}

		public FixedRegisters Registers;
		*/

		public class GprList
		{
			public CpuThreadState Processor;

			public int this[int Index]
			{
				get
				{
					fixed (uint* PTR = &Processor.GPR0)
					{
						return (int)PTR[Index];
					}
				}
				set
				{
					fixed (uint* PTR = &Processor.GPR0)
					{
						PTR[Index] = (uint)value;
					}
				}
			}
		}

		public class FprList
		{
			public CpuThreadState Processor;

			public float this[int Index]
			{
				get
				{
					fixed (float* PTR = &Processor.FPR0)
					{
						return PTR[Index];
					}
				}
				set
				{
					fixed (float* PTR = &Processor.FPR0)
					{
						PTR[Index] = value;
					}
				}
			}
		}

		//public uint *GPR_Ptr;
		//public float* FPR_Ptr;

		public GprList GPR;
		public FprList FPR;
		//readonly public float* FPR;

		public void* GetMemoryPtr(uint Address)
		{
			var Pointer = Processor.Memory.PspAddressToPointer(Address);
			//Console.WriteLine("%08X".Sprintf((uint)Pointer));
			return Pointer;
		}

		public IEnumerable<int> GPRList(params int[] Indexes)
		{
			return Indexes.Select(Index => {
				fixed (uint *PTR = &GPR0)
				{
					return (int)PTR[Index];
				}
			});
		}

		public CpuThreadState(Processor Processor)
		{
			this.Processor = Processor;

			GPR = new GprList() { Processor = this };
			FPR = new FprList() { Processor = this };

			for (int n = 0; n < 32; n++)
			{
				GPR[n] = 0;
				FPR[n] = 0;
			}
		}

		~CpuThreadState()
		{
			//Marshal.FreeHGlobal(new IntPtr(GPR));
			//Marshal.FreeHGlobal(new IntPtr(FPR));
		}

		/*
		public void Test()
		{
			GPR[0] = 0;
		}

		public uint LoadGPR(int R)
		{
			return (uint)GPR[R];
		}

		public void SaveGPR(int R, uint V)
		{
			GPR[R] = (int)V;
		}

		static public void TestBranchFlag(Processor Processor)
		{
			Processor.BranchFlag = (Processor.GPR[2] == Processor.GPR[2]);
		}

		static public void TestGPR(Processor Processor)
		{
			Processor.GPR[1] = Processor.GPR[2] + Processor.GPR[2];
		}
		*/

		public void Syscall(int Code)
		{
			Processor.Syscall(Code, this);
		}

		/*
		static public void TestMemset(CpuThreadState Processor)
		{
			*((byte*)Processor.GetMemoryPtr(0x04000000)) = 0x77;
		}
		*/

		public void Yield()
		{
			//Console.WriteLine(StepInstructionCount);
			GreenThread.Yield();
			//Console.WriteLine(StepInstructionCount);
		}

		public void Trace(uint PC)
		{
			Console.WriteLine("  Trace: {0:X}", PC);
		}

		public void BreakpointIfEnabled()
		{
		}
	}
}

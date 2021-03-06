﻿//#define DEBUG_FUNCTION_CREATION

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;
using CSPspEmu.Core.Cpu.Assembler;
using CSPspEmu.Core.Cpu.VFpu;
using CSPspEmu.Core.Memory;
using CSharpUtils.Threading;
using CSPspEmu.Core.Cpu.InstructionCache;
using CSPspEmu.Core.Cpu.Dynarec;
using CSharpUtils;

namespace CSPspEmu.Core.Cpu
{
	unsafe delegate void* GetMemoryPtrSafeWithErrorDelegate(uint Address, String ErrorDescription, bool CanBeNull);
	unsafe delegate void* GetMemoryPtrNotNullDelegate(uint Address);

	unsafe sealed public partial class CpuThreadState
	{
		static public readonly CpuThreadState Methods = new CpuThreadState();

		public CpuProcessor CpuProcessor;

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PspMemory Memory
		{
			get
			{
				return CpuProcessor.Memory;
			}
		}
		public MethodCache MethodCache;

		public object CallerModule;

		public int StepInstructionCount;
		public long TotalInstructionCount;

		/// <summary>
		/// Las Valid Registered PC
		/// </summary>
		public uint LastValidPC = 0xFFFFFFFF;

		/// <summary>
		/// Current PC
		/// </summary>
		public uint PC;
		//public uint nPC;

		/// <summary>
		/// LOw, HIgh registers.
		/// Used for mult/div.
		/// </summary>
		public int LO, HI;

		/// <summary>
		/// 
		/// </summary>
		public uint IC;

		/////////////////////////////////////////////////////////////////////////////////////////////////
		// GPR: General Purporse Registers
		// FPR: Floating Point Registers
		// VFR: Vfpu registers
		// C0R: Cop0 registers
		// VFR_CC: Vfpu comparison flags
		/////////////////////////////////////////////////////////////////////////////////////////////////
		public uint GPR0, GPR1, GPR2, GPR3, GPR4, GPR5, GPR6, GPR7, GPR8, GPR9, GPR10, GPR11, GPR12, GPR13, GPR14, GPR15, GPR16, GPR17, GPR18, GPR19, GPR20, GPR21, GPR22, GPR23, GPR24, GPR25, GPR26, GPR27, GPR28, GPR29, GPR30, GPR31;
		public float FPR0, FPR1, FPR2, FPR3, FPR4, FPR5, FPR6, FPR7, FPR8, FPR9, FPR10, FPR11, FPR12, FPR13, FPR14, FPR15, FPR16, FPR17, FPR18, FPR19, FPR20, FPR21, FPR22, FPR23, FPR24, FPR25, FPR26, FPR27, FPR28, FPR29, FPR30, FPR31;
		public uint C0R0, C0R1, C0R2, C0R3, C0R4, C0R5, C0R6, C0R7, C0R8, C0R9, C0R10, C0R11, C0R12, C0R13, C0R14, C0R15, C0R16, C0R17, C0R18, C0R19, C0R20, C0R21, C0R22, C0R23, C0R24, C0R25, C0R26, C0R27, C0R28, C0R29, C0R30, C0R31;

		public float VFR0, VFR1, VFR2, VFR3, VFR4, VFR5, VFR6, VFR7, VFR8, VFR9, VFR10, VFR11, VFR12, VFR13, VFR14, VFR15, VFR16, VFR17, VFR18, VFR19, VFR20, VFR21, VFR22, VFR23, VFR24, VFR25, VFR26, VFR27, VFR28, VFR29, VFR30, VFR31, VFR32, VFR33, VFR34, VFR35, VFR36, VFR37, VFR38, VFR39, VFR40, VFR41, VFR42, VFR43, VFR44, VFR45, VFR46, VFR47, VFR48, VFR49, VFR50, VFR51, VFR52, VFR53, VFR54, VFR55, VFR56, VFR57, VFR58, VFR59, VFR60, VFR61, VFR62, VFR63, VFR64, VFR65, VFR66, VFR67, VFR68, VFR69, VFR70, VFR71, VFR72, VFR73, VFR74, VFR75, VFR76, VFR77, VFR78, VFR79, VFR80, VFR81, VFR82, VFR83, VFR84, VFR85, VFR86, VFR87, VFR88, VFR89, VFR90, VFR91, VFR92, VFR93, VFR94, VFR95, VFR96, VFR97, VFR98, VFR99, VFR100, VFR101, VFR102, VFR103, VFR104, VFR105, VFR106, VFR107, VFR108, VFR109, VFR110, VFR111, VFR112, VFR113, VFR114, VFR115, VFR116, VFR117, VFR118, VFR119, VFR120, VFR121, VFR122, VFR123, VFR124, VFR125, VFR126, VFR127;
		public bool VFR_CC_0, VFR_CC_1, VFR_CC_2, VFR_CC_3, VFR_CC_4, VFR_CC_5, VFR_CC_6, VFR_CC_7;

		public bool VFR_CC_ANY { get { return VFR_CC_4; } }
		public bool VFR_CC_ALL { get { return VFR_CC_5; } }

		public uint VFR_CC_Value
		{
			get
			{
				uint Value = 0;
				fixed (bool* VFR_CC = &VFR_CC_0)
				{
					for (int n = 0; n < 8; n++) Value |= (uint)(VFR_CC[n] ? (1 << n) : 0);
				}
				return Value;
			}
		}

		public VfpuPrefix PrefixNone;
		public VfpuPrefix PrefixSource;
		public VfpuPrefix PrefixDestination;
		public VfpuPrefix PrefixTarget;

		public Random Random = new Random();

		public FCR31 Fcr31;

		public readonly uint[] CallStack = new uint[10240];
		public int CallStackCount;

		public uint[] GetCurrentCallStack()
		{
			var Out = new List<uint>();
			var Count = Math.Min(10240, CallStackCount);
			for (int n = 0; n < Count; n++)
			{
				Out.Add(CallStack[(CallStackCount - n - 1) % CallStack.Length]);
			}
			return Out.ToArray();
		}

		public void CallStackPush(uint PC)
		{
			if (CallStackCount >= 0 && CallStackCount < CallStack.Length)
			{
				CallStack[CallStackCount] = PC;
			}
			CallStackCount++;
		}

		public void CallStackPop()
		{
			if (CallStackCount > 0) CallStackCount--;
		}

		// http://msdn.microsoft.com/en-us/library/ms253512(v=vs.80).aspx
		// http://logos.cs.uic.edu/366/notes/mips%20quick%20tutorial.htm

		/// <summary>
		/// Points to the middle of the 64K block of memory in the static data segment.
		/// </summary>
		public uint GP { get { return GPR28; } set { GPR28 = value; } }

		/// <summary>
		/// Points to last location on the stack.
		/// </summary>
		public uint SP { get { return GPR29; } set { GPR29 = value; } }

		/// <summary>
		/// Reserved for use by the interrupt/trap handler 
		/// </summary>
		public uint K0 { get { return GPR26; } set { GPR26 = value; } }

		/// <summary>
		/// saved value / frame pointer
		/// Preserved across procedure calls
		/// </summary>
		public uint FP { get { return GPR30; } set { GPR30 = value; } }

		/// <summary>
		/// Return Address
		/// </summary>
		public uint RA { get { return GPR31; } set { GPR31 = value; } }

		/// <summary>
		/// V0
		/// </summary>
		public uint V0 { get { return GPR2; } set { GPR2 = value; } }

		public GprList GPR;
		public C0rList C0R;
		public FprList FPR;
		public VfprList Vfpr;
		public FprListInteger FPR_I;
		//readonly public float* FPR;

		public void* GetMemoryPtr(uint Address)
		{
			var Pointer = Memory.PspAddressToPointerUnsafe(Address);
			//Console.WriteLine("%08X".Sprintf((uint)Pointer));
			return Pointer;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public byte Read1(uint Address) { return Memory.Read1(Address); }
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ushort Read2(uint Address) { return Memory.Read2(Address); }
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint Read4(uint Address) { return Memory.Read4(Address); }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float Read4F(uint Address) { var Value = Memory.Read4(Address); return *(float *)&Value; }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write1(uint Address, byte Value) { Memory.Write1(Address, Value); }
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write2(uint Address, ushort Value) { Memory.Write2(Address, Value); }
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write4(uint Address, uint Value) { Memory.Write4(Address, Value); }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write4F(uint Address, float Value) { Memory.Write4(Address, *(uint *)&Value); }

		public void* GetMemoryPtrNotNull(uint Address)
		{
			return Memory.PspAddressToPointerNotNull(Address);
		}

		public void* GetMemoryPtrSafe(uint Address)
		{
			return Memory.PspAddressToPointerSafe(Address, 0);
		}

		public void* GetMemoryPtrSafeWithError(uint Address, String ErrorDescription, bool CanBeNull)
		{
			try
			{
				void *Result = Memory.PspAddressToPointerSafe(Address, 0, CanBeNull);
				/*
				if (Result == null && !CanBeNull)
				{
					throw(new PspMemory.InvalidAddressException(""));
				}
				*/
				return Result;
			}
			catch (PspMemory.InvalidAddressException InvalidAddressException)
			{
				throw (new PspMemory.InvalidAddressException("GetMemoryPtrSafeWithError:" + ErrorDescription + " : " + InvalidAddressException.Message, InvalidAddressException));
			}
			catch (Exception Exception)
			{
				throw (new Exception("GetMemoryPtrSafeWithError: " + ErrorDescription + " : " + Exception.Message, Exception));
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="Indexes"></param>
		/// <returns></returns>
		public IEnumerable<int> GPRList(params int[] Indexes)
		{
			return Indexes.Select(Index => GPR[Index]);
		}

		private CpuThreadState()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Processor"></param>
		public CpuThreadState(CpuProcessor Processor)
		{
			this.CpuProcessor = Processor;
			this.MethodCache = Processor.MethodCache;
			//this.Memory = Processor.Memory;

			GPR = new GprList() { CpuThreadState = this };
			FPR = new FprList() { CpuThreadState = this };
			C0R = new C0rList() { CpuThreadState = this };
			FPR_I = new FprListInteger() { CpuThreadState = this };
			Vfpr = new VfprList() { CpuThreadState = this };

			for (int n = 0; n < 32; n++)
			{
				GPR[n] = 0;
				FPR[n] = 0.0f;
			}
		}

		/// <summary>
		/// Calls a syscall.
		/// </summary>
		/// <param name="Code"></param>
		public void Syscall(int Code)
		{
			CpuProcessor.Syscall(Code, this);
		}

		//private DateTime LastTick;
		private int TickCount = 0;

		DateTime LastTickYield = DateTime.UtcNow;

		/// <summary>
		/// Function called on some situations, that allow
		/// to yield the thread.
		/// </summary>
		public void Tick()
		{
			TickCount++;
			if (TickCount > 10000)
			{
				TickCount = 0;
				if ((DateTime.UtcNow - LastTickYield).TotalMilliseconds >= 2)
				{
					LastTickYield = DateTime.UtcNow;
					Yield();
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public void Yield()
		{
			//Console.WriteLine(StepInstructionCount);
			if (CpuProcessor.PspConfig.UseCoRoutines)
			{
				CpuProcessor.CoroutinePool.YieldInPool();
			}
			else
			{
				GreenThread.Yield();
			}
			//Console.WriteLine(StepInstructionCount);
		}

		/// <summary>
		/// 
		/// </summary>
		static MipsDisassembler MipsDisassembler;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="PC"></param>
		public void Trace(uint PC)
		{
			if (MipsDisassembler == null) MipsDisassembler = new MipsDisassembler();
			var Result = MipsDisassembler.Disassemble(PC, (Instruction)Memory.Read4(PC));
			Console.WriteLine("  Trace: PC:0x{0:X8} : DATA:0x{1:X8} : {2}", PC, Memory.Read4(PC), Result);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <see cref="http://msdn.microsoft.com/en-us/library/ms253512(v=vs.80).aspx"/>
		private static readonly string[] RegisterMnemonicNames = new string[] {
			"zr", "at", "v0", "v1", "a0", "a1", "a2", "a3",
			"t0", "t1", "t2", "t3", "t4", "t5", "t6", "t7",
			"s0", "s1", "s2", "s3", "s4", "s5", "s6", "s7",
			"t8", "t9", "k0", "k1", "gp", "sp", "fp", "ra", 
		};

		/// <summary>
		/// 
		/// </summary>
		public void DumpRegisters()
		{
			DumpRegisters(Console.Out);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="TextWriter"></param>
		public void DumpRegistersCpu(TextWriter TextWriter)
		{
			for (int n = 0; n < 32; n++)
			{
				if (n % 4 != 0) TextWriter.Write(", ");
				TextWriter.Write("r{0,2}({1}) : 0x{2:X8}", n, RegisterMnemonicNames[n], GPR[n]);
				if (n % 4 == 3) TextWriter.WriteLine();
			}
			TextWriter.WriteLine();
		}

		public void DumpRegistersFpu(TextWriter TextWriter)
		{
			for (int n = 0; n < 32; n++)
			{
				if (n % 4 != 0) TextWriter.Write(", ");
				TextWriter.Write("f{0,2} : 0x{1:X8}, {2}", n, FPR_I[n], FPR[n]);
				if (n % 4 == 3) TextWriter.WriteLine();
			}
			TextWriter.WriteLine();
		}

		public void DumpRegistersVFpu(TextWriter TextWriter)
		{
			for (int n = 0; n < 32; n++)
			{
				if (n % 4 != 0) TextWriter.Write(", ");
				TextWriter.Write("c0r{0,2} : 0x{1:X8}", n, C0R[n]);
				if (n % 4 == 3) TextWriter.WriteLine();
			}
			TextWriter.WriteLine();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="TextWriter"></param>
		public void DumpRegisters(TextWriter TextWriter)
		{
			DumpRegistersCpu(TextWriter);
			DumpRegistersFpu(TextWriter);
			DumpRegistersVFpu(TextWriter);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="TextWriter"></param>
		public void DumpVfpuRegisters(TextWriter TextWriter)
		{
			for (int Matrix = 0; Matrix < 8; Matrix++)
			{
				TextWriter.WriteLine("Matrix: {0}", Matrix);
				for (int Row = 0; Row < 4; Row++)
				{
					for (int Column = 0; Column < 4; Column++)
					{
						TextWriter.Write(", {0}", Vfpr[Matrix, Column, Row]);
					}
					TextWriter.WriteLine("");
				}
				TextWriter.WriteLine("");
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="that"></param>
		public unsafe void CopyRegistersFrom(CpuThreadState that)
		{
			this.PC = that.PC;
			this.Fcr31 = that.Fcr31;
			this.IC = that.IC;
			this.LO = that.LO;
			this.HI = that.HI;
			fixed (float* ThisFPR = &this.FPR0)
			fixed (float* ThatFPR = &that.FPR0)
			fixed (uint* ThisGPR = &this.GPR0)
			fixed (uint* ThatGPR = &that.GPR0)
			{
				for (int n = 0; n < 32; n++)
				{
					ThisFPR[n] = ThatFPR[n];
					ThisGPR[n] = ThatGPR[n];
				}
			}

			fixed (float* ThisVFR = &this.VFR0)
			fixed (float* ThatVFR = &that.VFR0)
			{
				for (int n = 0; n < 128; n++)
				{
					ThisVFR[n] = ThatVFR[n];
				}
			}
		}

		public void ExecuteAT(uint PC)
		{
			this.PC = PC;
			MethodCache.GetForPC(PC).CallDelegate(this);
		}

		public void _MethodCacheInfo_SetInternal(MethodCacheInfo MethodCacheInfo, uint PC)
		{
			Console.Write("Creating function for PC=0x{0:X8}...", PC);
			var Stopwatch = new Logger.Stopwatch();
			
			var DynarecFunction = CpuProcessor.DynarecFunctionCompiler.CreateFunction(new InstructionStreamReader(new PspMemoryStream(Memory)), PC);
			if (DynarecFunction.EntryPC != PC) throw(new Exception("Unexpected error"));

			var AstGenerationTime = Stopwatch.Tick();

			DynarecFunction.Delegate(null);

			var LinkingTime = Stopwatch.Tick();

			Console.WriteLine("({0}): Ast: {1}ms, Link: {2}ms", (DynarecFunction.MaxPC - DynarecFunction.MinPC) / 4, (int)AstGenerationTime.TotalMilliseconds, (int)LinkingTime.TotalMilliseconds);

			//DynarecFunction.AstNode = DynarecFunction.AstNode.Optimize(CpuProcessor);

#if DEBUG_FUNCTION_CREATION
			CpuProcessor.DebugFunctionCreation = true;
#endif

			if (CpuProcessor.DebugFunctionCreation)
			{
				Console.WriteLine("-------------------------------------");
				Console.WriteLine("Created function for PC=0x{0:X8}", PC);
				Console.WriteLine("-------------------------------------");
				this.DumpRegistersCpu(Console.Out);
				Console.WriteLine("-------------------------------------");
				Console.WriteLine(DynarecFunction.AstNode.ToCSharpString());
				Console.WriteLine("-------------------------------------");
			}

			MethodCacheInfo.AstTree = DynarecFunction.AstNode;
			MethodCacheInfo.StaticField.Value = DynarecFunction.Delegate;
			MethodCacheInfo.EntryPC = DynarecFunction.EntryPC;
			MethodCacheInfo.MinPC = DynarecFunction.MinPC;
			MethodCacheInfo.MaxPC = DynarecFunction.MaxPC;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Address"></param>
		/// <param name="PC"></param>
		public void SetPCWriteAddress(uint Address, uint PC)
		{
			//Console.WriteLine("SetPCWriteAddress: {0:X} : {1:X}", Address, PC);
			Memory.SetPCWriteAddress(Address, PC);
		}
	}
}

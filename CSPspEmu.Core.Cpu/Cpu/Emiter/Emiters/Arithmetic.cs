﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;

namespace CSPspEmu.Core.Cpu.Emiter
{
	sealed public partial class CpuEmiter
	{
		/////////////////////////////////////////////////////////////////////////////////////////////////
		// Arithmetic operations.
		/////////////////////////////////////////////////////////////////////////////////////////////////

		public void add()
		{
			MipsMethodEmiter.OP_3REG(RD, RS, RT, OpCodes.Add);
		}

		public void addu()
		{
			add();
		}

		public void addi()
		{
			MipsMethodEmiter.OP_2REG_IMM(RT, RS, (short)IMM, OpCodes.Add);
		}

		public void addiu()
		{
			addi();
		}

		public void sub()
		{
			MipsMethodEmiter.OP_3REG(RD, RS, RT, OpCodes.Sub);
		}

		public void subu()
		{
			sub();
		}

		/////////////////////////////////////////////////////////////////////////////////////////////////
		// Logical Operations.
		/////////////////////////////////////////////////////////////////////////////////////////////////

		public void and()
		{
			MipsMethodEmiter.OP_3REG(RD, RS, RT, OpCodes.And);
		}

		public void andi()
		{
			MipsMethodEmiter.OP_2REG_IMMU(RT, RS, IMMU, OpCodes.And);
		}

		public void or()
		{
			MipsMethodEmiter.OP_3REG(RD, RS, RT, OpCodes.Or);
		}

		public void ori()
		{
			MipsMethodEmiter.OP_2REG_IMMU(RT, RS, IMMU, OpCodes.Or);
		}

		public void xor()
		{
			MipsMethodEmiter.OP_3REG(RD, RS, RT, OpCodes.Xor);
		}

		public void xori()
		{
			MipsMethodEmiter.OP_2REG_IMMU(RT, RS, IMMU, OpCodes.Xor);
		}

		public void nor()
		{
			or();
			MipsMethodEmiter.ILGenerator.Emit(OpCodes.Not);
		}

		/////////////////////////////////////////////////////////////////////////////////////////////////
		// Shift Left/Right Logical/Arithmethic (Variable).
		/////////////////////////////////////////////////////////////////////////////////////////////////

		public void sll() {
			MipsMethodEmiter.OP_2REG_IMMU(RD, RT, Instruction.POS, OpCodes.Shl);
		}
		public void sllv() {
			MipsMethodEmiter.OP_3REG(RD, RT, RS, OpCodes.Shl);
		}
		public void sra() {
			MipsMethodEmiter.OP_2REG_IMMU(RD, RT, Instruction.POS, OpCodes.Shr);
		}
		public void srav() {
			MipsMethodEmiter.OP_3REG(RD, RT, RS, OpCodes.Shr);
		}
		public void srl() {
			MipsMethodEmiter.OP_2REG_IMMU(RD, RT, Instruction.POS, OpCodes.Shr_Un);
		}
		public void srlv() {
			MipsMethodEmiter.OP_3REG(RD, RT, RS, OpCodes.Shr_Un);
		}
		public void rotr() { throw(new NotImplementedException()); }
		public void rotrv() { throw(new NotImplementedException()); }

		/////////////////////////////////////////////////////////////////////////////////////////////////
		// Set Less Than (Immediate) (Unsigned).
		/////////////////////////////////////////////////////////////////////////////////////////////////
		public void slt() {
			MipsMethodEmiter.SaveGPR(RD, () =>
			{
				MipsMethodEmiter.LoadGPR(RS);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Conv_I4);
				MipsMethodEmiter.LoadGPR(RT);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Conv_I4);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Clt);
			});
		}
		public void sltu()
		{
			MipsMethodEmiter.SaveGPR(RD, () =>
			{
				MipsMethodEmiter.LoadGPR(RS);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Conv_U4);
				MipsMethodEmiter.LoadGPR(RT);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Conv_U4);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Clt);
			});
		}

		public void slti() {
			MipsMethodEmiter.OP_2REG_IMM(RT, RS, (short)Instruction.IMM, OpCodes.Clt);
		}
		public void sltiu() { MipsMethodEmiter.OP_2REG_IMMU(RT, RS, Instruction.IMMU, OpCodes.Clt); }

		/////////////////////////////////////////////////////////////////////////////////////////////////
		// Load Upper Immediate.
		/////////////////////////////////////////////////////////////////////////////////////////////////
		public void lui()
		{
			MipsMethodEmiter.SET(RT, IMMU << 16);
		}

		/////////////////////////////////////////////////////////////////////////////////////////////////
		// Sign Extend Byte/Half word.
		/////////////////////////////////////////////////////////////////////////////////////////////////
		public void seb() {
			MipsMethodEmiter.SaveGPR(RD, () =>
			{
				MipsMethodEmiter.LoadGPR(RT);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Conv_I1);
			});
		}
		public void seh() {
			MipsMethodEmiter.SaveGPR(RD, () =>
			{
				MipsMethodEmiter.LoadGPR(RT);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Conv_I2);
			});
		}

		/////////////////////////////////////////////////////////////////////////////////////////////////
		// BIT REVerse.
		/////////////////////////////////////////////////////////////////////////////////////////////////
		public void bitrev() 
		{
			MipsMethodEmiter.SaveGPR(RD, () =>
			{
				MipsMethodEmiter.LoadGPR(RT);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Call, typeof(CpuEmiter).GetMethod("bitrev_impl"));
			});
			//throw (new NotImplementedException());
		}
		static public uint bitrev_impl(uint v)
		{
			v = ((v >> 1) & 0x55555555) | ((v & 0x55555555) << 1); // swap odd and even bits
			v = ((v >> 2) & 0x33333333) | ((v & 0x33333333) << 2); // swap consecutive pairs
			v = ((v >> 4) & 0x0F0F0F0F) | ((v & 0x0F0F0F0F) << 4); // swap nibbles ... 
			v = ((v >> 8) & 0x00FF00FF) | ((v & 0x00FF00FF) << 8); // swap bytes
			v = (v >> 16) | (v << 16); // swap 2-byte long pairs
			return v;
		}

		/////////////////////////////////////////////////////////////////////////////////////////////////
		// MAXimum/MINimum.
		/////////////////////////////////////////////////////////////////////////////////////////////////
		private void _max_min(OpCode BranchOpCode)
		{
			var LabelIf = MipsMethodEmiter.ILGenerator.DefineLabel();
			var LabelElse = MipsMethodEmiter.ILGenerator.DefineLabel();
			var LabelEnd = MipsMethodEmiter.ILGenerator.DefineLabel();

			MipsMethodEmiter.LoadGPR(RS);
			MipsMethodEmiter.LoadGPR(RT);
			MipsMethodEmiter.ILGenerator.Emit(BranchOpCode, LabelElse);

			// IF
			MipsMethodEmiter.ILGenerator.MarkLabel(LabelIf);
			MipsMethodEmiter.SaveGPR(RD, () =>
			{
				MipsMethodEmiter.LoadGPR(RS);
			});
			MipsMethodEmiter.ILGenerator.Emit(OpCodes.Br, LabelEnd);

			// ELSE
			MipsMethodEmiter.ILGenerator.MarkLabel(LabelElse);
			MipsMethodEmiter.SaveGPR(RD, () =>
			{
				MipsMethodEmiter.LoadGPR(RT);
			});
			MipsMethodEmiter.ILGenerator.Emit(OpCodes.Br, LabelEnd);

			// END
			MipsMethodEmiter.ILGenerator.MarkLabel(LabelEnd);
		}

		public void max() { _max_min(OpCodes.Blt); }
		public void min() { _max_min(OpCodes.Bgt); }

		/////////////////////////////////////////////////////////////////////////////////////////////////
		// DIVide (Unsigned).
		/////////////////////////////////////////////////////////////////////////////////////////////////
		public void div() {
			MipsMethodEmiter.SaveHI(() =>
			{
				MipsMethodEmiter.LoadGPR_Signed(RS);
				MipsMethodEmiter.LoadGPR_Signed(RT);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Rem);
			});
			MipsMethodEmiter.SaveLO(() =>
			{
				MipsMethodEmiter.LoadGPR_Signed(RS);
				MipsMethodEmiter.LoadGPR_Signed(RT);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Div);
			});
		}
		public void divu() {
			div();
			/*
			MipsMethodEmiter.SaveHI(() =>
			{
				MipsMethodEmiter.LoadGPR_Unsigned(RS);
				MipsMethodEmiter.LoadGPR_Unsigned(RT);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Rem);
			});
			MipsMethodEmiter.SaveLO(() =>
			{
				MipsMethodEmiter.LoadGPR_Unsigned(RS);
				MipsMethodEmiter.LoadGPR_Unsigned(RT);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Div);
			});
			*/
		}

		/////////////////////////////////////////////////////////////////////////////////////////////////
		// MULTiply (Unsigned).
		/////////////////////////////////////////////////////////////////////////////////////////////////
		unsafe static public void _mult_impl(CpuThreadState CpuThreadState, int Left, int Right)
		{
			long Result = (long)Left * (long)Right;
			fixed (int* Ptr = &CpuThreadState.LO)
			{
				*((long*)Ptr) = Result;
			}
		}

		unsafe static public void _multu_impl(CpuThreadState CpuThreadState, uint Left, uint Right)
		{
			ulong Result = (ulong)Left * (ulong)Right;
			fixed (int* Ptr = &CpuThreadState.LO)
			{
				*((ulong*)Ptr) = Result;
			}
		}

		public void mult() {
			MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldarg_0);
			MipsMethodEmiter.LoadGPR_Signed(RS);
			MipsMethodEmiter.LoadGPR_Signed(RT);
			MipsMethodEmiter.ILGenerator.Emit(OpCodes.Call, typeof(CpuEmiter).GetMethod("_mult_impl"));
		}
		public void multu() {
			MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldarg_0);
			MipsMethodEmiter.LoadGPR_Signed(RS);
			MipsMethodEmiter.LoadGPR_Signed(RT);
			MipsMethodEmiter.ILGenerator.Emit(OpCodes.Call, typeof(CpuEmiter).GetMethod("_multu_impl"));
		}

		/////////////////////////////////////////////////////////////////////////////////////////////////
		// Multiply ADD/SUBstract (Unsigned).
		/////////////////////////////////////////////////////////////////////////////////////////////////
		public void madd() { throw (new NotImplementedException()); }
		public void maddu() { throw(new NotImplementedException()); }
		public void msub() { throw(new NotImplementedException()); }
		public void msubu() { throw(new NotImplementedException()); }

		/////////////////////////////////////////////////////////////////////////////////////////////////
		// Move To/From HI/LO.
		/////////////////////////////////////////////////////////////////////////////////////////////////
		public void mfhi() {
			MipsMethodEmiter.SaveGPR(RD, () =>
			{
				MipsMethodEmiter.LoadHI();
			});
		}
		public void mflo() {
			MipsMethodEmiter.SaveGPR(RD, () =>
			{
				MipsMethodEmiter.LoadLO();
			});
		}
		public void mthi() {
			MipsMethodEmiter.SaveHI(() =>
			{
				MipsMethodEmiter.LoadGPR(RS);
			});
		}
		public void mtlo() {
			MipsMethodEmiter.SaveLO(() =>
			{
				MipsMethodEmiter.LoadGPR(RS);
			});
		}

		/////////////////////////////////////////////////////////////////////////////////////////////////
		// Move if Zero/Non zero.
		/////////////////////////////////////////////////////////////////////////////////////////////////
		private void _movzn(OpCode OpCode)
		{
			var SkipMoveLabel = MipsMethodEmiter.ILGenerator.DefineLabel();
			MipsMethodEmiter.LoadGPR(RT);
			MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldc_I4_0);
			MipsMethodEmiter.ILGenerator.Emit(OpCode, SkipMoveLabel);
			MipsMethodEmiter.SET_REG(RD, RS);
			MipsMethodEmiter.ILGenerator.MarkLabel(SkipMoveLabel);
		}

		public void movz() {
			_movzn(OpCodes.Bne_Un);
		}
		public void movn() {
			_movzn(OpCodes.Beq);
		}

		/////////////////////////////////////////////////////////////////////////////////////////////////
		// EXTract/INSert.
		/////////////////////////////////////////////////////////////////////////////////////////////////
		public void ext() { throw (new NotImplementedException()); }
		public void ins() { throw(new NotImplementedException()); }

		/////////////////////////////////////////////////////////////////////////////////////////////////
		// Count Leading Ones/Zeros in word.
		/////////////////////////////////////////////////////////////////////////////////////////////////
		public void clz() { throw (new NotImplementedException()); }
		public void clo() { throw(new NotImplementedException()); }

		/////////////////////////////////////////////////////////////////////////////////////////////////
		// Word Swap Bytes Within Halfwords/Words.
		/////////////////////////////////////////////////////////////////////////////////////////////////
		public void wsbh() { throw (new NotImplementedException()); }
		public void wsbw() { throw (new NotImplementedException()); }
	}
}

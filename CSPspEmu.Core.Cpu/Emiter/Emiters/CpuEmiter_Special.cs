﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using CSharpUtils.Extensions;

namespace CSPspEmu.Core.Cpu.Emiter
{
	sealed public partial class CpuEmiter
	{
		// Syscall
		public void syscall()
		{
			MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldarg_0);
			MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldc_I4, Instruction.CODE);
			MipsMethodEmiter.ILGenerator.Emit(OpCodes.Call, MipsMethodEmiter.Method_Syscall);
		}

		public void cache() { throw(new NotImplementedException()); }
		public void sync() { throw(new NotImplementedException()); }

		public void _break() {
			//throw(new NotImplementedException());
		}
		public void dbreak() { throw(new NotImplementedException()); }
		public void halt() { throw(new NotImplementedException()); }

		// (D?/Exception) RETurn
		public void dret() { throw(new NotImplementedException()); }
		public void eret() { throw(new NotImplementedException()); }

		// Move (From/To) IC
		public void mfic() { throw(new NotImplementedException()); }
		public void mtic() { throw(new NotImplementedException()); }

		// Move (From/To) DR
		public void mfdr() { throw(new NotImplementedException()); }
		public void mtdr() { throw(new NotImplementedException()); }

		public void unknown()
		{
			throw (new NotImplementedException("0x%08X : %032b at 0x%08X".Sprintf(Instruction.Value, Instruction.Value, PC)));
		}
	}
}
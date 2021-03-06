﻿using System;
using CSPspEmu.Core.Cpu;

namespace CSPspEmu.Hle
{
	public sealed class HleCallback : IDisposable
	{
		public string Name { get; private set; }
		public uint Function { get; private set; }
		public object[] Arguments { get; private set; }
		public Action ExecutedNotify;

		private HleCallback()
		{
		}

		public static HleCallback Create(string Name, uint Function, params object[] Arguments)
		{
			return new HleCallback() { Name = Name, Function = Function, Arguments = Arguments };
		}

		public void SetArgumentsToCpuThreadState(CpuThreadState CpuThreadState)
		{
			HleInterop.SetArgumentsToCpuThreadState(CpuThreadState, Function, Arguments);
		}

		public void Dispose()
		{
		}

		public override string ToString()
		{
			return String.Format("HleCallback(Name='{0}', Function=0x{1:X})", Name, Function);
		}
	}
}

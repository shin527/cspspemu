﻿using System;
using System.Collections.Generic;
using CSPspEmu.Core;
using CSPspEmu.Core.Cpu;

namespace CSPspEmu.Hle.Managers
{
	[Obsolete("Should check Interop and decide which one to use, or refactor or something.")]
	public class HleCallbackManager : PspEmulatorComponent
	{
		public HleUidPool<HleCallback> Callbacks { get; protected set; }
		private Queue<HleCallback> ScheduledCallbacks;

		[Inject]
		private CpuProcessor CpuProcessor;

		[Inject]
		private HleInterop HleInterop;

		public override void InitializeComponent()
		{
			this.Callbacks = new HleUidPool<HleCallback>();
			this.ScheduledCallbacks = new Queue<HleCallback>();
		}

		public void ScheduleCallback(HleCallback HleCallback)
		{
			Console.WriteLine("ScheduleCallback!");
			lock (this)
			{
				this.ScheduledCallbacks.Enqueue(HleCallback);
			}
		}

		public HleCallback DequeueScheduledCallback()
		{
			Console.WriteLine("DequeueScheduledCallback!");
			return ScheduledCallbacks.Dequeue();
		}

		public bool HasScheduledCallbacks
		{
			get
			{
				lock (this)
				{
					return ScheduledCallbacks.Count > 0;
				}
			}
		}

		public int ExecuteQueued(CpuThreadState CpuThreadState, bool MustReschedule)
		{
			int ExecutedCount = 0;

			//Console.WriteLine("ExecuteQueued");
			if (HasScheduledCallbacks)
			{
				//Console.WriteLine("ExecuteQueued.HasScheduledCallbacks!");
				//Console.Error.WriteLine("STARTED CALLBACKS");
				while (HasScheduledCallbacks)
				{
					var HleCallback = DequeueScheduledCallback();

					/*
					var FakeCpuThreadState = new CpuThreadState(CpuProcessor);
					FakeCpuThreadState.CopyRegistersFrom(CpuThreadState);
					HleCallback.SetArgumentsToCpuThreadState(FakeCpuThreadState);

					if (FakeCpuThreadState.PC == 0x88040E0) FakeCpuThreadState.PC = 0x880416C;
					*/
					//Console.WriteLine("ExecuteCallback: PC=0x{0:X}", FakeCpuThreadState.PC);
					//Console.WriteLine("               : A0=0x{0:X}", FakeCpuThreadState.GPR[4]);
					//Console.WriteLine("               : A1=0x{0:X}", FakeCpuThreadState.GPR[5]);
					try
					{
						HleInterop.ExecuteFunctionNow(HleCallback.Function, HleCallback.Arguments);
					}
					catch (Exception Exception)
					{
						Console.Error.WriteLine(Exception);
					}
					finally
					{
						//Console.WriteLine("               : PC=0x{0:X}", FakeCpuThreadState.PC);
						ExecutedCount++;
					}

					//Console.Error.WriteLine("  CALLBACK ENDED : " + HleCallback);
					if (MustReschedule)
					{
						//Console.Error.WriteLine("    RESCHEDULE");
						break;
					}
				}

				ExecutedCount += HleInterop.ExecuteAllQueuedFunctionsNow();
				//Console.Error.WriteLine("ENDED CALLBACKS");
			}

			return ExecutedCount;
		}
	}
}

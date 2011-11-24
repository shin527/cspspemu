﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSharpUtils;

namespace CSPspEmu.Core
{
	public class PspEmulatorContext
	{
		public PspConfig PspConfig;

		public event Action ApplicationExit;

		public PspEmulatorContext(PspConfig PspConfig)
		{
			this.PspConfig = PspConfig;
		}

		protected Dictionary<Type, object> ObjectsByType = new Dictionary<Type, object>();
		protected Dictionary<Type, Type> TypesByType = new Dictionary<Type, Type>();

		public TType GetInstance<TType>() where TType : PspEmulatorComponent
		{
			if (!ObjectsByType.ContainsKey(typeof(TType)))
			{
				Console.WriteLine("GetInstance<{0}>: Miss!", typeof(TType));
				if (TypesByType.ContainsKey(typeof(TType)))
				{
					return SetInstance<TType>(Activator.CreateInstance(TypesByType[typeof(TType)], this));
				}
				else
				{
					return SetInstance<TType>(Activator.CreateInstance(typeof(TType), this));
				}
			}

			return (TType)ObjectsByType[typeof(TType)];
		}

		public TType SetInstance<TType>(object Instance)
		{
			ConsoleUtils.SaveRestoreConsoleState(() =>
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("PspEmulatorContext.SetInstance<{0}>", typeof(TType));
				//Console.WriteLine(Environment.StackTrace);
			});
			if (ObjectsByType.ContainsKey(typeof(TType)))
			{
				throw(new InvalidOperationException());
			}
			ObjectsByType[typeof(TType)] = Instance;
			return (TType)Instance;
		}

		public void SetInstanceType<TType1, TType2>() where TType1 : PspEmulatorComponent
		{
			TypesByType[typeof(TType1)] = typeof(TType2);
		}
	}
}

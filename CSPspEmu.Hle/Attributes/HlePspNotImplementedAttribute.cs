﻿using System;

namespace CSPspEmu.Hle
{
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
	public sealed class HlePspNotImplementedAttribute : Attribute
	{
		/// <summary>
		/// 
		/// </summary>
		public bool PartialImplemented = false;

		/// <summary>
		/// 
		/// </summary>
		public bool Notice = true;
	}
}

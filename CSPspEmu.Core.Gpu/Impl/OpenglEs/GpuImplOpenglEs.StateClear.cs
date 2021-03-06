﻿using CSPspEmu.Core.Gpu.State;
using GLES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSPspEmu.Core.Gpu.Impl.OpenglEs
{
	public sealed unsafe partial class GpuImplOpenglEs
	{
		static void PrepareStateClear(GpuStateStruct* GpuState)
		{
			bool ccolorMask = false, calphaMask = false;

			//return;

			GL.glDisable(GL.GL_BLEND);
			//GL.glDisable(GLEnableCap.Lighting);
			GL.glDisable(GL.GL_TEXTURE_2D);
			//GL.glDisable(EnableCap.AlphaTest);
			GL.glDisable(GL.GL_DEPTH_TEST);
			GL.glDisable(GL.GL_STENCIL_TEST);
			//GL.glDisable(EnableCap.Fog);
			//GL.glDisable(EnableCap.ColorLogicOp);
			GL.glDisable(GL.GL_CULL_FACE);
			GL.glDepthMask(false);

			if (GpuState->ClearFlags.HasFlag(ClearBufferSet.ColorBuffer))
			{
				ccolorMask = true;
			}

			if (GlEnableDisable(GL.GL_STENCIL_TEST, GpuState->ClearFlags.HasFlag(ClearBufferSet.StencilBuffer)))
			{
				calphaMask = true;
				// Sets to 0x00 the stencil.
				// @TODO @FIXME! : Color should be extracted from the color! (as alpha component)
				GL.glStencilFunc(GL.GL_ALWAYS, 0x00, 0xFF);
				GL.glStencilOp(GL.GL_REPLACE, GL.GL_REPLACE, GL.GL_REPLACE);
				//Console.Error.WriteLine("Stencil!");
				//GL.Enable(EnableCap.DepthTest);
			}

			//int i; glGetIntegerv(GL_STENCIL_BITS, &i); writefln("GL_STENCIL_BITS: %d", i);

			if (GpuState->ClearFlags.HasFlag(ClearBufferSet.DepthBuffer))
			{
				GL.glEnable(GL.GL_DEPTH_TEST);
				GL.glDepthFunc(GL.GL_ALWAYS);
				GL.glDepthMask(true);
				GL.glDepthRangef(0f, 0f);
				//GL.DepthRange((double)-1, (double)0);

				//glDepthRange(0.0, 1.0); // Original value
			}

			GL.glColorMask(ccolorMask, ccolorMask, ccolorMask, calphaMask);

			//glClearDepth(0.0); glClear(GL_COLOR_BUFFER_BIT);

			//if (state.clearFlags & ClearBufferMask.GU_COLOR_BUFFER_BIT) glClear(GL_DEPTH_BUFFER_BIT);
			//GL.Clear(ClearBufferMask.StencilBufferBit);
		}
	}
}

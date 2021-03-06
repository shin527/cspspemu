﻿using System;
using CSPspEmu.Core;
using CSPspEmu.Core.Audio;
using CSPspEmu.Hle.Attributes;
using CSPspEmu.Hle.Managers;

namespace CSPspEmu.Hle.Modules.audio
{
	[HlePspModule(ModuleFlags = ModuleFlags.UserMode | ModuleFlags.Flags0x00010011)]
	public unsafe class sceAudio : HleModuleHost
	{
		[Inject]
		PspAudio PspAudio;

		[Inject]
		HleThreadManager ThreadManager;

		/// <summary>
		/// 
		/// </summary>
		public struct pspAudioInputParams
		{
			/// <summary>
			/// Unknown. Pass 0
			/// </summary>
			public int unknown1;

			/// <summary>
			/// Gain
			/// </summary>
			public int gain;
			
			/// <summary>
			/// Unknown. Pass 0
			/// </summary>
			public int unknown2;

			/// <summary>
			/// Unknown. Pass 0
			/// </summary>
			public int unknown3;
			
			/// <summary>
			/// Unknown. Pass 0
			/// </summary>
			public int unknown4;
			
			/// <summary>
			/// Unknown. Pass 0
			/// </summary>
			public int unknown5;
		}

		/// <summary>
		/// The minimum number of samples that can be allocated to a channel.
		/// </summary>
		public const int PSP_AUDIO_SAMPLE_MIN = 64;

		/// <summary>
		/// The maximum number of samples that can be allocated to a channel.
		/// </summary>
		public const int PSP_AUDIO_SAMPLE_MAX = 65472;

		/*
		/// <summary>
		/// Make the given sample count a multiple of 64.
		/// </summary>
		/// <param name="?"></param>
		/// <returns></returns>
		//public Type PSP_AUDIO_SAMPLE_ALIGN(Type)(Type s) { return (s + 63) & ~63; }
		*/

		protected int Output2ChannelId = -1;

		/// <summary>
		/// Reserve the audio output and set the output sample count
		/// </summary>
		/// <param name="SamplesCount">The number of samples to output in one output call (min 17, max 4111).</param>
		/// <returns>0 on success, an error if less than 0.</returns>
		[HlePspFunction(NID = 0x01562BA3, FirmwareVersion = 150)]
		[HlePspNotImplemented]
		public int sceAudioOutput2Reserve(int SamplesCount)
		{
			return Output2ChannelId = sceAudioChReserve(-1, SamplesCount, PspAudio.FormatEnum.Stereo);
			//throw(new NotImplementedException());
		}

		/// <summary>
		/// Output audio (blocking)
		/// </summary>
		/// <param name="Volume">The volume. A value between 0 and PSP_AUDIO_VOLUME_MAX.</param>
		/// <param name="Buffer">Pointer to the PCM data.</param>
		/// <returns>0 on success, an error if less than 0.</returns>
		[HlePspFunction(NID = 0x2D53F36E, FirmwareVersion = 150)]
		public int sceAudioOutput2OutputBlocking(int Volume, short* Buffer)
		{
			return sceAudioOutputBlocking(Output2ChannelId, Volume, Buffer);
		}

		/// <summary>
		/// Release the audio output
		/// </summary>
		/// <returns>0 on success, an error if less than 0.</returns>
		[HlePspFunction(NID = 0x43196845, FirmwareVersion = 150)]
		public int sceAudioOutput2Release()
		{
			return sceAudioChRelease(Output2ChannelId);
		}

		/// <summary>
		/// Get count of unplayed samples remaining
		/// </summary>
		/// <param name="ChannelId">The channel number.</param>
		/// <returns>Number of samples to be played, an error if less than 0.</returns>
		[HlePspFunction(NID = 0xB011922F, FirmwareVersion = 150)]
		public int sceAudioGetChannelRestLength(int ChannelId)
		{
#if true
			return 844;
#else
			var Channel = PspAudio.GetChannel(ChannelId);
			int RestLength = Channel.AvailableChannelsForRead;
			//Console.Error.WriteLine(RestLength);
			return RestLength;
			//return -1;
#endif
		}

		/// <summary>
		/// Output panned audio of the specified channel (blocking)
		/// </summary>
		/// <param name="ChannelId">The channel number</param>
		/// <param name="LeftVolume">The left volume</param>
		/// <param name="RightVolume">The right volume</param>
		/// <param name="Buffer">Pointer to the PCM data to output</param>
		/// <param name="Blocking"></param>
		/// <returns>
		///		Number of samples played.
		///		A negative value on error.
		/// </returns>
		private int _sceAudioOutputPannedBlocking(int ChannelId, int LeftVolume, int RightVolume, short* Buffer, bool Blocking)
		{
			//Console.WriteLine(ChannelId);
			var Channel = PspAudio.GetChannel(ChannelId);
			ThreadManager.Current.SetWaitAndPrepareWakeUp(HleThread.WaitType.Audio, "_sceAudioOutputPannedBlocking", Channel, WakeUpCallback =>
			{
				Channel.Write(Buffer, LeftVolume, RightVolume, () =>
				{
					if (Blocking) WakeUpCallback();
				});
				/*
				if (Blocking)
				{
					PspRtc.RegisterTimerInOnce(TimeSpan.FromMilliseconds(1), () =>
					{
						WakeUpCallback();
					});
				}
				*/
				if (!Blocking) WakeUpCallback();
			});
			return Channel.SampleCount;
		}

		/// <summary>
		/// Output panned audio of the specified channel (blocking)
		/// </summary>
		/// <param name="ChannelId">The channel number.</param>
		/// <param name="LeftVolume">The left volume. A value between 0 and PSP_AUDIO_VOLUME_MAX.</param>
		/// <param name="RightVolume">The right volume. A value between 0 and PSP_AUDIO_VOLUME_MAX.</param>
		/// <param name="Buffer">Pointer to the PCM data to output.</param>
		/// <returns>
		///		Number of samples played.
		///		A negative value on error.
		/// </returns>
		[HlePspFunction(NID = 0x13F592BC, FirmwareVersion = 150)]
		public int sceAudioOutputPannedBlocking(int ChannelId, int LeftVolume, int RightVolume, short* Buffer)
		{
			return _sceAudioOutputPannedBlocking(
				ChannelId: ChannelId,
				LeftVolume: LeftVolume,
				RightVolume: RightVolume,
				Buffer: Buffer,
				Blocking: true
			);
		}


		/// <summary>
		/// Output audio of the specified channel (blocking)
		/// </summary>
		/// <param name="ChannelId">The channel number.</param>
		/// <param name="Volume">The volume.</param>
		/// <param name="Buffer">Pointer to the PCM data to output.</param>
		/// <returns>
		///		Number of samples played.
		///		A negative value on error.
		///	</returns>
		[HlePspFunction(NID = 0x136CAF51, FirmwareVersion = 150)]
		public int sceAudioOutputBlocking(int ChannelId, int Volume, short* Buffer)
		{
			return _sceAudioOutputPannedBlocking(
				ChannelId: ChannelId,
				LeftVolume: Volume,
				RightVolume: Volume,
				Buffer: Buffer,
				Blocking: true
			);
		}

		/// <summary>
		/// Output panned audio of the specified channel
		/// </summary>
		/// <param name="ChannelId">The channel number.</param>
		/// <param name="LeftVolume">The left volume. A value between 0 and PSP_AUDIO_VOLUME_MAX.</param>
		/// <param name="RightVolume">The right volume. A value between 0 and PSP_AUDIO_VOLUME_MAX.</param>
		/// <param name="Buffer">Pointer to the PCM data to output.</param>
		/// <returns>0 on success, an error if less than 0.</returns>
		[HlePspFunction(NID = 0xE2D56B2D, FirmwareVersion = 150)]
		public int sceAudioOutputPanned(int ChannelId, int LeftVolume, int RightVolume, short* Buffer)
		{
			return _sceAudioOutputPannedBlocking(
				ChannelId: ChannelId,
				LeftVolume: LeftVolume,
				RightVolume: RightVolume,
				Buffer: Buffer,
				Blocking: false
			);
		}

		/// <summary>
		/// Output audio of the specified channel
		/// </summary>
		/// <param name="ChannelId">The channel number.</param>
		/// <param name="Volume">The volume. A value between 0 and PSP_AUDIO_VOLUME_MAX.</param>
		/// <param name="Buffer">Pointer to the PCM data to output.</param>
		/// <returns>0 on success, an error if less than 0.</returns>
		[HlePspFunction(NID = 0x8C1009B2, FirmwareVersion = 150)]
		public int sceAudioOutput(int ChannelId, int Volume, short* Buffer)
		{
			return _sceAudioOutputPannedBlocking(
				ChannelId: ChannelId,
				LeftVolume: Volume,
				RightVolume: Volume,
				Buffer: Buffer,
				Blocking: false
			);
		}

		/// <summary>
		/// Get count of unplayed samples remaining
		/// </summary>
		/// <param name="ChannelId">The channel number.</param>
		/// <returns>Number of samples to be played, an error if less than 0.</returns>
		[HlePspFunction(NID = 0xE9D97901, FirmwareVersion = 150)]
		public int sceAudioGetChannelRestLen(int ChannelId)
		{
			throw (new NotImplementedException());
		}

		/// <summary>
		/// Change the output sample count, after it's already been reserved
		/// </summary>
		/// <param name="ChannelId">The channel number.</param>
		/// <param name="SampleCount">The number of samples to output in one output call.</param>
		/// <returns>0 on success, an error if less than 0.</returns>
		[HlePspFunction(NID = 0xCB2E439E, FirmwareVersion = 150)]
		public int sceAudioSetChannelDataLen(int ChannelId, int SampleCount)
		{
			var Channel = PspAudio.GetChannel(ChannelId);
			Channel.SampleCount = SampleCount;
			Channel.Updated();
			return 0;
		}

		/// <summary>
		/// Change the format of a channel
		/// </summary>
		/// <param name="ChannelId">The channel number.</param>
		/// <param name="Format">One of PspAudioFormats</param>
		/// <returns>0 on success, an error if less than 0.</returns>
		[HlePspFunction(NID = 0x95FD0C2D, FirmwareVersion = 150)]
		public int sceAudioChangeChannelConfig(int ChannelId, PspAudio.FormatEnum Format)
		{
			var Channel = PspAudio.GetChannel(ChannelId);
			Channel.Format = Format;
			Channel.Updated();
			return 0;
		}

		/// <summary>
		/// Allocate and initialize a hardware output channel.
		/// </summary>
		/// <param name="ChannelId">
		///		Use a value between 0 - 7 to reserve a specific channel.
		///		Pass PSP_AUDIO_NEXT_CHANNEL to get the first available channel.
		/// </param>
		/// <param name="SampleCount">
		///		The number of samples that can be output on the channel per
		///		output call.  It must be a value between <see cref="PSP_AUDIO_SAMPLE_MIN"/>
		///		and <see cref="PSP_AUDIO_SAMPLE_MAX"/>, and it must be aligned to 64 bytes
		///		(use the ::PSP_AUDIO_SAMPLE_ALIGN macro to align it).
		/// </param>
		/// <param name="Format">The output format to use for the channel.  One of ::PspAudioFormats.</param>
		/// <returns>The channel number on success, an error code if less than 0.</returns>
		[HlePspFunction(NID = 0x5EC81C55, FirmwareVersion = 150)]
		public int sceAudioChReserve(int ChannelId, int SampleCount, PspAudio.FormatEnum Format)
		{
			try
			{
				var Channel = PspAudio.GetChannel(ChannelId, CanAlloc: true);
				Channel.SampleCount = SampleCount;
				Channel.Format = Format;
				Channel.Updated();
				return Channel.Index;
			}
			catch (Exception Exception)
			{
				Console.Error.WriteLine(Exception);
				return -1;
			}
		}

		/// <summary>
		/// Release a hardware output channel.
		/// </summary>
		/// <param name="Channel">The channel to release.</param>
		/// <returns>0 on success, an error if less than 0.</returns>
		[HlePspFunction(NID = 0x6FC46853, FirmwareVersion = 150)]
		[HlePspNotImplemented]
		public int sceAudioChRelease(int Channel)
		{
			//throw (new NotImplementedException());
			return 0;
		}

		/// <summary>
		/// Perform audio input (blocking)
		/// </summary>
		/// <param name="SampleCount">Number of samples.</param>
		/// <param name="Frequency">Either 44100, 22050 or 11025.</param>
		/// <param name="Buffer">Pointer to where the audio data will be stored.</param>
		/// <returns>0 on success, an error if less than 0.</returns>
		[HlePspFunction(NID = 0x086E5895, FirmwareVersion = 150)]
		public int sceAudioInputBlocking(int SampleCount, int Frequency, void* Buffer)
		{
			throw (new NotImplementedException());
		}

		/// <summary>
		/// Init audio input
		/// </summary>
		/// <param name="Unknown1">Unknown. Pass 0.</param>
		/// <param name="Gain">Gain</param>
		/// <param name="Unknown2">Unknown. Pass 0.</param>
		/// <returns>0 on success, an error if less than 0.</returns>
		[HlePspFunction(NID = 0x7DE61688, FirmwareVersion = 150)]
		public int sceAudioInputInit(int Unknown1, int Gain, int Unknown2)
		{
			throw (new NotImplementedException());
		}

		/// <summary>
		/// Change the volume of a channel
		/// </summary>
		/// <param name="Channel">The channel number.</param>
		/// <param name="LeftVolume">The left volume.</param>
		/// <param name="RightVolume">The right volume.</param>
		/// <returns>0 on success, an error if less than 0.</returns>
		[HlePspFunction(NID = 0xB7E1D8E7, FirmwareVersion = 150)]
		[HlePspNotImplemented()]
		public int sceAudioChangeChannelVolume(int Channel, int LeftVolume, int RightVolume)
		{
			//throw (new NotImplementedException());
			return 0;
		}

		/// <summary>
		/// Reserve the audio output
		/// </summary>
		/// <param name="SampleCount">The number of samples to output in one output call (min 17, max 4111).</param>
		/// <param name="Frequency">The frequency. One of 48000, 44100, 32000, 24000, 22050, 16000, 12000, 11050, 8000.</param>
		/// <param name="Channels">Number of channels. Pass 2 (stereo).</param>
		/// <returns>0 on success, an error if less than 0.</returns>
		[HlePspFunction(NID = 0x38553111, FirmwareVersion = 150)]
		[HlePspNotImplemented]
		public int sceAudioSRCChReserve(int SampleCount, int Frequency, int Channels)
		{
			//throw (new NotImplementedException());
			return 0;
		}

		/// <summary>
		/// Release the audio output
		/// </summary>
		/// <returns>0 on success, an error if less than 0.</returns>
		[HlePspFunction(NID = 0x5C37C0AE, FirmwareVersion = 150)]
		public int sceAudioSRCChRelease()
		{
			throw (new NotImplementedException());
		}

		/// <summary>
		/// Output audio
		/// </summary>
		/// <param name="Volume">The volume.</param>
		/// <param name="Buffer">Pointer to the PCM data to output.</param>
		/// <returns>0 on success, an error if less than 0.</returns>
		[HlePspFunction(NID = 0xE0727056, FirmwareVersion = 150)]
		public int sceAudioSRCOutputBlocking(int Volume, void* Buffer)
		{
			throw(new NotImplementedException());
		}

		/// <summary>
		/// Change the output sample count, after it's already been reserved
		/// </summary>
		/// <param name="SampleCount">The number of samples to output in one output call (min 17, max 4111).</param>
		/// <returns>0 on success, an error if less than 0.</returns>
		[HlePspFunction(NID = 0x63F2889C, FirmwareVersion = 150)]
		public int sceAudioOutput2ChangeLength(int SampleCount)
		{
			var Channel = PspAudio.GetChannel(Output2ChannelId);
			try
			{
				//return Channel.SampleCount;
				return 0;
			}
			finally
			{
				Channel.SampleCount = SampleCount;
			}
			//return 0;
		}

		/// <summary>
		/// Get count of unplayed samples remaining
		/// </summary>
		/// <returns>Number of samples to be played, an error if less than 0.</returns>
		[HlePspFunction(NID = 0x647CEF33, FirmwareVersion = 150)]
		[HlePspNotImplemented]
		public int sceAudioOutput2GetRestSample()
		{
			throw (new NotImplementedException());
		}

		/// <summary>
		/// Init audio input (with extra arguments)
		/// </summary>
		/// <param name="parameters">A pointer to a <see cref="pspAudioInputParams"/> struct.</param>
		/// <returns>0 on success, an error if less than 0.</returns>
		[HlePspFunction(NID = 0xE926D3FB, FirmwareVersion = 150)]
		[HlePspNotImplemented]
		public int sceAudioInputInitEx(pspAudioInputParams *parameters)
		{
			return 0;
		}
	}
}

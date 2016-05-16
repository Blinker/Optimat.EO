﻿using BotEngine.Common;
using BotEngine.EveOnline.Interface;
using System;
using System.Linq;
using MemoryStruct = BotEngine.EveOnline.Interface.MemoryStruct;

namespace BotEngine.EveOnline
{
	/// <summary>
	/// dispatches messages between sensor and server.
	/// </summary>
	public class SimpleSensorServerDispatcher
	{
		readonly object Lock = new object();

		public BotEngine.Interface.InterfaceAppManager SensorAppManager;

		public BotEngine.Interface.LicenseClient LicenseClient;

		BotEngine.Interface.FromProcessMeasurement<MemoryStruct.MemoryMeasurement> MeasurementMemoryReceivedLast = null;

		const int ServerExchangeTimeDistanceMin = 1000;

		Int64 ServerExchangeLastTime;

		public void Exchange(
			int? EveOnlineClientProcessId,
			string LicenseServerSessionId,
			Int64 RequestedMeasurementTime,
			Action<BotEngine.Interface.FromProcessMeasurement<MemoryStruct.MemoryMeasurement>> CallbackMeasurementMemoryNew)
		{
			BotEngine.Interface.FromProcessMeasurement<MemoryStruct.MemoryMeasurement> MeasurementMemoryNew = null;

			lock (Lock)
			{
				var Time = Bib3.Glob.StopwatchZaitMiliSictInt();

				var ServerExchangeLastAge = Time - ServerExchangeLastTime;

				if (ServerExchangeTimeDistanceMin <= ServerExchangeLastAge)
				{
					ServerExchangeLastTime = Time;

					var ToServerMessage = new BotEngine.Interface.FromClientToServerMessage()
					{
						SessionId = LicenseServerSessionId,
						Interface = SensorAppManager?.ToServer(),
						Time = Bib3.Glob.StopwatchZaitMiliSictInt(),
					};

					var FromServerMessage = LicenseClient?.ExchangePayload(ToServerMessage);

					if (null != FromServerMessage)
					{
						SensorAppManager.FromServer(FromServerMessage.Interface);
					}
				}

				var MeasurementMemoryReceivedLastTime = MeasurementMemoryReceivedLast?.EndeZait;

				var ToSensorMessage = new FromConsumerToSensorMessage()
				{
					RequestedMeasurementProcessId = EveOnlineClientProcessId,
					MeasurementMemoryRequestTime = RequestedMeasurementTime,
					MeasurementMemoryReceivedLastTime = MeasurementMemoryReceivedLastTime,
				};

				var FromSensorAppMessage = SensorAppManager?.ConsumerExchange(new BotEngine.Interface.FromConsumerToInterfaceProxyMessage()
				{
					AppSpecific = ToSensorMessage.SerializeSingleBib3RefNezDifProtobuf(),
				});

				if (null == FromSensorAppMessage)
				{
					return;
				}

				var FromSensorAppMessagePortionAppSpecific = FromSensorAppMessage.AppSpecific;

				var SensorMessageEveOnline =
					FromSensorAppMessagePortionAppSpecific.DeSerializeProtobufBib3RefNezDif()?.FirstOrDefault() as FromSensorToConsumerMessage;

				var MeasurementMemory = SensorMessageEveOnline?.MemoryMeasurement;

				if (null == MeasurementMemory)
				{
					return;
				}

				if (MeasurementMemory?.EndeZait == MeasurementMemoryReceivedLast?.EndeZait)
				{
					return;
				}

				MeasurementMemoryReceivedLast = MeasurementMemory;

				MeasurementMemoryNew = MeasurementMemory;
			}

			if (null != MeasurementMemoryNew)
			{
				CallbackMeasurementMemoryNew?.Invoke(MeasurementMemoryNew);
			}
		}
	}
}

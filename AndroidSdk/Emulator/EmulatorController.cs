
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Android.Emulation.Control;

namespace AndroidSdk;

public class EmulatorControllerGrpcClient
{
	public EmulatorControllerGrpcClient(string grpcAddress)
	{
		var channel = GrpcChannel.ForAddress(grpcAddress);
		_client = new EmulatorController.EmulatorControllerClient(channel);
	}

	protected readonly EmulatorController.EmulatorControllerClient _client;

	public async Task<bool> CheckIsBootedAsync()
	{
		var s = await _client.getStatusAsync(new Google.Protobuf.WellKnownTypes.Empty());

		return s.Booted;
	}

	public async Task<int> GetBatteryLevelAsync()
	{
		var s = await _client.getBatteryAsync(new Google.Protobuf.WellKnownTypes.Empty());

		return s.ChargeLevel;
	}
}

using System;
using System.Runtime.Serialization;

namespace AndroidSdk;

public partial class Adb
{
	/// <summary>
	/// Android Device
	/// </summary>
	[DataContract]
	public class AdbDevice(string serial, string? usb = null, string? product = null, string? model = null, string? device = null)
	{
		/// <summary>
		/// Gets or sets the serial.
		/// </summary>
		/// <value>The serial.</value>
		[DataMember(Name = "serial")]
		public string Serial { get; set; } = serial;

		[DataMember(Name = "isEmulator")]
		public bool IsEmulator
			=> Serial?.StartsWith("emulator-", StringComparison.OrdinalIgnoreCase) ?? false;

		/// <summary>
		/// Gets or sets the usb.
		/// </summary>
		/// <value>The usb.</value>
		[DataMember(Name="usb")]
		public string? Usb { get; set; } = usb;

		/// <summary>
		/// Gets or sets the product.
		/// </summary>
		/// <value>The product.</value>
		[DataMember(Name = "product")]
		public string? Product { get; set; } = product;

		/// <summary>
		/// Gets or sets the model.
		/// </summary>
		/// <value>The model.</value>
		[DataMember(Name = "model")]
		public string? Model { get; set; } = model;

		/// <summary>
		/// Gets or sets the device.
		/// </summary>
		/// <value>The device.</value>
		[DataMember(Name = "device")]
		public string? Device { get; set; } = device;
	}
}
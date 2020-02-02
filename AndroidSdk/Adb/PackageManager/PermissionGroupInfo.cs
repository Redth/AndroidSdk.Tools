using System;
using System.Collections.Generic;

namespace AndroidSdk
{
	public partial class PackageManager
	{
		/// <summary>
		/// Android Permission Group
		/// </summary>
		public class PermissionGroupInfo
		{
			/// <summary>
			/// Gets or sets the permission group name
			/// </summary>
			/// <value>The permission group name.</value>
			public string Group { get; set; }

			/// <summary>
			/// Gets or sets the name of the package.
			/// </summary>
			/// <value>The name of the package.</value>
			public string PackageName { get; set; }

			/// <summary>
			/// Gets or sets the label.
			/// </summary>
			/// <value>The label.</value>
			public string Label { get; set; }

			/// <summary>
			/// Gets or sets the description.
			/// </summary>
			/// <value>The description.</value>
			public string Description { get; set; }

			/// <summary>
			/// Gets or sets the permissions in the group
			/// </summary>
			/// <value>The permissions.</value>
			public List<PermissionInfo> Permissions { get; set; } = new List<PermissionInfo>();
		}

		/// <summary>
		/// Android Permission
		/// </summary>
		public class PermissionInfo
		{
			/// <summary>
			/// Gets or sets the permission name.
			/// </summary>
			/// <value>The permission name.</value>
			public string Permission { get; set; }

			/// <summary>
			/// Gets or sets the name of the package.
			/// </summary>
			/// <value>The name of the package.</value>
			public string PackageName { get; set; }

			/// <summary>
			/// Gets or sets the label.
			/// </summary>
			/// <value>The label.</value>
			public string Label { get; set; }

			/// <summary>
			/// Gets or sets the description.
			/// </summary>
			/// <value>The description.</value>
			public string Description { get; set; }

			/// <summary>
			/// Gets or sets the protection levels.
			/// </summary>
			/// <value>The protection levels.</value>
			public List<string> ProtectionLevels { get; set; } = new List<string>();
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;  // Windows PowerShell assembly.

namespace AndroidSdk.Dsc;


#region Get-TargetResource

[OutputType(typeof(System.Collections.Hashtable))]
[Cmdlet(VerbsCommon.Get, "TargetResource")]
public class GetTargetResource : PSCmdlet
{
	[Parameter(Mandatory = true)]
	public string Path { get; set; }

	/// <summary>
	/// Implement the logic to return the current state of the resource as a hashtable with
	/// keys being the resource properties and the values are the corresponding current
	/// value on the machine.
	/// </summary>
	protected override void ProcessRecord()
	{
		var currentResourceState = new Dictionary<string, string>();
		if (File.Exists(Path))
		{
			currentResourceState.Add("Ensure", "Present");

			// read current content
			string CurrentContent = "";
			using (var reader = new StreamReader(Path))
			{
				CurrentContent = reader.ReadToEnd();
			}
			currentResourceState.Add("Content", CurrentContent);
		}
		else
		{
			currentResourceState.Add("Ensure", "Absent");
			currentResourceState.Add("Content", "");
		}
		// write the hashtable in the PS console.
		WriteObject(currentResourceState);
	}
}

# endregion

#region Set-TargetResource
[OutputType(typeof(void))]
[Cmdlet(VerbsCommon.Set, "TargetResource")]
public class SetTargetResource : PSCmdlet
{
	[Parameter(Mandatory = true)]
	public string Path { get; set; }

	[Parameter(Mandatory = false)]

	[ValidateSet("Present", "Absent", IgnoreCase = true)]
	public string Ensure {
		get
		{
			// set the default to present.
			return (this._ensure ?? "Present") ;
		}
		set
		{
			this._ensure = value;
		}
	}

	[Parameter(Mandatory = false)]
	public string Content {
		get { return (string.IsNullOrEmpty(this._content) ? "" : this._content); }
		set { this._content = value; }
	}

	private string _ensure;
	private string _content;

	/// <summary>
	/// Implement the logic to set the state of the machine to the desired state.
	/// </summary>
	protected override void ProcessRecord()
	{
		WriteVerbose(string.Format("Running set with parameters {0}{1}{2}", Path, Ensure, Content));
		if (File.Exists(Path))
		{
			if (Ensure.Equals("absent", StringComparison.InvariantCultureIgnoreCase))
			{
				File.Delete(Path);
			}
			else
			{
				// file already exist and ensure "present" is specified. start writing the content to a file
				if (!string.IsNullOrEmpty(Content))
				{
					string existingContent = null;
					using (var reader = new StreamReader(Path))
					{
						existingContent = reader.ReadToEnd();
					}
					// check if the content of the file mathes the content passed
					if (!existingContent.Equals(Content, StringComparison.InvariantCultureIgnoreCase))
					{
						WriteVerbose("Existing content did not match with desired content updating the content of the file");
						using (var writer = new StreamWriter(Path))
						{
							writer.Write(Content);
							writer.Flush();
						}
					}
				}
			}

		}
		else
		{
			if (Ensure.Equals("present", StringComparison.InvariantCultureIgnoreCase))
			{
				// if nothing is passed for content just write "" otherwise write the content passed.
				using (var writer = new StreamWriter(Path))
				{
					WriteVerbose(string.Format("Creating a file under path {0} with content {1}", Path, Content));
					writer.Write(Content);
				}
			}

		}

		/* if you need to reboot the VM. please add the following two line of code.
		PSVariable DscMachineStatus = new PSVariable("DSCMachineStatus", 1, ScopedItemOptions.AllScope);
		this.SessionState.PSVariable.Set(DscMachineStatus);
			*/

	}

}

# endregion

#region Test-TargetResource

[Cmdlet("Test", "TargetResource")]
[OutputType(typeof(Boolean))]
public class TestTargetResource : PSCmdlet
{
	[Parameter(Mandatory = true)]
	public string Path { get; set; }

	[Parameter(Mandatory = false)]

	[ValidateSet("Present", "Absent", IgnoreCase = true)]
	public string Ensure
	{
		get
		{
			// set the default to present.
			return (this._ensure ?? "Present");
		}
		set
		{
			this._ensure = value;
		}
	}

	[Parameter(Mandatory = false)]
	public string Content
	{
		get { return (string.IsNullOrEmpty(this._content) ? "" : this._content); }
		set { this._content = value; }
	}

	private string _ensure;
	private string _content;

	/// <summary>
	/// Return a boolean value which indicates wheather the current machine is in desired state or not.
	/// </summary>
	protected override void ProcessRecord()
	{
		if (File.Exists(Path))
		{
			if( Ensure.Equals("absent", StringComparison.InvariantCultureIgnoreCase))
			{
				WriteObject(false);
			}
			else
			{
				// check if the content matches

				string existingContent = null;
				using (var stream = new StreamReader(Path))
				{
					existingContent = stream.ReadToEnd();
				}

				WriteObject(Content.Equals(existingContent, StringComparison.InvariantCultureIgnoreCase));
			}
		}
		else
		{
			WriteObject(Ensure.Equals("Absent", StringComparison.InvariantCultureIgnoreCase));
		}
	}
}

# endregion
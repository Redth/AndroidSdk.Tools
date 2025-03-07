using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace AndroidRepository.Common_2
{
	partial class RepositoryType
	{
		[XmlIgnore]
		public string Url { get; internal set; } = string.Empty;
	}
}

namespace AndroidRepository.SitesCommon_1
{
	partial class SiteList
	{
		[XmlIgnore]
		public string Url { get; internal set; } = string.Empty;
	}
}
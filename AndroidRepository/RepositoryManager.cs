using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static System.Net.WebRequestMethods;

namespace AndroidRepository;

public class RepositoryManager
{
	readonly HttpClient httpClient;
	private readonly Dictionary<Type, XmlSerializer> serializerCache = new();

	public const string GoogleAndroidRepositoryUrlBase = "https://dl.google.com/android/repository/";

	public RepositoryManager(HttpClient? httpClient = null)
	{
		if (this.httpClient is null)
		{
			this.httpClient = new HttpClient();
			this.httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml");
			this.httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
			this.httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
			this.httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Charset", "ISO-8859-1");
		}
	}

	public async Task<T> LoadUrlAsync<T>(string url)
	{
		var xml = await httpClient.GetStringAsync(url);

		return await LoadXmlAsync<T>(xml);
	}

	public async Task<T> LoadXmlAsync<T>(string xml)
	{
		// Get or create the serializer for type T
		var serializer = GetSerializer(typeof(T));

		using var reader = new StringReader(xml);

		// Deserialize synchronously since XmlSerializer doesn't support async operations
		return (T)await Task.Run(() => serializer.Deserialize(reader)!);
	}

	private XmlSerializer GetSerializer(Type type)
	{
		if (!serializerCache.TryGetValue(type, out var serializer))
		{
			serializer = new XmlSerializer(type);
			serializerCache[type] = serializer;
		}
		return serializer;
	}


	public async Task<SitesCommon_1.SiteList> GetAddonsListAsync()
	{
		var url = $"{GoogleAndroidRepositoryUrlBase}addons_list-6.xml";
		var r = await LoadUrlAsync<SitesCommon_1.SiteList>(url);
		r.Url = url;
		return r;
	}

	public async Task<Repository2_3.SdkRepository> GetSdkRepositoryAsync()
	{
		var url = $"{GoogleAndroidRepositoryUrlBase}repository2-3.xml";
		var r = await LoadUrlAsync<Repository2_3.SdkRepository>(url);
		r.Url = url;
		return r;
	}
}

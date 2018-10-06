using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace InsolvencniRejstrik
{
	public class DatasetConnector
	{
		private static string apiRoot = "https://www.hlidacstatu.cz/api/v1";

		private readonly string ApiToken;
		private readonly HttpClient HttpClient;

		public DatasetConnector(string apiToken)
		{
			ApiToken = apiToken;

			HttpClient = new HttpClient();
			HttpClient.DefaultRequestHeaders.Add("Authorization", ApiToken);
		}

		public async Task<string> AddItemToDataset<TData>(Dataset<TData> dataset, TData item) 
			where TData : IDatasetItem
		{
			var content = new StringContent(JsonConvert.SerializeObject(item));
			var response = await HttpClient.PostAsync(apiRoot + $"/DatasetItem/{dataset.DatasetId}/{item.Id}", content);
			var result = JObject.Parse(await response.Content.ReadAsStringAsync());
			if (result["error"] == null)
			{
				return result["id"].Value<string>();
			}
			else
			{
				throw new ApplicationException(result["error"]["description"].Value<string>());
			}
		}

		public async Task<bool> DatasetExists<TData>(Dataset<TData> dataset) 
			where TData : IDatasetItem
		{
			var response = await HttpClient.GetAsync(apiRoot + "/Datasets/" + dataset.DatasetId);
			var content = await response.Content.ReadAsStringAsync();

			return JContainer.Parse(content).HasValues;
		}

		public async Task DeleteDataset<TData>(Dataset<TData> dataset) 
			where TData : IDatasetItem
		{
			var deleteResponse = await HttpClient.DeleteAsync(apiRoot + "/Datasets/" + dataset.DatasetId);
			await deleteResponse.Content.ReadAsStringAsync();
			return;
		}

		public async Task<string> RegisterDataset<TData>(Dataset<TData> dataset) 
			where TData : IDatasetItem
		{
			if (await DatasetExists(dataset))
			{
				throw new ApplicationException($"Dataset {dataset.DatasetId} already exists");
			}

			var content = new StringContent(JsonConvert.SerializeObject(dataset));
			var response = await HttpClient.PostAsync(apiRoot + "/Datasets", content);
			var result = JObject.Parse(await response.Content.ReadAsStringAsync());
			if (result["error"] == null)
			{
				return result["datasetId"].Value<string>();
			}
			else
			{
				throw new ApplicationException(result["error"]["description"].ToString());
			}
		}

		public async Task<string> UpdateDataset<TData>(Dataset<TData> dataset) 
			where TData : IDatasetItem
		{
			if (!await DatasetExists(dataset))
			{
				throw new ApplicationException($"Dataset {dataset.DatasetId} not found");
			}

			var content = new StringContent(JsonConvert.SerializeObject(dataset));
			var response = await HttpClient.PutAsync(apiRoot + "/Datasets/" + dataset.DatasetId, content);
			var result = JObject.Parse(await response.Content.ReadAsStringAsync());

			if (!result["error"].HasValues)
			{
				return "Ok";
			}
			else
			{
				throw new ApplicationException(result["error"]["description"].ToString());
			}
		}
	}

	public class Template
	{
		public string Header { get; set; } = "";
		public string Body { get; set; } = "";
		public string Footer { get; set; } = "";
		public string[] Properties { get; set; } = null;
	}

	public interface IDatasetItem
	{
		string Id { get; }
	}

	public class Dataset<TData> where TData : IDatasetItem
	{
		public Dataset(string name, string datasetId, string origUrl, string description = "", string sourceCodeUrl = "", bool betaVersion = true, bool allowWriteAccess = false, string[,] orderList = null, Template searchResultTemplate = null, Template detailTemplate = null)
		{
			Name = name;
			DatasetId = datasetId;
			OrigUrl = origUrl;
			Description = description;
			SourceCodeUrl = sourceCodeUrl;
			BetaVersion = betaVersion;
			AllowWriteAccess = allowWriteAccess;
			OrderList = orderList;
			SearchResultTemplate = searchResultTemplate;
			DetailTemplate = detailTemplate;

			var jsonGen = new JSchemaGenerator
			{
				DefaultRequired = Required.Default
			};
			JsonSchema = jsonGen.Generate(typeof(TData));
		}

		public string Name { get; private set; }
		public string DatasetId { get; private set; }
		public string OrigUrl { get; private set; }
		public string Description { get; private set; }
		public string SourceCodeUrl { get; private set; }
		public JSchema JsonSchema { get; private set; }
		public bool BetaVersion { get; private set; }
		public bool AllowWriteAccess { get; private set; }
		public string[,] OrderList { get; private set; }
		public Template SearchResultTemplate { get; private set; }
		public Template DetailTemplate { get; private set; }
	}
}

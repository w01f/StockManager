using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Authenticators;
using StockManager.Infrastructure.Connectors.Common.Common;

namespace StockManager.Infrastructure.Connectors.HitBtc.Rest.Connection
{
	class ApiConnection
	{
		private const string BaseUrl = "https://api.hitbtc.com/api/2";

		public async Task<IRestResponse> DoRequest(RestRequest request, string apiKey = null, string secretKey = null)
		{
			var client = new RestClient(BaseUrl);

			if (!String.IsNullOrEmpty(apiKey) && !String.IsNullOrEmpty(secretKey))
				client.Authenticator = new HttpBasicAuthenticator(apiKey, secretKey);

			var response = await client.ExecuteTaskAsync(request);

			if (response.StatusCode == HttpStatusCode.OK)
				return response;
			if (response.ErrorException != null)
				throw new ConnectorException("Error retrieving response.  Check inner details for more info.", response.ErrorException);

			var error = response.ExtractData<ApiError>();
			throw new ConnectorException(String.Format("{0}. {1}{3}{2}", 
				error.Data.Message, 
				error.Data.Description,
				String.Join(Environment.NewLine,request.Parameters.Select(p=> $"{p.Name}: {p.Value}").ToList()),
				Environment.NewLine), response.ErrorException);
		}
	}
}

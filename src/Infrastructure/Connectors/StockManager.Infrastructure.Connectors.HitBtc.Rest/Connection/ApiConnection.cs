using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
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
			{
				request.AddParameter("nonce", GetNonce());
				request.AddParameter("apikey", apiKey);
				var sign = CalculateSignature(client.BuildUri(request).PathAndQuery, secretKey);
				request.AddHeader("X-Signature", sign);
			}

			var response = await client.ExecuteTaskAsync(request);

			if (response.StatusCode == HttpStatusCode.OK)
				return response;
			if (response.ErrorException != null)
				throw new ConnectorException("Error retrieving response.  Check inner details for more info.", response.ErrorException);

			var error = response.ExtractData<ApiError>();
			throw new ConnectorException(String.Format("{0}. {1}", error.Data.Message, error.Data.Description), response.ErrorException);
		}

		private static long GetNonce()
		{
			return DateTime.Now.Ticks * 10 / TimeSpan.TicksPerMillisecond;
		}

		private static string CalculateSignature(string text, string secretKey)
		{
			using (var hmacsha512 = new HMACSHA512(Encoding.UTF8.GetBytes(secretKey)))
			{
				hmacsha512.ComputeHash(Encoding.UTF8.GetBytes(text));
				return string.Concat(hmacsha512.Hash.Select(b => b.ToString("x2")).ToArray());
			}
		}
	}
}

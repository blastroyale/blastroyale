using System;
using System.IO;
using System.Net.Http;
using System.Text;
using Backend.Game.Services;
using FirstLight.Server.SDK.Services;
using IntegrationTests.Setups;
using Medallion.Threading;
using Medallion.Threading.FileSystem;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Tests.Stubs;

/// <summary>
/// Test application wrapper to test microservices.
/// Should contain basic testing framework needed to perform smoke & sanity checks.
/// </summary>
public class TestService<T> : WebApplicationFactory<T> where T : class
{
	public HttpClient Client;
	private StubConfiguration _cfg;

	public TestService()
	{
		_cfg = IntegrationSetup.GetIntegrationConfiguration() as StubConfiguration;
		_cfg.RemoteGameConfiguration = false;
		_cfg.AppPath = Path.GetDirectoryName(typeof(TestService<T>).Assembly.Location);
		SetupEnv();
		Client = CreateClient();
	}

	/// <summary>
	/// Performs a post request to the webservice.
	/// Returns the unpacked string contents of the response
	/// </summary>
	public string Post(string url, object param)
	{
		var payload = new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, "application/json");
		var response = Client.PostAsync(url, payload).GetAwaiter().GetResult();
		var responseString = response.Content.ReadAsStringAsync().Result;
		return responseString;
	}

	public ResponseType Post<ResponseType>(string url, object param)
	{
		return JsonConvert.DeserializeObject<ResponseType>(Post(url, param))!;
	}

	public dynamic PostGetDynamic(string url, object param)
	{
		return JsonConvert.DeserializeObject(Post(url, param))!;
	}


	/// <summary>
	/// Performs a post request to webservice.
	/// Returns the whole packed response object.
	/// </summary>
	public HttpResponseMessage PostAndGetResponse(string url, object param)
	{
		var payload = new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, "application/json");
		var response = Client.PostAsync(url, payload).GetAwaiter().GetResult();
		return response;
	}

	/// <summary>
	/// Performs a get request to the webservice.
	/// Returns an unpacked string response.
	/// </summary>
	public string Get(string url)
	{
		var response = Client.GetAsync(url).GetAwaiter().GetResult();
		var responseString = response.Content.ReadAsStringAsync().Result;
		return responseString;
	}

	private void SetupEnv()
	{
		Environment.SetEnvironmentVariable("SqlConnectionString", _cfg.DbConnectionString,
			EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("PLAYFAB_DEV_SECRET", _cfg.PlayfabSecretKey,
			EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("API_URL", "localhost",
			EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("API_KEY", "devkey",
			EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("PLAYFAB_TITLE", _cfg.PlayfabTitle, EnvironmentVariableTarget.Process);
		
		Environment.SetEnvironmentVariable("PIRATENATION_SYNC_ENABLED", "true", EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("PLAGUEDOCTOR_SYNC_ENABLED", "true", EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("GAMESGGGAMERS_SYNC_ENABLED", "true", EnvironmentVariableTarget.Process);
		Services.GetService<IPlayfabServer>().CreateServer("integration_test_user");
	}

	protected override IHost CreateHost(IHostBuilder builder)
	{
		builder.ConfigureServices(services =>
		{
			services.RemoveAll(typeof(IDistributedLockProvider));
			services.AddSingleton<IDistributedLockProvider, FileDistributedSynchronizationProvider>(_ =>
			{
				var lockFileDirectory =
					new DirectoryInfo(Environment.CurrentDirectory +
						"/Temp_Locks"); // choose where the lock files will live
				return new FileDistributedSynchronizationProvider(lockFileDirectory);
			});
			services.RemoveAll(typeof(IBaseServiceConfiguration));
			services.AddSingleton<IBaseServiceConfiguration>(p => _cfg);
		});
		return base.CreateHost(builder);
	}
}
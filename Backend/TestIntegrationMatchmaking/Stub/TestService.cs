using System;
using System.Net.Http;
using System.Text;
using ContainerApp.Cloudscript;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;


public class TestService<T> : WebApplicationFactory<T> where T : class
{
    public HttpClient Client;
	
    public TestService()
    {
        SetupEnv();
        Client = CreateClient();
	}

	public CloudscriptResponse? Post(string url, CloudscriptRequest param)
	{
		var payload = new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, "application/json");
		var response = Client.PostAsync(url, payload).GetAwaiter().GetResult();
		var responseString = response.Content.ReadAsStringAsync().Result;
		return JsonConvert.DeserializeObject<CloudscriptResponse>(responseString);
	}

    private void SetupEnv()
    {
		Environment.SetEnvironmentVariable("API_SECRET", "devkey", EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", "***REMOVED***", EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("PLAYFAB_TITLE", "***REMOVED***", EnvironmentVariableTarget.Process);

    }

	protected override IHost CreateHost(IHostBuilder builder)
    {
		return base.CreateHost(builder);
    }
}
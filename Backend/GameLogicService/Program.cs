using System.IO;
using Backend;
using GameLogicApp.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
	builder.Services.AddHttpLogging(options =>
	{
		options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
			HttpLoggingFields.RequestBody;
	});
}

if (builder.Environment.IsDevelopment())
{
	builder.Services.AddHttpLogging(options =>
	{
		options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
			HttpLoggingFields.RequestBody;
	});
}

var binPath = Path.GetDirectoryName(typeof(GameLogicWebWebService).Assembly.Location);
ServerStartup.Setup(builder.Services.AddControllers(), binPath);

var app = builder.Build();
app.UseCors(x => x
	.AllowAnyMethod()
	.AllowAnyHeader()
	.SetIsOriginAllowed(origin => true) // playfab origin is dynamic
	.AllowCredentials());


app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ApiKeyMiddleware>();
app.MapControllers();
app.Run();

public partial class Program
{
} // make it accessible in tests
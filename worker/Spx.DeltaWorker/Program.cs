using Serilog;
using Spx.DeltaWorker.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, loggerConfiguration) =>
	loggerConfiguration
		.ReadFrom.Configuration(builder.Configuration)
		.ReadFrom.Services(services)
		.Enrich.FromLogContext());

builder.Services.AddDeltaWorker(builder.Configuration);

var host = builder.Build();
host.Run();

using Serilog;
using Spx.DeltaWorker.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Host.UseSerilog((context, _, loggerConfiguration) =>
	loggerConfiguration
		.ReadFrom.Configuration(context.Configuration)
		.Enrich.FromLogContext());

builder.Services.AddDeltaWorker(builder.Configuration);

var host = builder.Build();
host.Run();

var endpointConfiguration = new EndpointConfiguration("Billing");
endpointConfiguration.UseTransport(new LearningTransport());

await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);

Console.ReadKey();

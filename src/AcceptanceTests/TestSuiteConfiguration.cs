﻿public partial class TestSuiteConfiguration
{
    public IConfigureTransportTestExecution CreateTransportConfiguration() => new ConfigureAcceptanceTestingTransportTestExecution();
}


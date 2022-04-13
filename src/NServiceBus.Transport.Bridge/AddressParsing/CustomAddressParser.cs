
using System;

class CustomAddressParser : ITransportAddressParser
{
    public CustomAddressParser(Func<string, string> parsingFunc) => this.parsingFunc = parsingFunc;

    public string ParseEndpointName(string address)
    {
        return parsingFunc(address);
    }

    readonly Func<string, string> parsingFunc;
}

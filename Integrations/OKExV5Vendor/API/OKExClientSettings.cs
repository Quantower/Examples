// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

namespace OKExV5Vendor.API;

internal class OKExClientSettings
{
    public string RestEndpoint { get; private set; }
    public string PublicWebsoketEndpoint { get; set; }
    public string PrivateWebsoketEndpoint { get; set; }

    public string PrivateBusinessWss { get; set; }

    public bool IsDemo { get; private set; }

    public OKExClientSettings(string restEndpoint, string publicWS, string privateWS, string privateBusinessWss, bool isDemo = false)
    {
        this.RestEndpoint = restEndpoint;
        this.PublicWebsoketEndpoint = publicWS;
        this.PrivateWebsoketEndpoint = privateWS;
        this.IsDemo = isDemo;
        this.PrivateBusinessWss = privateBusinessWss;
    }
}
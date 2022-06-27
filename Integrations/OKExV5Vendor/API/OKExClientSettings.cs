// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

namespace OKExV5Vendor.API
{
    class OKExClientSettings
    {
        public string RestEndpoint { get; private set; }
        public string PublicWebsoketEndpoint { get; set; }
        public string PrivateWebsoketEndpoint { get; set; }

        public bool IsDemo { get; private set; }

        public OKExClientSettings(string restEndpoint, string publicWS, string privateWS, bool isDemo = false)
        {
            this.RestEndpoint = restEndpoint;
            this.PublicWebsoketEndpoint = publicWS;
            this.PrivateWebsoketEndpoint = privateWS;
            this.IsDemo = false;
        }
    }
}

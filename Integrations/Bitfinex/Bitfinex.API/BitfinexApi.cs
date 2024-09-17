// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Bitfinex.API.Abstractions;
using Bitfinex.API.JsonConverters;
using Bitfinex.API.Misc;
using Newtonsoft.Json;
using Refit;

namespace Bitfinex.API;

public class BitfinexApi : IDisposable
{
    #region Properties

    public IBitfinexRestApiV1 RestApiV1 { get; }

    public IBitfinexPublicRestApiV2 PublicRestApiV2 { get; }

    public IBitfinexPrivateRestApiV2 PrivateRestApiV2 { get; }

    public IBitfinexPublicWebSocketApi PublicWebSocketApi { get; }

    public IBitfinexPrivateWebSocketApi PrivateWebSocketApi { get; }

    public bool IsConnected => this.PublicWebSocketApi.IsOpened && (this.PrivateWebSocketApi?.IsOpened ?? true);

    public int? UserId { get; }

    public long NonceOffset
    {
        get => this.authHelper.NonceOffset;
        set => this.authHelper.NonceOffset = value;
    }

    private readonly BitfinexAuthHelper authHelper;

    #endregion Properties

    public BitfinexApi(CancellationToken cancellation)
    {
        var settings = new RefitSettings(new NewtonsoftJsonContentSerializer(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter>
            {
                new BitfinexCandleJsonConverter(),
                new BitfinexTickerJsonConverter(),
                new BitfinexTradeJsonConverter(),
                new BitfinexDerivativeStatusJsonConverter()
            }
        }));

        this.RestApiV1 = RestService.For<IBitfinexRestApiV1>("https://api.bitfinex.com/v1", settings);
        this.PublicRestApiV2 = RestService.For<IBitfinexPublicRestApiV2>("https://api-pub.bitfinex.com/v2", settings);

        this.PublicWebSocketApi = new BitfinexPublicWebSocketApi("wss://api.bitfinex.com/ws/1");
        this.PublicWebSocketApi.Connect(cancellation);
    }

    public BitfinexApi(string apiKey, string apiSecret, CancellationToken cancellation)
        : this(cancellation)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentNullException(nameof(apiKey));

        if (string.IsNullOrEmpty(apiSecret))
            throw new ArgumentNullException(nameof(apiSecret));

        this.authHelper = new BitfinexAuthHelper();

        this.PrivateRestApiV2 = RestService.For<IBitfinexPrivateRestApiV2>(new HttpClient(new BitfinexPrivateHttpRequestHandler(apiKey, apiSecret, this.authHelper))
        {
            BaseAddress = new Uri("https://api.bitfinex.com/v2")
        }, new RefitSettings(new NewtonsoftJsonContentSerializer(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter>
            {
                new BitfinexWalletJsonConverter(),
                new BitfinexMarginInfoJsonConverter(),
                new BitfinexOrderJsonConverter(),
                new BitfinexOrderResponseJsonConverter(),
                new BitfinexPositionJsonConverter(),
                new BitfinexUserTradeJsonConverter(),
                new BitfinexUserInfoJsonConverter(),
                new BitfinexAccountSummaryJsonConverter()
            }
        })));

        this.PrivateWebSocketApi = new BitfinexPrivateWebSocketApi("wss://api.bitfinex.com/ws/2", this.authHelper);
        this.PrivateWebSocketApi.Connect(cancellation);
        this.UserId = this.PrivateWebSocketApi.Authenticate(apiKey, apiSecret, out string error, cancellation);

        if (!string.IsNullOrEmpty(error))
            throw new AuthenticationException(error);

        if (this.UserId == null)
            throw new AuthenticationException("User id is null");
    }

    public void Dispose()
    {
        this.PublicWebSocketApi.Disconnect();
        this.PrivateWebSocketApi?.Disconnect();
    }

    #region Utilities

    private class BitfinexPrivateHttpRequestHandler : HttpClientHandler
    {
        private readonly string apiKey;
        private readonly string apiSecret;
        private readonly BitfinexAuthHelper authHelper;

        public BitfinexPrivateHttpRequestHandler(string apiKey, string apiSecret, BitfinexAuthHelper authHelper)
        {
            this.apiKey = apiKey;
            this.apiSecret = apiSecret;
            this.authHelper = authHelper;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            long nonce = this.authHelper.GenerateNonce();
            string payload = await BitfinexAuthHelper.GetPayload(request, nonce);
            string signature = BitfinexAuthHelper.ComputeSignature(payload, this.apiSecret);

            request.Headers.Add("bfx-nonce", nonce.ToString());
            request.Headers.Add("bfx-apikey", this.apiKey);
            request.Headers.Add("bfx-signature", signature);

            return await base.SendAsync(request, cancellationToken);
        }
    }

    #endregion Utilities
}
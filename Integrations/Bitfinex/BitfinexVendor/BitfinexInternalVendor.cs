// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Bitfinex.API;
using Bitfinex.API.Models;
using BitfinexVendor.Misc;
using Newtonsoft.Json;
using Refit;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Integration;
using TradingPlatform.BusinessLayer.Utils;

namespace BitfinexVendor
{
    internal class BitfinexInternalVendor : Vendor
    {
        #region Properties

        private protected BitfinexApi Api { get; private set; }

        private protected BitfinexContext Context { get; }

        private readonly PingMeter pingMeter;

        private protected CancellationToken GlobalCancellation => this.globalCancellation.Token;
        private CancellationTokenSource globalCancellation;

        private Timer timer;

        private readonly DealTicketLimiter tooManyRequestsLimiter;

        #endregion Properties

        protected BitfinexInternalVendor()
        {
            this.Context = new BitfinexContext();
            this.pingMeter = new PingMeter(BitfinexVendor.VENDOR_NAME, "https://api-pub.bitfinex.com");

            this.tooManyRequestsLimiter = new DealTicketLimiter(TimeSpan.FromSeconds(15));
        }

        #region Connection

        public override ConnectionResult Connect(ConnectRequestParameters parameters)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return ConnectionResult.CreateFail(loc._("Network does not available"));

            this.Api = this.CreateApiClient(parameters);

            if (!this.Api.IsConnected)
                return ConnectionResult.CreateFail(loc._("Can't connect."));

            return ConnectionResult.CreateSuccess();
        }

        private protected virtual BitfinexApi CreateApiClient(ConnectRequestParameters parameters) => new(parameters.CancellationToken);

        public override void OnConnected(CancellationToken token)
        {
            this.globalCancellation = new CancellationTokenSource();

            this.timer = new Timer(this.TimerCallback, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

            base.OnConnected(token);
        }

        public override void Disconnect()
        {
            if (this.timer != null)
            {
                this.timer.Change(Timeout.Infinite, Timeout.Infinite);
                this.timer.Dispose();
            }

            this.globalCancellation?.Cancel();

            this.Api?.Dispose();
            this.Context?.Dispose();

            base.Disconnect();
        }

        public override PingResult Ping()
        {
            var result = new PingResult
            {
                State = PingEnum.Disconnected
            };

            if (this.Api is not { IsConnected: true })
                return result;

            try
            {
                var stopWatch = Stopwatch.StartNew();

                this.Api.PublicWebSocketApi.Ping(this.GlobalCancellation);

                stopWatch.Stop();

                result.RoundTripTime = stopWatch.Elapsed;
                result.PingTime = this.pingMeter.MeasurePing();
                result.State = result.PingTime != null ? PingEnum.Connected : PingEnum.Disconnected;
            }
            catch (Exception ex)
            {
                Core.Instance.Loggers.Log(ex);
            }

            return result;
        }

        #endregion Connection

        #region Misc

        private protected TResult HandleApiResponse<TResult>(Func<Task<TResult>> taskFunc, out string error, bool notify = false, bool retry = false)
        {
            error = default;

            try
            {
                return taskFunc.Invoke().GetResultWithoutContextCapturing();
            }
            catch (ApiException aex)
            {
                if (!string.IsNullOrEmpty(aex.Content))
                {
                    var bitfinexError = JsonConvert.DeserializeObject<BitfinexError>(aex.Content);
                    error = bitfinexError?.Text;
                }
                else
                    error = $"{aex.RequestMessage.RequestUri.LocalPath}. {aex.GetFullMessageRecursive()}";

                if ((int)aex.StatusCode == 429)
                {
                    if (notify)
                        notify = this.tooManyRequestsLimiter.AllowDealTicket();

                    if (notify && !string.IsNullOrEmpty(error))
                    {
                        this.PushMessage(DealTicketGenerator.CreateRefuseDealTicket(error));
                        notify = false;
                    }

                    if (retry && aex.Headers.TryGetValues("Retry-After", out var values))
                    {
                        string valueString = values.FirstOrDefault();
                        if (!string.IsNullOrEmpty(valueString) && int.TryParse(valueString, out int value))
                        {
                            Task.Delay(TimeSpan.FromSeconds(value), this.GlobalCancellation).Wait(this.GlobalCancellation);
                            return this.HandleApiResponse(taskFunc, out _, true, true);
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                error = ex.GetFullMessageRecursive();
            }

            if (notify && !string.IsNullOrEmpty(error))
                this.PushMessage(DealTicketGenerator.CreateRefuseDealTicket(error));

            return default;
        }

        #endregion Misc

        private void TimerCallback(object state)
        {
            try
            {
                this.OnTimerTick();
            }
            catch (Exception ex)
            {
                Core.Instance.Loggers.Log(ex);
            }
        }

        private protected virtual void OnTimerTick()
        {

        }
    }
}
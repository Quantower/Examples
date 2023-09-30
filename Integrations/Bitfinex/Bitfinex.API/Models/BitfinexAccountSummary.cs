// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

namespace Bitfinex.API.Models;

public class BitfinexAccountSummary
{
    public double MakerFee { get; internal set; }

    public double DerivativeRebate { get; internal set; }

    public double TakerFeeToCrypto { get; internal set; }

    public double TakerFeeToStable { get; internal set; }

    public double TakerFeeToFiat { get; internal set; }

    public double DerivativeTakerFee { get; internal set; }
}
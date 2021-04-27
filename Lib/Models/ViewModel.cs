using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Lib.Models
{
    public class MarkPrice
    {
        public string symbol { get; set; }
        public string markPrice { get; set; }
        public string indexPrice { get; set; }
        public string lastFundingRate { get; set; }
        public string interestRate { get; set; }
        public long nextFundingTime { get; set; }
        public long time { get; set; }
    }

    public class MiniTicket
    {
        public string p { get; set; }
        public decimal l { get; set; }
        public decimal c { get; set; }
        public Dictionary<int, decimal> R { get; set; } = new Dictionary<int, decimal>();
        public Dictionary<int, decimal> F { get; set; } = new Dictionary<int, decimal>();

        public MiniTicket() { }

        public MiniTicket(CustomBinanceTicket x)
        {
            p = x.Pair.Replace("USDT", "");
            l = x.LastPrice.ToRoundPrice();
            c = x.PriceChangePecent.ToRoundPecent();
            R = x.RSIs.ToDictionary(r => (int)r.Key, r => r.Value.Value.ToRoundPecent());
            F = x.Fibonaccis.ToDictionary(f => f.Key, f => f.Value.ToRoundPrice());
        }
    }
    public class CustomBinanceTicket
    {
        public string Pair { get; set; }
        public decimal LastPrice { get; set; }
        public decimal HighPrice { get; set; }
        public decimal LowPrice { get; set; }
        public decimal BaseVolume { get; set; }
        public decimal QuoteVolume { get; set; }
        public decimal PriceChange { get; set; }
        public decimal PriceChangePecent { get; set; }

        public ConcurrentDictionary<KlineInterval, CustomRSI> RSIs { get; set; } = new ConcurrentDictionary<KlineInterval, CustomRSI>();
        public ConcurrentDictionary<int, decimal> Fibonaccis { get; set; } = new ConcurrentDictionary<int, decimal>();

        public CustomBinanceTicket() { }

        public CustomBinanceTicket(CustomBinanceTicket x)
        {
            Pair = x.Pair;
            LastPrice = x.LastPrice;
            HighPrice = x.HighPrice;
            LowPrice = x.LowPrice;
            BaseVolume = x.BaseVolume;
            QuoteVolume = x.QuoteVolume;
            PriceChange = x.PriceChange;
            PriceChangePecent = x.PriceChangePecent;
            RSIs = x.RSIs;
        }

        public CustomBinanceTicket(IBinance24HPrice x)
        {
            Pair = x.Symbol;
            LastPrice = x.LastPrice;
            HighPrice = x.HighPrice;
            LowPrice = x.LowPrice;
            BaseVolume = x.BaseVolume;
            QuoteVolume = x.QuoteVolume;
            PriceChange = x.PriceChange;
            PriceChangePecent = x.PriceChangePercent;
        }

        public void FromBinanceTick(IBinanceTick x)
        {
            LastPrice = x.LastPrice;
            HighPrice = x.HighPrice;
            LowPrice = x.LowPrice;
            BaseVolume = x.BaseVolume;
            QuoteVolume = x.QuoteVolume;
            PriceChange = x.PriceChange;
            PriceChangePecent = x.PriceChangePercent;

            Fibonaccis[3] = CalFibonacci(x.HighPrice, x.LowPrice, (decimal)38.2);
            Fibonaccis[5] = CalFibonacci(x.HighPrice, x.LowPrice, 50);
            Fibonaccis[6] = CalFibonacci(x.HighPrice, x.LowPrice, (decimal)61.8);
        }

        public void FromBinanceMiniTick(IBinanceMiniTick x)
        {
            LastPrice = x.LastPrice;
            HighPrice = x.HighPrice;
            LowPrice = x.LowPrice;
            BaseVolume = x.BaseVolume;
            QuoteVolume = x.QuoteVolume;
            //PriceChange = x.PriceChange;
            //PriceChangePecent = x.PriceChangePercent;
        }

        public decimal CalFibonacci(decimal h, decimal l, decimal fib)
        {
            return h - ((h - l) * fib / 100);
        }
    }

    public class CustomRSI
    {
        public decimal Value { get; set; } = -1;
        public List<CustomKline> CustomKlines { get; set; } = new List<CustomKline>();
    }

    public class CustomKline
    {
        public CustomBinanceKline CustomBinanceKline { get; set; }

        public decimal Gain { get; set; }
        public decimal Loss { get; set; }
        public decimal AvgGain { get; set; }
        public decimal AvgLoss { get; set; }
        public decimal RS { get; set; }
        public decimal Value { get; set; }
    }

    public class CustomBinanceKline
    {
        public DateTime OpenTime { get; set; }
        public decimal Close { get; set; }

        public CustomBinanceKline() { }

        public CustomBinanceKline(IBinanceKline kline)
        {
            OpenTime = kline.OpenTime;
            Close = kline.Close;
        }
    }

    public class WSMessage
    {
        public WSMessage(string type, object data)
        {
            Type = type;
            Data = data;
        }

        public string Type { get; set; }
        public object Data { get; set; }

        public static string CreateMessage(WSMessage message)
        {
            return JsonConvert.SerializeObject(message);
        }
    }
}
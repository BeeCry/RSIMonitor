using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lib.Models;

namespace Lib
{
    public class Worker
    {
        static List<CustomBinanceTicket> _customTickets = new List<CustomBinanceTicket>();
        static Action<List<CustomBinanceTicket>> _updateCallback;
        static int _rsiPeriod = 14;
        static List<KlineInterval> _klineIntervals = new List<KlineInterval>();

        static Worker()
        { }

        public static void Initialize(List<CustomBinanceTicket> customTickets, Action<List<CustomBinanceTicket>> updateCallback)
        {
            _customTickets = customTickets;
            _updateCallback = updateCallback;

            _klineIntervals.Add(KlineInterval.FiveMinutes);
            _klineIntervals.Add(KlineInterval.ThirtyMinutes);
            _klineIntervals.Add(KlineInterval.OneHour);
            _klineIntervals.Add(KlineInterval.FourHour);
            _klineIntervals.Add(KlineInterval.OneDay);
        }

        public static void DoWork()
        {
            var socketClient = new BinanceSocketClient();
            Parallel.ForEach(_klineIntervals, new ParallelOptions() { MaxDegreeOfParallelism = 5 }, klineInterval =>
            {
                Parallel.ForEach(_customTickets, new ParallelOptions() { MaxDegreeOfParallelism = 10 }, item =>
                {
                    PreveDataRSI(item, klineInterval);
                });

                socketClient.FuturesUsdt.SubscribeToKlineUpdates(_customTickets.Select(x => x.Pair).ToList(), klineInterval, data =>
                {
                    var item = _customTickets.FirstOrDefault(x => x.Pair.Equals(data.Symbol));
                    if (item.RSIs[klineInterval].CustomKlines.Count == 0) return;

                    if (DateTime.Now.Minute % 5 == 0)
                    {
                        PreveDataRSI(item, klineInterval);
                        return;
                    }

                    var klineUpdate = (IBinanceKline)data.Data;
                    if (klineUpdate == null) return;

                    var rsiIndex = item.RSIs[klineInterval].CustomKlines.FindIndex(k => k.CustomBinanceKline.OpenTime == data.Data.OpenTime);
                    if (rsiIndex != -1)
                    {
                        item.RSIs[klineInterval].CustomKlines[rsiIndex].CustomBinanceKline = new CustomBinanceKline(klineUpdate);
                        CalculatorRSI(item, klineInterval);
                    }
                    else
                    {
                        item.RSIs[klineInterval].CustomKlines.Add(new CustomKline() { CustomBinanceKline = new CustomBinanceKline(klineUpdate) });
                        PreveDataRSI(item, klineInterval);
                    }
                });
            });
            
            socketClient.FuturesUsdt.SubscribeToAllSymbolTickerUpdates(data =>
            {
                var tickets = data.Where(x => _customTickets.Any(t => t.Pair.Equals(x.Symbol))).ToList();
                int ticketIndex;
                foreach (var item in tickets)
                {
                    ticketIndex = _customTickets.FindIndex(x => x.Pair.Equals(item.Symbol));
                    _customTickets[ticketIndex].FromBinanceTick(item);
                }
                _updateCallback(_customTickets.ToList());
            });
        }

        static void PreveDataRSI(CustomBinanceTicket item, KlineInterval klineInterval)
        {
            using (var client = new BinanceClient())
            {
                var callResult = client.FuturesUsdt.Market.GetKlines(item.Pair, klineInterval, limit: _rsiPeriod * 2 + 1);
                if (!callResult.Success || !callResult.Data.Any()) return;

                item.RSIs[klineInterval] = new CustomRSI()
                {
                    Value = -1,
                    CustomKlines = callResult.Data.Select(x => new CustomKline()
                    {
                        CustomBinanceKline = new CustomBinanceKline(x)
                    }).ToList()
                };
            }
            CalculatorRSI(item, klineInterval);
        }

        public static List<CustomBinanceTicket> GetAllPrices(int topN = 100)
        {
            var client = new BinanceClient();
            var result = client.FuturesUsdt.Market.Get24HPrices();
            if (!result.Success) return new List<CustomBinanceTicket>();

            return result.Data.OrderByDescending(x => x.QuoteVolume).Take(topN).Select(x => new CustomBinanceTicket(x) { }).ToList();
        }

        static void CalculatorRSI(CustomBinanceTicket ticket, KlineInterval klineInterval)
        {
            try
            {
                decimal totalGain = 0, totalLoss = 0, closeChange = 0;
                int previousPeriod = _rsiPeriod - 1;
                for (int i = 0; i < ticket.RSIs[klineInterval].CustomKlines.Count; i++)
                {
                    if (i == 0) continue;

                    closeChange = ticket.RSIs[klineInterval].CustomKlines[i].CustomBinanceKline.Close - ticket.RSIs[klineInterval].CustomKlines[i - 1].CustomBinanceKline.Close;
                    ticket.RSIs[klineInterval].CustomKlines[i].Gain = closeChange > 0 ? closeChange : 0;
                    ticket.RSIs[klineInterval].CustomKlines[i].Loss = closeChange < 0 ? Math.Abs(closeChange) : 0;

                    if (i < previousPeriod)
                    {
                        totalGain += ticket.RSIs[klineInterval].CustomKlines[i].Gain;
                        totalLoss += ticket.RSIs[klineInterval].CustomKlines[i].Loss;
                        continue;
                    }
                    else if (i == previousPeriod)
                    {
                        ticket.RSIs[klineInterval].CustomKlines[i].AvgGain = totalGain / _rsiPeriod;
                        ticket.RSIs[klineInterval].CustomKlines[i].AvgLoss = totalLoss / _rsiPeriod;
                    }
                    else
                    {
                        ticket.RSIs[klineInterval].CustomKlines[i].AvgGain = (ticket.RSIs[klineInterval].CustomKlines[i - 1].AvgGain * previousPeriod + ticket.RSIs[klineInterval].CustomKlines[i].Gain) / _rsiPeriod;
                        ticket.RSIs[klineInterval].CustomKlines[i].AvgLoss = (ticket.RSIs[klineInterval].CustomKlines[i - 1].AvgLoss * previousPeriod + ticket.RSIs[klineInterval].CustomKlines[i].Loss) / _rsiPeriod;
                    }
                    ticket.RSIs[klineInterval].CustomKlines[i].RS = ticket.RSIs[klineInterval].CustomKlines[i].AvgGain / ticket.RSIs[klineInterval].CustomKlines[i].AvgLoss;
                    ticket.RSIs[klineInterval].CustomKlines[i].Value = 100 - 100 / (1 + ticket.RSIs[klineInterval].CustomKlines[i].RS);
                }
                ticket.RSIs[klineInterval].Value = ticket.RSIs[klineInterval].CustomKlines.OrderByDescending(r => r.CustomBinanceKline.OpenTime).Take(1).FirstOrDefault().Value;
            }
            catch (Exception ex)
            {
                //Log
            }
        }
    }
}

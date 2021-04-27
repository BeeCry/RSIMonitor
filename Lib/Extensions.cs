using Newtonsoft.Json;
using System;

namespace Lib
{
    public static class Extensions
    {
        public static T Clone<T>(this T source)
        {
            var serialized = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(serialized);
        }

        public static decimal ToRoundPrice(this decimal d)
        {
            return Math.Round(d, d < 1 ? 4 : d < 50 ? 3 : d < 500 ? 2 : 0);
        }

        public static decimal ToRoundPecent(this decimal d)
        {
            return decimal.Round(d, 2, MidpointRounding.AwayFromZero);
        }
    }
}

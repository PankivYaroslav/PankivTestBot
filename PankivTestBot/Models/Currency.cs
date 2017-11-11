using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PankivTestBot.Models
{
    [Serializable]
    public class Currency
    {
        public string id { get; set; }
        public string name { get; set; }
        public string symbol { get; set; }
        public string price_usd { get; set; }
        public double amount { get; set; }
    }
}
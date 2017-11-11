using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PankivTestBot.Models
{
    public class ContextConstants
    {
        public const string Portfolio = "PortfolioList";
        public const string AddMessage = @"add \d*.*\d* \w*";
        public const string RemoveMessage = @"remove \d*.*\d* \w*";
        public const string ShowPortfolioMessage = "Show my portfolio";
        public const string NotificationMessage = @"notify when portfolio grows by \d*.*\d* percent";
    }
}
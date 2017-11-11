using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using PankivTestBot.Models;

namespace PankivTestBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        [NonSerialized]
        Timer t;
        private double portfolioValue;
        private double portfolioPercentage;
        private List<Currency> portfolioList;

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            if (!context.UserData.TryGetValue(ContextConstants.Portfolio, out portfolioList))
            {
                portfolioList = new List<Currency>();
            }

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;
            
            if (new Regex(ContextConstants.AddMessage).IsMatch(activity.Text))
            {
                await AddMessageAsync(context, activity);
            }
            else
            {
                if (new Regex(ContextConstants.RemoveMessage).IsMatch(activity.Text))
                {
                    await RemoveMessageAsync(context, activity);
                }
                else
                {
                    if (activity.Text.Equals(ContextConstants.ShowPortfolioMessage, StringComparison.InvariantCultureIgnoreCase))
                    {
                        await ShowPortfolioMessageAsync(context);
                    }
                    else
                    {
                        if (new Regex(ContextConstants.NotificationMessage).IsMatch(activity.Text))
                        {
                            await NotificationMessageAsync(context, activity);
                        }
                        else
                        {
                            await context.PostAsync("Please enter correct message");
                        }
                    }
                }
            }

            context.UserData.SetValue(ContextConstants.Portfolio, portfolioList);

            context.Wait(MessageReceivedAsync);
        }

        private async Task<List<Currency>> GetAllCurrencies()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var currencyUrl = "https://api.coinmarketcap.com/v1/ticker/?limit=0";
            var responseString = await httpClient.GetStringAsync(currencyUrl);
            List<Currency> currencies = JsonConvert.DeserializeObject<List<Currency>>(responseString);
            return currencies;
        }

        private bool FindCurrency(List<Currency> currencies, string currency, ref Currency currentCurrency)
        {
            foreach (var cur in currencies)
            {
                if (cur.name.Equals(currency, StringComparison.InvariantCultureIgnoreCase) ||
                    cur.symbol.Equals(currency, StringComparison.InvariantCultureIgnoreCase))
                {
                    currentCurrency = cur;
                    return true;
                }
            }
            return false;
        }

        private async Task AddMessageAsync(IDialogContext context, Activity activity)
        {
            string[] strings = activity.Text.Split(' ');
            double amount;
            if (Double.TryParse(strings[1], NumberStyles.Any, CultureInfo.InvariantCulture, out amount))
            {
                var currency = strings[2];
                Currency currentCurrency = null;
                var currencies = await GetAllCurrencies();
                if (currencies.Any())
                {
                    if (!FindCurrency(currencies, currency, ref currentCurrency))
                    {
                        await context.PostAsync("No such currency found");
                    }
                    else
                    {
                        bool currencyFound = false;
                        foreach (var cur in portfolioList)
                        {
                            if (cur.name.Equals(currency, StringComparison.InvariantCultureIgnoreCase) ||
                                cur.symbol.Equals(currency, StringComparison.InvariantCultureIgnoreCase))
                            {
                                cur.amount += amount;
                                currencyFound = true;
                                break;
                            }
                        }
                        if (!currencyFound)
                        {
                            currentCurrency.amount = amount;
                            portfolioList.Add(currentCurrency);
                        }
                        await context.PostAsync("Currency added to portfolio");
                    }
                }
            }
            else
            {
                await context.PostAsync("Please enter correct value");
            }
        }

        private async Task RemoveMessageAsync(IDialogContext context, Activity activity)
        {
            string[] strings = activity.Text.Split(' ');
            double amount;
            if (Double.TryParse(strings[1], NumberStyles.Any, CultureInfo.InvariantCulture, out amount))
            {
                var currency = strings[2];
                Currency currentCurrency = null;
                var currencies = await GetAllCurrencies();
                if (currencies.Any())
                {
                    if (!FindCurrency(currencies, currency, ref currentCurrency))
                    {
                        await context.PostAsync("No such currency found");
                    }
                    else
                    {
                        bool currencyFound = false;
                        foreach (var cur in portfolioList)
                        {
                            if (cur.name.Equals(currency, StringComparison.InvariantCultureIgnoreCase) ||
                                cur.symbol.Equals(currency, StringComparison.InvariantCultureIgnoreCase))
                            {
                                cur.amount -= amount;
                                currencyFound = true;
                                break;
                            }
                        }
                        if (!currencyFound)
                        {
                            currentCurrency.amount = amount;
                            portfolioList.Add(currentCurrency);
                        }
                        await context.PostAsync("Currency removed from portfolio");
                    }
                }
            }
            else
            {
                await context.PostAsync("Please enter correct value");
            }
        }

        private async Task<double> CalculatePortfolioValue()
        {
            double sum = 0;
            Currency currency = null;
            var currencies = await GetAllCurrencies();
            foreach (var cur in portfolioList)
            {
                if (FindCurrency(currencies, cur.name, ref currency))
                {
                    double price;
                    if (Double.TryParse(currency.price_usd, NumberStyles.Any, CultureInfo.InvariantCulture, out price))
                    {
                        sum += price * cur.amount;
                    }
                }
            }
            return sum;
        }

        private async Task ShowPortfolioMessageAsync(IDialogContext context)
        {
            var sum = await CalculatePortfolioValue();
            await context.PostAsync($"Your current portfolio value is {sum} $");
        }

        private async Task NotificationMessageAsync(IDialogContext context, Activity activity)
        {
            string[] strings = activity.Text.Split(' ');
            if (!Double.TryParse(strings[5], NumberStyles.Any, CultureInfo.InvariantCulture, out portfolioPercentage))
            {
                await context.PostAsync("Please enter correct value");
                return;
            }
            ConversationStarter.toId = activity.From.Id;
            ConversationStarter.toName = activity.From.Name;
            ConversationStarter.fromId = activity.Recipient.Id;
            ConversationStarter.fromName = activity.Recipient.Name;
            ConversationStarter.serviceUrl = activity.ServiceUrl;
            ConversationStarter.channelId = activity.ChannelId;
            ConversationStarter.conversationId = activity.Conversation.Id;

            portfolioValue = await CalculatePortfolioValue();
            t = new Timer(new TimerCallback(timerEvent));
            t.Change(50000, Timeout.Infinite);  
        }

        public async void timerEvent(object target)
        {
            var result = await CalculatePortfolioValue() + 1000;
            if ((result - portfolioValue) / portfolioValue * 100 >= portfolioPercentage)
            {
                t.Dispose();
                ConversationStarter.Resume(ConversationStarter.conversationId, ConversationStarter.channelId, 
                    string.Format("{0:N2}", (result - portfolioValue) / portfolioValue * 100));
                return;
            }
            t.Change(50000, Timeout.Infinite);
        }
    }
}
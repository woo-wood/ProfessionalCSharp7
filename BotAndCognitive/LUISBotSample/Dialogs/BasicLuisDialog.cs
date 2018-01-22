using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.Configuration;
using System.Threading.Tasks;

namespace LuisBotSample.Dialogs
{
    [Serializable]
    public class BasicLuisDialog : LuisDialog<object>
    {
        public BasicLuisDialog() : base(new LuisService(new LuisModelAttribute(
            ConfigurationManager.AppSettings["LuisAppId"],
            ConfigurationManager.AppSettings["LuisAPIKey"])))
        {
        }

        private IForm<RestaurantReservation> BuildRestaurantReservationForm() =>
            new FormBuilder<RestaurantReservation>()
            .Field(nameof(RestaurantReservation.Day), state => state.Day == null)
            .Field(nameof(RestaurantReservation.Time), state => state.Time == null)
            .Field(nameof(RestaurantReservation.Number), state => state.Number == null)
            .OnCompletion(OnCompleteRestaurantReservation)
            .Build();

        public async Task OnCompleteRestaurantReservation(IDialogContext context, RestaurantReservation reservation)
        {
            await context.PostAsync($"Thanks for the reservation on {reservation.Day:d} at {reservation.Time:t} for {reservation.Number} people");
        }

        public async Task ResumeAfterRestaurantReservation(IDialogContext context, IAwaitable<RestaurantReservation> result)
        {
            await context.PostAsync("See you soon!!!");
        }

        [LuisIntent("RestaurantReservation")]
        public async Task RestaurantReservationIntent(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            var message = await activity;
            await context.PostAsync($"Welcome to the reservation system! Analyzing your message '{message.Text}'...");

            var reservation = new RestaurantReservation();
           
            EntityRecommendation weekdayRecommendation;
            if (result.TryFindEntity(RestaurantReservation.Reservation_Weekday, out weekdayRecommendation))
            {
                DayOfWeek weekday;            
                if (Enum.TryParse(weekdayRecommendation.Entity, true, out weekday))
                {
                    reservation.Weekday = weekday;
                }
            }
            EntityRecommendation dayRecommendation;
            if (result.TryFindEntity(RestaurantReservation.Reservation_Day, out dayRecommendation))
            {
                DateTime day;
                if (DateTime.TryParse(dayRecommendation.Entity, out day))
                {
                    reservation.Day = day;
                }
            }
            EntityRecommendation timeRecommendation;
            if (result.TryFindEntity(RestaurantReservation.Reservation_Time, out timeRecommendation))
            {
                DateTime time;
                if (DateTime.TryParse(timeRecommendation.Entity, out time))
                {
                    reservation.Time = time;
                }
            }
            EntityRecommendation numberRedommendation;
            if (result.TryFindEntity(RestaurantReservation.Reservation_Number, out numberRedommendation))
            {
                int number;
                if (int.TryParse(numberRedommendation.Entity, out number))
                {
                    reservation.Number = number;
                }
            }

            var reservationForm = new FormDialog<RestaurantReservation>(reservation, BuildRestaurantReservationForm, FormOptions.PromptInStart, result.Entities);
            context.Call(reservationForm, ResumeAfterRestaurantReservation);
        }

        [LuisIntent("Greeting")]
        [LuisIntent("None")]
        [LuisIntent("Help")]
        [LuisIntent("Cancel")]
        public async Task SeveralIntents(IDialogContext context, LuisResult result)
        {
            await ShowLuisResult(context, result);
        }

        private async Task ShowLuisResult(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"You have reached {result.Intents[0].Intent}. You said: {result.Query}");
            context.Wait(MessageReceived);
        }
    }
}
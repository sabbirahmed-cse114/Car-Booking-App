using Microsoft.EntityFrameworkCore;
using Wafi.SampleTest.Dtos;
using Wafi.SampleTest.Entities;

namespace Wafi.SampleTest
{
    public static class BookingUtility
    {
        public static List<BookingCalendarDto> GetCalendarBookings(
        List<Booking> bookings, DateOnly? StartBookingDate = null, DateOnly? EndBookingDate = null)
        {
            var calendar = new Dictionary<DateOnly, List<Booking>>();

            foreach (var booking in bookings)
            {
                var currentDate = booking.BookingDate;
                var EndRepeatDate = booking.EndRepeatDate ?? booking.BookingDate;

                while (currentDate <= EndRepeatDate)
                {
                    bool shouldAdd = booking.RepeatOption switch
                    {
                        RepeatOption.DoesNotRepeat => currentDate == booking.BookingDate,
                        RepeatOption.Daily => true,
                        RepeatOption.Weekly => booking.DaysToRepeatOn.HasValue && BookingUtility
                        .IsDayOfWeekMatch(booking.DaysToRepeatOn.Value, currentDate.DayOfWeek), _ => false
                    };

                    if (shouldAdd)
                    {
                        if (!calendar.ContainsKey(currentDate))
                            calendar[currentDate] = new List<Booking>();

                        calendar[currentDate].Add(booking);
                    }
                    currentDate = currentDate.AddDays(1);
                }
            }

            List<BookingCalendarDto> result = new List<BookingCalendarDto>();

            foreach (var item in calendar)
            {
                if (StartBookingDate.HasValue && item.Key < StartBookingDate.Value)
                    continue;
                if (EndBookingDate.HasValue && item.Key > EndBookingDate.Value)
                    continue;
                foreach (var booking in item.Value)
                {
                    result.Add(new BookingCalendarDto
                    {
                        BookingDate = item.Key,
                        CarModel = booking.Car.Model,
                        StartTime = booking.StartTime,
                        EndTime = booking.EndTime
                    });
                }
            }
            return result.OrderBy(c => c.BookingDate).ToList();
        }

        public static bool IsTimeConflictOrNot(List<BookingCalendarDto> bookingList, DateOnly BookingDate, TimeSpan StartTime, TimeSpan EndTime, string? CarModel)
        {
            return bookingList.Any(c => c.BookingDate == BookingDate && c.CarModel == CarModel &&
                ((c.StartTime <= StartTime && c.EndTime > StartTime) ||
                (c.StartTime < EndTime && c.EndTime >= EndTime) ||
                (c.StartTime >= StartTime && c.EndTime <= EndTime)));
        }

        public static bool IsDayOfWeekMatch(DaysOfWeek repeatDays, DayOfWeek day)
        {
            return ((DaysOfWeek)(1 << (int)day) & repeatDays) != 0;
        }
    }
}

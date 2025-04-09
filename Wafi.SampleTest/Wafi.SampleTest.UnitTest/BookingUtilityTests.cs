using Autofac.Extras.Moq;
using System.Diagnostics.CodeAnalysis;
using Wafi.SampleTest.Dtos;
using Wafi.SampleTest.Entities;

namespace Wafi.SampleTest.UnitTest
{
    [ExcludeFromCodeCoverage]
    public class BookingUtilityTests
    {
        private AutoMock _moq;
        private Car car;

        [SetUp]
        public void Setup()
        {
            car = new Car
            {
                Id = Guid.NewGuid(),
                Make = "Toyota",
                Model = "Corolla"
            };
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _moq = AutoMock.GetLoose();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _moq?.Dispose();
        }

        [Test]
        public void GetCalendarBookings_ForNonRepeatBooking_ProvideNonRepeatedBookingEntry()
        {
            var bookings = new Booking
                {
                    Id = Guid.NewGuid(),
                    BookingDate = new DateOnly(2025, 2, 10),
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(11, 0, 0),
                    RepeatOption = RepeatOption.DoesNotRepeat,
                    RequestedOn = DateTime.Now,
                    Car = car
            };


            var result = BookingUtility.GetCalendarBookings(new List<Booking> { bookings });


            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(new DateOnly(2025, 2, 10), result[0].BookingDate);
        }

        [Test]
        public void GetCalendarBookings_ForDailyRepeat_ProvideDailyRepeatedBookingDateAndCountOfBookings()
        {
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                BookingDate = new DateOnly(2025, 2, 10),
                EndRepeatDate = new DateOnly(2025, 2, 12),
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                RepeatOption = RepeatOption.Daily,
                RequestedOn = DateTime.Now,
                Car = car
            };

            var bookingDates = new List<DateOnly>
            {
                new DateOnly(2025, 2, 10),
                new DateOnly(2025, 2, 11),
                new DateOnly(2025, 2, 12)
            };


            var result = BookingUtility.GetCalendarBookings(new List<Booking> { booking });           
            var resultDates = result.Select(c => c.BookingDate).ToList();


            Assert.AreEqual(3,result.Count);
            CollectionAssert.AreEquivalent(bookingDates, resultDates);
        }

        [Test]
        public void GetCalendarBookings_ForWeeklyRepeat_ProvideMatchingWeekDaysBookingListAndCountOfBookings()
        {
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                BookingDate = new DateOnly(2025, 2, 1),
                EndRepeatDate = new DateOnly(2025, 2, 21),
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                RepeatOption = RepeatOption.Weekly,
                DaysToRepeatOn = DaysOfWeek.Monday | DaysOfWeek.Wednesday,
                RequestedOn = DateTime.Now,
                Car = car
            };

            var bookingDates = new List<DateOnly>
            {
                new DateOnly(2025, 2, 3),
                new DateOnly(2025, 2, 5),
                new DateOnly(2025, 2, 10),
                new DateOnly(2025, 2, 12),
                new DateOnly(2025, 2, 17),
                new DateOnly(2025, 2, 19)
            };


            var result = BookingUtility.GetCalendarBookings(new List<Booking> { booking });       
            var resultDates = result.Select(c => c.BookingDate).ToList();


            Assert.AreEqual(6,result.Count);
            CollectionAssert.AreEquivalent(bookingDates, resultDates);
        }

        [Test]
        public void IsTimeConflictOrNot_ForTimeConflict_ReturnTrue()
        {
            var bookingList = new List<BookingCalendarDto>
            {
                new BookingCalendarDto
                {
                    BookingDate = new DateOnly(2025, 2, 20),
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(12, 0, 0),
                    CarModel = "Corolla"
                }
            };

            bool conflict = BookingUtility.IsTimeConflictOrNot(
                bookingList,
                new DateOnly(2025, 2, 20),
                new TimeSpan(11, 0, 0),
                new TimeSpan(13, 0, 0),
                "Corolla"
            );

            Assert.IsTrue(conflict);
        }

        [Test]
        public void IsTimeConflictOrNot_ForNonTimeConflict_ReturnFalse()
        {
            var bookingList = new List<BookingCalendarDto>
            {
                new BookingCalendarDto
                {
                    BookingDate = new DateOnly(2025, 2, 20),
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(12, 0, 0),
                    CarModel = "Corolla"
                }
            };

            bool conflict = BookingUtility.IsTimeConflictOrNot(
                bookingList,
                new DateOnly(2025, 2, 20),
                new TimeSpan(12, 20, 0),
                new TimeSpan(14, 0, 0),
                "Corolla"
            );

            Assert.IsFalse(conflict);
        }

        [Test]
        public void IsDayOfWeekMatch_WhenDayOfWeekMatch_ReturnTrue()
        {
            DaysOfWeek repeatDays = DaysOfWeek.Monday | DaysOfWeek.Wednesday;
            DayOfWeek day = DayOfWeek.Monday;

            bool result = BookingUtility.IsDayOfWeekMatch(repeatDays, day);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsDayOfWeekMatch_WhenDayOfWeekNotMatch_ReturnFalse()
        {
            DaysOfWeek repeatDays = DaysOfWeek.Monday | DaysOfWeek.Wednesday;
            DayOfWeek day = DayOfWeek.Friday;

            bool result = BookingUtility.IsDayOfWeekMatch(repeatDays, day);

            Assert.IsFalse(result);
        }
    }
}

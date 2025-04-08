using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wafi.SampleTest.Dtos;
using Wafi.SampleTest.Entities;

namespace Wafi.SampleTest.Controllers
{
    [Route("api/Bookings")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly WafiDbContext _context;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(WafiDbContext context, ILogger<BookingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Bookings
        [HttpGet("Booking")]
        public async Task<IEnumerable<BookingCalendarDto>> GetCalendarBookings(BookingFilterDto input)
        {
            try
            {
                var bookings = await _context.Bookings
                    .Where(c => c.CarId == input.CarId)
                    .Include(c => c.Car).ToListAsync();
                return BookingUtility.GetCalendarBookings(bookings, input.StartBookingDate, input.EndBookingDate);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Failed to GetCalendarBookings method...");
                throw new InvalidOperationException(ex.Message);
            }
        }

        // POST: api/Bookings
        [HttpPost("Booking")]
        public async Task<CreateUpdateBookingDto> PostBooking(CreateUpdateBookingDto booking)
        {
            try
            {
                var existingBookings = await _context.Bookings
                .Where(c => c.CarId == booking.CarId)
                .Include(c => c.Car)
                .ToListAsync();

                var bookingList = BookingUtility.GetCalendarBookings(existingBookings, booking.BookingDate, booking.RepeatOption != RepeatOption.DoesNotRepeat ? booking.EndRepeatDate : null);

                var currentDate = booking.BookingDate;
                while (currentDate <= (booking.EndRepeatDate ?? booking.BookingDate))
                {
                    bool shouldCheck = booking.RepeatOption switch
                    {
                        RepeatOption.DoesNotRepeat => currentDate == booking.BookingDate,
                        RepeatOption.Daily => true,
                        RepeatOption.Weekly => booking.DaysToRepeatOn.HasValue &&
                                               BookingUtility.IsDayOfWeekMatch(booking.DaysToRepeatOn.Value, currentDate.DayOfWeek),
                        _ => false
                    };

                    if (shouldCheck)
                    {
                        var CarModel = (await _context.Cars.FindAsync(booking.CarId))?.Model;

                        if (BookingUtility.IsTimeConflictOrNot(bookingList, currentDate, booking.StartTime, booking.EndTime, CarModel))
                        {
                            throw new Exception($"Booking time conflict with existing bookings on {currentDate}.");
                        }
                    }

                    currentDate = currentDate.AddDays(1);
                }

                var newBooking = new Booking
                {
                    Id = Guid.NewGuid(),
                    BookingDate = booking.BookingDate,
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    RepeatOption = booking.RepeatOption,
                    EndRepeatDate = booking.RepeatOption != RepeatOption.DoesNotRepeat ? booking.EndRepeatDate : null,
                    DaysToRepeatOn = booking.DaysToRepeatOn,
                    RequestedOn = DateTime.UtcNow,
                    CarId = booking.CarId
                };

                await _context.Bookings.AddAsync(newBooking);
                await _context.SaveChangesAsync();
                return booking;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Failed to PostBooking method...");
                throw new InvalidOperationException(ex.Message);
            }
        }

        // GET: api/SeedData
        // For test purpose
        [HttpGet("SeedData")]
        public async Task<IEnumerable<BookingCalendarDto>> GetSeedData()
        {
            try
            {
                var cars = await _context.Cars.ToListAsync();

                if (!cars.Any())
                {
                    cars = GetCars().ToList();
                    await _context.Cars.AddRangeAsync(cars);
                    await _context.SaveChangesAsync();
                }

                var bookings = await _context.Bookings.ToListAsync();

                if (!bookings.Any())
                {
                    bookings = GetBookings().ToList();

                    await _context.Bookings.AddRangeAsync(bookings);
                    await _context.SaveChangesAsync();
                }
                return BookingUtility.GetCalendarBookings(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Failed to GetSeedData method...");
                throw new InvalidOperationException(ex.Message);
            }
        }


        #region Sample Data
        private IList<Car> GetCars()
        {
            var cars = new List<Car>
            {
                new Car { Id = Guid.NewGuid(), Make = "Toyota", Model = "Corolla" },
                new Car { Id = Guid.NewGuid(), Make = "Honda", Model = "Civic" },
                new Car { Id = Guid.NewGuid(), Make = "Ford", Model = "Focus" }
            };

            return cars;
        }

        private IList<Booking> GetBookings()
        {
            var cars = GetCars();

            var bookings = new List<Booking>
            {
                new Booking { Id = Guid.NewGuid(), BookingDate = new DateOnly(2025, 2, 5), StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(12, 0, 0), RepeatOption = RepeatOption.DoesNotRepeat, RequestedOn = DateTime.Now, CarId = cars[0].Id, Car = cars[0] },
                new Booking { Id = Guid.NewGuid(), BookingDate = new DateOnly(2025, 2, 10), StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(16, 0, 0), RepeatOption = RepeatOption.Daily, EndRepeatDate = new DateOnly(2025, 2, 20), RequestedOn = DateTime.Now, CarId = cars[1].Id, Car = cars[1] },
                new Booking { Id = Guid.NewGuid(), BookingDate = new DateOnly(2025, 2, 15), StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), RepeatOption = RepeatOption.Weekly, EndRepeatDate = new DateOnly(2025, 3, 31), RequestedOn = DateTime.Now, DaysToRepeatOn = DaysOfWeek.Monday, CarId = cars[2].Id,  Car = cars[2] },
                new Booking { Id = Guid.NewGuid(), BookingDate = new DateOnly(2025, 3, 1), StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(13, 0, 0), RepeatOption = RepeatOption.DoesNotRepeat, RequestedOn = DateTime.Now, CarId = cars[0].Id, Car = cars[0] },
                new Booking { Id = Guid.NewGuid(), BookingDate = new DateOnly(2025, 3, 7), StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(10, 0, 0), RepeatOption = RepeatOption.Weekly, EndRepeatDate = new DateOnly(2025, 3, 28), RequestedOn = DateTime.Now, DaysToRepeatOn = DaysOfWeek.Friday, CarId = cars[1].Id, Car = cars[1] },
                new Booking { Id = Guid.NewGuid(), BookingDate = new DateOnly(2025, 3, 15), StartTime = new TimeSpan(15, 0, 0), EndTime = new TimeSpan(17, 0, 0), RepeatOption = RepeatOption.Daily, EndRepeatDate = new DateOnly(2025, 3, 20), RequestedOn = DateTime.Now, CarId = cars[2].Id,  Car = cars[2] }
            };

            return bookings;
        }
        #endregion
    }
}

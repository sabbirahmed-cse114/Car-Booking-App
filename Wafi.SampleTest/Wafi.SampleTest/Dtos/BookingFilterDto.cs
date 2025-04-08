using System.ComponentModel.DataAnnotations;

namespace Wafi.SampleTest.Dtos
{
    public class BookingFilterDto
    {
        [Required(ErrorMessage = "Car Id is required.")]
        public Guid CarId { get; set; }
        [Required(ErrorMessage = "Start booking date is required.")]
        public DateOnly StartBookingDate { get; set; }
        [Required(ErrorMessage = "End booking date is required.")]
        public DateOnly EndBookingDate { get; set; }
    }
}

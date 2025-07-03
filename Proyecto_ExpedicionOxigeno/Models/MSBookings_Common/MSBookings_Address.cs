using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_ExpedicionOxigeno.Models
{
    public class MSBookings_Address
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Location")]
        public int? LocationId { get; set; }

        [Column("street")]
        public string Street { get; set; }

        [Column("city")]
        public string City { get; set; }

        [Column("state")]
        public string State { get; set; }

        [Column("countryOrRegion")]
        public string CountryOrRegion { get; set; }

        [Column("postalCode")]
        public string PostalCode { get; set; }

        public virtual MSBookings_Location Location { get; set; }
    }
}
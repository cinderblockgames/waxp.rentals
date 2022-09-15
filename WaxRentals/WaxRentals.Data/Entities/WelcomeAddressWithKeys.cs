using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace WaxRentals.Data.Entities
{
	[Table("AddressWithKeys", Schema = "welcome")]
    public class WelcomeAddressWithKeys
    {

		[Column("AddressWithKeysId"), Key]
		public int AddressId { get; set; }

		[Column("Address"), DatabaseGenerated(DatabaseGeneratedOption.Computed)]
		public string BananoAddress { get; set; }

	}
}

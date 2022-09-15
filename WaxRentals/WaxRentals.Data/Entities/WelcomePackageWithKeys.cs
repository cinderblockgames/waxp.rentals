using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace WaxRentals.Data.Entities
{
	[Table("PackagesWithKeys", Schema = "welcome")]
    public class WelcomePackageWithKeys
    {

		[Key]
		public int PackageId { get; set; }

		[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
		public DateTime Inserted { get; set; }

		public string WaxAccount { get; set; }
		public string OwnerPublicKey { get; set; }
        public string ActivePublicKey { get; set; }
        public int Ram { get; set; }

		public decimal Banano { get; set; }
		public string SweepBananoTransaction { get; set; }
		public DateTime? Paid { get; set; }

		public string FundTransaction { get; set; }
		public string NftTransaction { get; set; }

		public int? RentalId { get; set; }
		public virtual Rental Rental { get; set; }

		[NotMapped]
		public Status Status
		{
			get { return (Status)StatusId; }
			set { StatusId = (int)value; }
		}
		public int StatusId { get; set; }

		[Column("Address"), DatabaseGenerated(DatabaseGeneratedOption.Computed)]
		public string BananoAddress { get; set; }

	}
}

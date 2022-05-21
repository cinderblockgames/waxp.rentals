﻿using System.Linq;
using IronBarCode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WaxRentals.Banano.Transact;
using WaxRentals.Data.Manager;
using static WaxRentalsWeb.Config.Constants;

namespace WaxRentalsWeb.Pages
{
    public class BananoModel : PageModel
    {

        private readonly IDataFactory _data;
        private readonly IBananoAccountFactory _banano;

        public BananoModel(IDataFactory data, IBananoAccountFactory banano)
        {
            _data = data;
            _banano = banano;
        }

        public IActionResult OnGet(string address)
        {
            if (!string.IsNullOrWhiteSpace(address))
            {
                var rental = _data.Explore.GetRentalsByBananoAddresses(new string[] { address }).SingleOrDefault();
                if (rental != null)
                {
                    var account = _banano.BuildAccount((uint)rental.RentalId);
                    var link = account.BuildLink(rental.Banano);
                    var qr = QRCodeWriter.CreateQrCodeWithLogo(link, Images.Logo, Images.Size);
                    return File(qr.ToPngBinaryData(), "image/png");
                }
            }
            return null;
        }

    }
}

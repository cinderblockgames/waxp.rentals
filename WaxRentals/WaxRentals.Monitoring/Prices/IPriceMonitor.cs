﻿using System;

namespace WaxRentals.Monitoring.Prices
{
    public interface IPriceMonitor
    {

        event EventHandler Updated;
        void Initialize();

        decimal Banano { get; }
        decimal Wax { get; }

    }
}

﻿using System;

namespace MangoPay.SDK.Entities.GET
{
    /// <summary>Class represents GB type of bank account.</summary>
	public class BankAccountGbObsoleteDTO : BankAccountObsoleteDTO
    {
        /// <summary>Account number.</summary>
        public String AccountNumber { get; set; }

        /// <summary>Sort code.</summary>
        public String SortCode { get; set; }
    }
}

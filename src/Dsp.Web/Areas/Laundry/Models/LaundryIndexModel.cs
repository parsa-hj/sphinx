﻿namespace Dsp.Web.Areas.Laundry.Models
{
    using System.Collections.Generic;
    using System.Web.Mvc;

    public class LaundryIndexModel
    {
        public LaundrySchedule Schedule { get; set; }
        public IEnumerable<SelectListItem> SemesterList { get; set; }
    }
}
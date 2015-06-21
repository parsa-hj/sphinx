﻿namespace Dsp.Areas.Dsp.Models
{
    using Entities;
    using System.Collections.Generic;
    using System.Web.Mvc;

    public class SoberManagerModel
    {
        public IEnumerable<SoberSignup> Signups { get; set; }
        public SoberSignup NewSignup { get; set; }
        public MultiAddSoberSignupModel MultiAddModel { get; set; }
        public SelectList SignupTypes { get; set; }
    }
}
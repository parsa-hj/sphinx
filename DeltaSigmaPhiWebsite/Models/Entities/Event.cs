namespace DeltaSigmaPhiWebsite.Models.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public partial class Event
    {
        public Event()
        {
            ServiceHours = new HashSet<ServiceHour>();
        }

        public int EventId { get; set; }

        public DateTime DateTimeOccurred { get; set; }

        [Required]
        [StringLength(50)]
        public string EventName { get; set; }

        public double DurationHours { get; set; }

        public virtual ICollection<ServiceHour> ServiceHours { get; set; }
    }
}

﻿namespace DeltaSigmaPhiWebsite.Areas.Edu.Models
{
    using Entities;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Web.Mvc;

    public class ClassScheduleModel
    {
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Member")]
        public int SelectedMember { get; set; }
        public IEnumerable<SelectListItem> Members { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Semester")]
        public int SelectedSemester { get; set; }
        public IEnumerable<SelectListItem> Semesters { get; set; }

        public IEnumerable<Class> AllClasses { get; set; }
        public List<ClassTaken> ClassesTaken { get; set; }
    }
}
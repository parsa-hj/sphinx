﻿namespace Dsp.WebCore.Areas.House.Models;

using System.Collections.Generic;
using Dsp.Data.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

public class RoomIndexModel
{
    public IEnumerable<Room> Rooms { get; set; }
    public IEnumerable<Member> Members { get; set; }
    public Semester Semester { get; set; }
    public int sid { get; set; }
    public IEnumerable<SelectListItem> SemesterList { get; set; }
}
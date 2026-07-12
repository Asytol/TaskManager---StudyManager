using Godot;
using System;

[GlobalClass]
public partial class Date : Resource
{
	[Export]public int Year;
	[Export]public int Month;
	[Export]public int Day;

	public Date() : this(0,0,0) {}

	public Date(int year, int month, int day)
	{
		this.Year = year;
		this.Month = month;
		this.Day = day;	
	}
	public static bool CheckIfPassed(Date TaskDate)
	{
		DateTime CurrentDate = DateTime.Today;
		
		if (TaskDate.Year < CurrentDate.Year)
		{
			return true;
		}
		if (TaskDate.Year > CurrentDate.Year)
		{
			return false;
		}

		if (TaskDate.Month < CurrentDate.Month)
		{
			return true;
		}
		if (TaskDate.Month > CurrentDate.Month)
		{
			return false;
		}

		if (TaskDate.Day <= CurrentDate.Day+10)
		{
			return true;
		}

		return false;	
	}
}
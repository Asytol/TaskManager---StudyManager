using Godot;
using System;
using System.Text.RegularExpressions;


[GlobalClass]
public partial class TaskContainerSaveCs : Resource
{
	[Export] public string Name;
	[Export] public string Description;
	[Export] public Date CreatedOn;
	[Export] public Date DeadLine;
	[Export] public int TimesRepeated = 0;
	[Export] public Godot.Collections.Array<string> Links = [];
	public bool Cleared;

	[Export] public int IndexNumber;

	public TaskContainerSaveCs() : this(null,null,new Date(0,0,0),null,0) {}

	public TaskContainerSaveCs(string Name,string Description,Date CreatedDate,Godot.Collections.Array<string> links,int IndexNumber)
	{
		this.Name = Name;
		this.Description = Description;

		this.CreatedOn = CreatedDate;
		this.DeadLine = ExpandDate(CreatedDate,1);
		this.Links = links;

		this.IndexNumber = IndexNumber;
	}

	
	public void Completed()
	{
		GD.Print("Completed a task");
		TimesRepeated += 1;
		switch (TimesRepeated)
		{
			case 1:
				DeadLine = ExpandDate(DeadLine,3);
				break;
			case 2:
				DeadLine = ExpandDate(DeadLine,7);
				break;
			case 3:
				DeadLine = ExpandDate(DeadLine,21);
				break;
			case 4:
				DeadLine = ExpandDate(DeadLine,40);
				break;
			case 5:
				DeadLine = ExpandDate(DeadLine,60);
				break;
			case 6:
				this.Cleared = true;
				//Delete task or maybe put it in a permanently completed tab
				break;
		}
		
		SaveGame save = LogicHandler.GetSaveGame();
		save.WriteSaveGame();

		LogicHandler Handler = LogicHandler.GetLogicHandler();
		if (Handler != null)
		{
			Handler.InitializeTasks(save);
		}
	}

	public static Date ExpandDate(Date DeadLine, int DaysMore){
		Date result = new Date { Year = DeadLine.Year, Month = DeadLine.Month, Day = DeadLine.Day };
    	result.Day += DaysMore;

		int DaysInMonth = GetDaysInMonth(result.Year, result.Month);
		result.Day += DaysMore;

		

		while (true)
		{
			if (result.Day <= DaysInMonth)
			{
				return result;
			}

			result.Day -= DaysInMonth;

			result.Month += 1;

			if (result.Month > 12)
			{
				result.Month = 0;
				result.Year += 1;
			}

			DaysInMonth = GetDaysInMonth(result.Year, result.Month);
		}
	}

	public static int GetDaysInMonth(int Year,int Month)
	{
		if (Month == 2)
		{
			if (Year % 4 == 0 && (Year % 100 != 0 || Year % 400 == 0))
			{
				return 29;
			}
			return 28;
		}

		if (Month % 2 == 0)
		{
			return 30;
		}
		return 31;
	}
}

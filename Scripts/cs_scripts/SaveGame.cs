using Godot;
using System;
using System.Linq;

[GlobalClass]
public partial class SaveGame : Resource
{
	const string SaveGamePath = "user://SaveGame.tres";

	[Export] public Godot.Collections.Array<TaskContainerSaveCs> TaskContainers = [];
	// Called when the node enters the scene tree for the first time.

	public SaveGame() : this(0) {}

	public SaveGame(int o)
	{
		
	}

	public void WriteSaveGame()
	{
		ResourceSaver.Save(this,SaveGamePath);
	}
	public static bool SaveExists()
	{
		if (ResourceLoader.Exists(SaveGamePath))
		{
			GD.Print("Exists");
		}
		return ResourceLoader.Exists(SaveGamePath);
	}
	public static SaveGame LoadSaveGame()
	{
		return ResourceLoader.Load(SaveGamePath,"",ResourceLoader.CacheMode.Ignore) as SaveGame;
	}

	public static void DeleteSave()
	{
		ResourceSaver.Save(new SaveGame(),SaveGamePath);
	}

	public void AddTaskContainer(string Name,string Description,Date CreatedDate)
	{
		TaskContainerSaveCs container = new TaskContainerSaveCs(Name,Description,CreatedDate,TaskContainers.Count);
		TaskContainers.Add(container);
		WriteSaveGame();
	}
}

using Godot;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

public partial class LogicHandler : Node
{
	public static LogicHandler LogicInstance;
	public static SaveGame SaveGame;

	// Called when the node enters the scene tree for the first time.
	[Export] public PackedScene TaskScene;
	[Export] public VBoxContainer TodoContainer;
	[Export] public VBoxContainer UpcomingContainer;

	//  ______________________________________________________
	//PostitNote and creating Tasks __--__
	[Export] private AnimationPlayer AnimPlayer;
	private bool AlreadyAnimating;
	[Export] private TextureRect PostItNote;
	[Export] private Control PostItHitbox;

	// TaskCreationPapper
	[Export] private TextureRect TaskCreationPapper;
	private bool ExtraPapperUsed = false;
	private string TempName;
	private string TempDescription;
	private LineEdit NewTaskNameEditor;
	private TextEdit NewDescriptorEditor;
	

	private bool HoveringOnPostit = false;
	private bool PostItOut = false;


	
	public override void _Ready()
	{
		LoadOrInitializeSave();
		//SaveGame.WriteSaveGame();
		InitializeTasks(SaveGame);
		

		//Postit note hitboxes
		// --__--
		PostItHitbox.MouseEntered += RevealPostIt;
		PostItHitbox.GetChild<Control>(0).MouseExited += ConsealPostIt;

		//PostitButton
		Button PostItButton = PostItNote.GetNode<Button>("%Button");
		PostItButton.MouseEntered += HOP; 
		PostItButton.MouseExited += NHOP;
		PostItButton.ButtonUp += RevealTaskCreationPapper;

		PostItHitbox.MouseFilter = Control.MouseFilterEnum.Stop;
		PostItHitbox.GetChild<Control>(0).MouseFilter = Control.MouseFilterEnum.Ignore;

		AnimPlayer.AnimationStarted += ANST; AnimPlayer.AnimationFinished += ANE;

		NewTaskNameEditor = TaskCreationPapper.GetNode<LineEdit>("%NameEditor");
		TaskCreationPapper.GetNode<LineEdit>("%NameEditor").TextChanged += OnNameEdited;
		NewDescriptorEditor = TaskCreationPapper.GetNode<TextEdit>("%DescriptorEditor");
		TaskCreationPapper.GetNode<TextEdit>("%DescriptorEditor").TextChanged += OnDescriptionEdited;
		TaskCreationPapper.GetNode<Button>("%Submit").ButtonUp += SubmitTask;
	
		TextureButton UpcomingBookmark = GetNode<TextureButton>("%UpcomingBookmark");
		UpcomingBookmark.MouseEntered += () => ScheduleAnimation("BookMarkExtend");
		UpcomingBookmark.MouseExited += () => ScheduleAnimation("BookMarkExtend",true);
		UpcomingBookmark.ButtonUp += () => SwitchPapper(2);
		
		NewTaskNameEditor.TextChanged += CheckLineEditOverflow;

		TaskCreationPapper.GetNode<AnimationPlayer>("%AnimationPlayer");

		LogicInstance = this;

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	public void LoadOrInitializeSave()
	{
		if (SaveGame.SaveExists())
		{
			SaveGame = SaveGame.LoadSaveGame();
		}
		else
		{
			SaveGame = new SaveGame();
			SaveGame.WriteSaveGame();
		}
	}
	public static SaveGame GetSaveGame()
	{
		return SaveGame;
	}
	public static LogicHandler GetLogicHandler()
	{
		if (LogicInstance != null)
		{
			return LogicInstance;
		}
		return null;
	}

	public void InitializeTasks(SaveGame SaveFile)
	{
		//Clear TaskList
		foreach (Node child in TodoContainer.GetChildren())
		{
			child.QueueFree();	
		}

		foreach (Node child in UpcomingContainer.GetChildren())
		{
			child.QueueFree();	
		}


		//Initialize new tasks
		for (int i = 0; i < SaveFile.TaskContainers.Count; i++)
		{
			TaskContainerSaveCs TaskContainer = SaveFile.TaskContainers[i]; 
			
			if (TaskContainer.Cleared == true)
			{
				return;				
			}
			Node Instance = TaskScene.Instantiate();

			//All GameObjects
			Label TaskName = Instance.GetNode<Label>("%TaskName");
			Label DeadlineLabel = Instance.GetNode<Label>("%DeadlineLabel");
			Label RepeatNum = Instance.GetNode<Label>("%RepeatNum");
			TextureButton Options = Instance.GetNode<TextureButton>("%Options");
			TextureButton CheckMark = Instance.GetNode<TextureButton>("%CheckMark");

			TaskName.Text = TaskContainer.Name;
			RepeatNum.Text = TaskContainer.TimesRepeated.ToString();
			// __--__                   __--__
			if (TaskContainer.DeadLine != null)
			{
				Date Deadline = TaskContainer.DeadLine;
				DeadlineLabel.Text = "Deadline: " + $"{Deadline.Year}/{Deadline.Month}/{Deadline.Day}";	
			}
			//Checks if deadline is before currentdate
			if (Date.CheckIfPassed(TaskContainer.DeadLine) == true)
			{	
				//Options.ButtonUp += Function;
				CheckMark.ButtonUp += TaskContainer.Completed;
				TodoContainer.AddChild(Instance);	
			}
			else
			{
				CheckMark.Visible = false;
				Options.Visible = false;
				UpcomingContainer.AddChild(Instance);
			}
		}	
	}

	public async void RevealPostIt()
	{
		if (PostItOut == false)
		{
			await ScheduleAnimation("PostItSliding",false);
			PostItHitbox.MouseFilter = Control.MouseFilterEnum.Ignore;
			
			await ToSignal(AnimPlayer,"animation_finished");
			PostItHitbox.GetChild<Control>(0).MouseFilter = Control.MouseFilterEnum.Stop;
			PostItOut = true;	
		}
	}
	public async void ConsealPostIt()
	{
		await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
		if (AlreadyAnimating){await ToSignal(AnimPlayer,"animation_finished");}
		await ConsealPostItFollowUp();
	}
	public async Task ConsealPostItFollowUp()
	{
		if (PostItOut == true && HoveringOnPostit == false)
		{
			PostItHitbox.MouseFilter = Control.MouseFilterEnum.Stop;
			PostItHitbox.GetChild<Control>(0).MouseFilter = Control.MouseFilterEnum.Ignore;
			await ScheduleAnimation("PostItSliding",true);
			PostItOut = false;	
		}
	}


	public async void RevealTaskCreationPapper()
	{
		await ScheduleAnimation("PapperSliding",false);
	}

	public int CurrentPapperNum = 1;
	public async void SwitchPapper(int PapperNum)
	{
		string Animation = "SwitchPapper";
		if (CurrentPapperNum < PapperNum)
		{
			switch (PapperNum)
			{
				case 3:
					await ScheduleAnimation(Animation + "1");
					await ScheduleAnimation(Animation + "2");
					CurrentPapperNum = 3;
					break;
				case 2:
					await ScheduleAnimation(Animation + "1");
					CurrentPapperNum = 2;
					break;
			}
			return;
		}
		if (CurrentPapperNum > PapperNum)
		{
			switch (PapperNum)
			{
				case 2:
					await ScheduleAnimation(Animation + "2",true);
					CurrentPapperNum = 2;
					break;
				case 1:
					await ScheduleAnimation(Animation + "2",true);
					await ScheduleAnimation(Animation + "1",true);
					CurrentPapperNum = 2;
					break;
			}
			return;
		}
		
	}
	public void OnNameEdited(string Name)
	{
		TempName = Name;
	}
	public void OnDescriptionEdited()
	{
		TempDescription = TaskCreationPapper.GetNode<TextEdit>("%DescriptorEditor").Text;;
	}

	public void SubmitTask()
	{
		DateTime date = DateTime.Today;
		Date TempDate = new Date(date.Year,date.Month,date.Day);

		SaveGame.AddTaskContainer(TempName,TempDescription,TempDate);
		SaveGame.WriteSaveGame();

		InitializeTasks(SaveGame);
		AnimPlayer.PlayBackwards("PapperSliding2");

		TempName = "";
		TempDescription = "";
	}

	public async void CheckLineEditOverflow(string text)
	{
		if (text.Length >= 13)
		{
			if (ExtraPapperUsed == false)
			{
				await ScheduleAnimation("TapeOnPapper",false);
			 	ExtraPapperUsed = true;	
			}
			return;
		}
		if (ExtraPapperUsed == true)
		{
			TaskCreationPapper.GetNode<AnimationPlayer>("%AnimationPlayer").PlayBackwards("TapeOnPapper");
			ExtraPapperUsed = false;
		}
	}

	public async Task ScheduleAnimation(string Name, bool PlayBackwards = false)
	{
		if (AlreadyAnimating){await ToSignal(AnimPlayer,"animation_finished");}
		

		if (PlayBackwards == true)
		{
			AnimPlayer.PlayBackwards(Name);
			return;
		}
		AnimPlayer.Play(Name);
	}

	public void HOP(){ HoveringOnPostit = true;}
	public void NHOP(){ HoveringOnPostit = false;}
	public void ANST(StringName name){ AlreadyAnimating = true;}
	public void ANE(StringName name){ AlreadyAnimating = false;}


}

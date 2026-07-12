using Godot;
using System;
using System.Threading.Tasks;

public partial class LogicHandler : Node
{
	public static LogicHandler LogicInstance;
	public static SaveGame SaveGame;

	// Called when the node enters the scene tree for the first time.
	[Export] public PackedScene TaskScene;
	[Export] public VBoxContainer TodoContainer;

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
		InitializeTasks(TodoContainer,SaveGame);
		

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

	public void InitializeTasks(VBoxContainer Container,SaveGame SaveFile)
	{
		//Clear TaskList
		foreach (Node child in Container.GetChildren())
		{
			child.QueueFree();	
		}


		//Initialize new tasks
		for (int i = 0; i < SaveFile.TaskContainers.Count; i++)
		{
			TaskContainerSaveCs TaskContainer = SaveFile.TaskContainers[i]; 
			
			//Checks if deadline is before currentdate
			if (Date.CheckIfPassed(TaskContainer.DeadLine) == true)
			{
				Node Instance = TaskScene.Instantiate();

				//All GameObjects
				Label TaskName = Instance.GetNode<Label>("%TaskName");
				Label DeadlineLabel = Instance.GetNode<Label>("%DeadlineLabel");
				Label RepeatNum = Instance.GetNode<Label>("%RepeatNum");
				TextureButton Options = Instance.GetNode<TextureButton>("%Options");
				TextureButton CheckMark = Instance.GetNode<TextureButton>("%CheckMark");
				// __--__                   __--__

				TaskName.Text = TaskContainer.Name;
				RepeatNum.Text = TaskContainer.TimesRepeated.ToString();
				//Options.ButtonUp += Function;
				CheckMark.ButtonUp += TaskContainer.Completed;

				if (TaskContainer.DeadLine != null)
				{
					Date Deadline = TaskContainer.DeadLine;
					DeadlineLabel.Text = "Deadline: " + $"{Deadline.Year}/{Deadline.Month}/{Deadline.Day}";	
				}

				Container.AddChild(Instance);	
			}
		}	
	}

	public async void RevealPostIt()
	{
		if (PostItOut == false)
		{
			if (AlreadyAnimating){await ToSignal(AnimPlayer,"animation_finished");}
			PostItHitbox.MouseFilter = Control.MouseFilterEnum.Ignore;
			
			AnimPlayer.Play("PostItSliding");
			await ToSignal(AnimPlayer,"animation_finished");
			PostItHitbox.GetChild<Control>(0).MouseFilter = Control.MouseFilterEnum.Stop;
			PostItOut = true;	
		}
	}
	public async void ConsealPostIt()
	{
		await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
		if (AlreadyAnimating){await ToSignal(AnimPlayer,"animation_finished");}
		ConsealPostItFollowUp();
	}
	public void ConsealPostItFollowUp()
	{
		if (PostItOut == true && HoveringOnPostit == false)
		{
			PostItHitbox.MouseFilter = Control.MouseFilterEnum.Stop;
			PostItHitbox.GetChild<Control>(0).MouseFilter = Control.MouseFilterEnum.Ignore;
			AnimPlayer.PlayBackwards("PostItSliding");
			PostItOut = false;	
		}
	}


	public async void RevealTaskCreationPapper()
	{
		if (AlreadyAnimating){await ToSignal(AnimPlayer,"animation_finished");}
		AnimPlayer.Play("PapperSliding");
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

		InitializeTasks(TodoContainer,SaveGame);
		AnimPlayer.PlayBackwards("PapperSliding2");

		TempName = "";
		TempDescription = "";
	}

	public void CheckLineEditOverflow(string text)
	{
		if (text.Length >= 13)
		{
			if (ExtraPapperUsed == false)
			{
				TaskCreationPapper.GetNode<AnimationPlayer>("%AnimationPlayer").Play("TapeOnPapper");	
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


	public void HOP(){ HoveringOnPostit = true;}
	public void NHOP(){ HoveringOnPostit = false;}
	public void ANST(StringName name){ AlreadyAnimating = true;}
	public void ANE(StringName name){ AlreadyAnimating = false;}


}

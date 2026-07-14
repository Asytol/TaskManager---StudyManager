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
	[Export] public VBoxContainer CompletedContainer;

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
	private Godot.Collections.Array<string> TempLinks = [];
	private LineEdit NewTaskNameEditor;
	private TextEdit NewDescriptorEditor;

	[Export] private TextureRect TaskInfoPapper;
	

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
		ChangeTaskCreationMouseFilter(Control.MouseFilterEnum.Ignore);

		int i = 0;
		foreach (Node child in TaskCreationPapper.GetNode<VBoxContainer>("%LinkContainer").GetChildren())
		{
			if (child is LineEdit LinkEdit)
			{
				LinkEdit.TextChanged += (string text) => OnLinkEdited(text, i);
				i++;
			}
		}
		TempLinks.Resize(i+1);
		

		TextureButton ToDoBookmark = GetNode<TextureButton>("%ToDoBookmark");
		ToDoBookmark.MouseEntered += async () => await ScheduleAnimation("BookMarkExtend1");
		ToDoBookmark.MouseExited += async () => await ScheduleAnimation("BookMarkExtend1",true);
		ToDoBookmark.ButtonUp += () => SwitchPapper(1);

		TextureButton UpcomingBookmark = GetNode<TextureButton>("%UpcomingBookmark");
		UpcomingBookmark.MouseEntered += async () => await ScheduleAnimation("BookMarkExtend2");
		UpcomingBookmark.MouseExited += async () => await ScheduleAnimation("BookMarkExtend2",true);
		UpcomingBookmark.ButtonUp += () => SwitchPapper(2);

		TextureButton CompletedBookmark = GetNode<TextureButton>("%CompletedBookmark");
		CompletedBookmark.MouseEntered += async () => await ScheduleAnimation("BookMarkExtend3");
		CompletedBookmark.MouseExited += async () => await ScheduleAnimation("BookMarkExtend3",true);
		CompletedBookmark.ButtonUp += () => SwitchPapper(3);
		
		
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
			
			Node Instance = TaskScene.Instantiate();

			//All GameObjects
			Label TaskName = Instance.GetNode<Label>("%TaskName");
			Label DeadlineLabel = Instance.GetNode<Label>("%DeadlineLabel");
			Label RepeatNum = Instance.GetNode<Label>("%RepeatNum");
			TextureButton Options = Instance.GetNode<TextureButton>("%Options");
			TextureButton CheckMark = Instance.GetNode<TextureButton>("%CheckMark");

			

			TaskName.Text = TaskContainer.Name;
			RepeatNum.Text = TaskContainer.TimesRepeated.ToString();
			if (TaskContainer.Cleared == true)
			{
				CheckMark.Visible = false;
				Options.Visible = false;
				CompletedContainer.AddChild(Instance);
				return;				
			}
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
				Options.ButtonUp += () => RevealTaskInfoPapper(TaskContainer);
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
		ChangeTaskCreationMouseFilter(Control.MouseFilterEnum.Stop);
	}

	public async void RevealTaskInfoPapper(TaskContainerSaveCs TaskContainer)
	{
		TaskInfoPapper.GetNode<Label>("%TaskName").Text = TaskContainer.Name;
		TaskInfoPapper.GetNode<TextEdit>("%Descriptor").Text = TaskContainer.Description;
		TaskInfoPapper.GetNode<Label>("%TimesRepeated").Text = "Times repeated: " + TaskContainer.TimesRepeated;

		sbyte i = 0;
		foreach (Node child in TaskInfoPapper.GetNode<VBoxContainer>("%LinkContainer").GetChildren())
		{
			if (child is LinkButton button)
			{
				if (TaskContainer.Links.Count <= i)
				{		
					break;
				}
				button.Uri = TaskContainer.Links[i];

				
				sbyte CharNum = 0;
				char[] NewString = [];

				for (sbyte id = 0;id < TaskContainer.Links.Count; id++,CharNum++)
				{
					if (TaskContainer.Links[i][id] == '.')
					{
						if (id != CharNum)
						{
							TaskContainer.Links[i].CopyTo(id-CharNum+1,NewString,0,CharNum+1);
						}
						CharNum = 0;
					}
				}
				button.Text = $"Link{i+1}: {new string(NewString)}";

				i++;
			}
		}

		await ScheduleAnimation("TaskInfoOut");
	}
	public async void ConsealTaskInfoPapper()
	{
		foreach (Node child in TaskInfoPapper.GetNode<VBoxContainer>("%LinkContainer").GetChildren())
		{
			if (child is LinkButton button)
			{
				button.Text = "No_Link";
				button.Uri = "";
			}
		}
		await ScheduleAnimation("TaskInfoOut");
	}
	private void ChangeTaskCreationMouseFilter(Godot.Control.MouseFilterEnum Filter)
	{
		LineEdit NameEditor = TaskCreationPapper.GetNode<LineEdit>("%NameEditor");
		TextEdit TextEditor = TaskCreationPapper.GetNode<TextEdit>("%DescriptorEditor");
		Button SubmitButton = TaskCreationPapper.GetNode<Button>("%Submit");
	
		NameEditor.MouseFilter = Filter;
		TextEditor.MouseFilter = Filter;
		SubmitButton.MouseFilter = Filter;
		foreach (Node child in TaskCreationPapper.GetNode<VBoxContainer>("%LinkContainer").GetChildren())
		{
			if (child is LineEdit LinkEdit)
			{
				LinkEdit.MouseFilter = Filter;
			}
		}
	}

	public int CurrentPapperNum = 1;
	public async void SwitchPapper(int PapperNum)
	{
		string Animation = "SwitchPapper";
		string BookmarkAnimation = "BookMarkSlide";
		while (CurrentPapperNum != PapperNum)
		{
			if (CurrentPapperNum < PapperNum)
			{
				await ScheduleAnimation(Animation + CurrentPapperNum.ToString());
				await ScheduleAnimation(BookmarkAnimation + CurrentPapperNum.ToString());
				CurrentPapperNum++;
				continue;
			}

			CurrentPapperNum--;
			await ScheduleAnimation(Animation + CurrentPapperNum.ToString(),true);
			await ScheduleAnimation(BookmarkAnimation + CurrentPapperNum.ToString(),true);
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
	public void OnLinkEdited(string Text,int IndexNumber)
	{
		TempLinks[IndexNumber] = Text;
	}

	public void SubmitTask()
	{
		DateTime date = DateTime.Today;
		Date TempDate = new Date(date.Year,date.Month,date.Day);

		SaveGame.AddTaskContainer(TempName,TempDescription,TempDate,TempLinks);
		SaveGame.WriteSaveGame();

		InitializeTasks(SaveGame);
		AnimPlayer.PlayBackwards("PapperSliding2");
		ChangeTaskCreationMouseFilter(Control.MouseFilterEnum.Ignore);

		TempName = "";
		TempDescription = "";
		TempLinks.Clear();
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

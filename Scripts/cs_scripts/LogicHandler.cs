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

	
	[Export] private TextureRect MainBoard;
	[Export] private TextureButton WindowModeButton;	
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
		ChangeInfoPapperMouseFilter(Control.MouseFilterEnum.Ignore);

		int i = 0;
		foreach (Node child in TaskCreationPapper.GetNode<VBoxContainer>("%LinkContainer").GetChildren())
		{
			if (child is LineEdit LinkEdit)
			{
				LinkEdit.TextChanged += (string text) => OnLinkEdited(text, i);
				i++;
			}
		} 
		TempLinks.Resize(1);
		
		TaskInfoPapper.GetNode<TextureButton>("%CloseButton").ButtonUp += () => ConsealTaskInfoPapper();

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


		GD.Print("WindowId: " + DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle));
		GD.Print("Window DisplayHandle: " + DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.DisplayHandle));
		GetWindow().InitialPosition = Window.WindowInitialPosition.Absolute;
		WindowInFocus();
		GetWindow().Mode = Window.ModeEnum.Maximized;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//DisplayServer.WindowSetPosition(new Vector2I(200,100));
		//DisplayServer.WindowSetPosition
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
				if (TaskContainer.Name.Length > 13)
				{
					TaskName.Text = TaskContainer.Name.Substring(0,14)+"...";
				}
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
		if (TaskContainer.Links == null)
		{
			GD.Print("No links found");
			goto SkipLinks;
		}
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
				string NewString = "";

				for (sbyte id = 0; id < TaskContainer.Links[i].Length; id++,CharNum++)
				{
					if (TaskContainer.Links[i][id] == '.')
					{
						if (id != CharNum)
						{
							NewString = TaskContainer.Links[i].Substring(id-CharNum+1,id);
							GD.Print("Id2: "+id);
							break;
						}
						GD.Print("Id1: "+id);
						CharNum = 0;
					}
				}
				GD.Print(TaskContainer.Links[i]);
				button.Text = $"Link{i+1}: {NewString}";

				i++;
			}
		}
		SkipLinks:

		await ScheduleAnimation("TaskInfoOut");
		ChangeInfoPapperMouseFilter(Control.MouseFilterEnum.Stop);
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
		await ScheduleAnimation("TaskInfoOut",true);
		ChangeInfoPapperMouseFilter(Control.MouseFilterEnum.Ignore);
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
	private void ChangeInfoPapperMouseFilter(Godot.Control.MouseFilterEnum Filter)
	{
		TaskInfoPapper.MouseFilter = Filter;
		if (Filter == Control.MouseFilterEnum.Stop)
		{
			TaskInfoPapper.MouseFilter = Control.MouseFilterEnum.Pass;
		}
		
		Label TaskName = TaskInfoPapper.GetNode<Label>("%TaskName");
		TextEdit Description = TaskInfoPapper.GetNode<TextEdit>("%Descriptor");
		TextureButton CloseButton = TaskInfoPapper.GetNode<TextureButton>("%CloseButton");

		TaskName.MouseFilter = Filter;
		Description.MouseFilter = Filter;
		CloseButton.MouseFilter = Filter;
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
		TempLinks.Resize(IndexNumber+1);
		TempLinks[IndexNumber] = Text;
	}

	public void SubmitTask()
	{
		DateTime date = DateTime.Today;
		Date TempDate = new Date(date.Year,date.Month,date.Day-1);

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
				//Reminder, this is for another animation player, don't schedule it for fucks sake.
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



	private sbyte CurrentWindowFocusMode = 1; 

	public void SwitchWindowFocus()
	{
		CurrentWindowFocusMode += 1;
		if (CurrentWindowFocusMode == 1)
		{
			WindowInFocus();
			return;
		}
		if (CurrentWindowFocusMode == 2)
		{
			CurrentWindowFocusMode = 0;
		}
		
		WindowOutOfFocus();
	}
	public void WindowInFocus()
	{
		MainBoard.Visible = true;
		GetWindow().Size = new Vector2I(600,600);
		GetWindow().Mode = Window.ModeEnum.Maximized;
		GetWindow().Borderless = false;
		GetWindow().AlwaysOnTop = false;
		GetWindow().GrabFocus();
		WindowModeButton.StretchMode = TextureButton.StretchModeEnum.Keep;
		//WindowModeButton.SetAnchor();
		WindowModeButton.Size = new Vector2(48,26);
		//WindowModeButton.AnchorLeft = 0.5f; WindowModeButton.AnchorRight = 0.53f; WindowModeButton.AnchorTop = 0.5f; WindowModeButton.AnchorBottom = 0.5f;
		WindowModeButton.SetAnchorAndOffset(Side.Left,0.3f,0); WindowModeButton.SetAnchorAndOffset(Side.Right,0.3f,0);WindowModeButton.SetAnchorAndOffset(Side.Top,0.05f,0);WindowModeButton.SetAnchorAndOffset(Side.Bottom,0.1f,0);
		WindowModeButton.SetAnchorsPreset(Control.LayoutPreset.Center);

		Vector2I DisplaySize = DisplayServer.ScreenGetSize(DisplayServer.WindowGetCurrentScreen());
		Vector2I WindowSize = GetWindow().Size;
		GetWindow().Position = new Vector2I(DisplaySize.X/2-WindowSize.X/2,DisplaySize.Y/2-WindowSize.Y/2);
	}
	public void WindowOutOfFocus()
	{
		GetWindow().AlwaysOnTop = true;
		Vector2I DisplaySize = DisplayServer.ScreenGetSize(DisplayServer.WindowGetCurrentScreen());

		MainBoard.Visible = false;
		GetWindow().Mode = Window.ModeEnum.Windowed;
		GetWindow().Borderless = true;
		GetWindow().Size = new Vector2I(48*2,26*2);
		WindowModeButton.StretchMode = TextureButton.StretchModeEnum.Scale;
		WindowModeButton.Position = new Vector2(0,0);
		//WindowModeButton.AnchorLeft = 0f; WindowModeButton.AnchorRight = 0f; WindowModeButton.AnchorTop = 0f; WindowModeButton.AnchorBottom = 0f;
		WindowModeButton.Size = new Vector2(700,700);

		
		GetWindow().Position = new Vector2I(DisplaySize.X+400,DisplaySize.Y+50);
	}

	public void HOP(){ HoveringOnPostit = true;}
	public void NHOP(){ HoveringOnPostit = false;}
	public void ANST(StringName name){ AlreadyAnimating = true;}
	public void ANE(StringName name){ AlreadyAnimating = false;}


}

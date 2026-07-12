using Godot;


[GlobalClass]
public partial class TestFileCode : Resource
{
    [Export] public string Descriptor {get; set;}

    [Export] public string Description {get; set;}

    public TestFileCode() : this(null,null) {}

    public TestFileCode(string Descriptor, string Description)
    {
        this.Descriptor = Descriptor;
        this.Description = Description;
    }
}
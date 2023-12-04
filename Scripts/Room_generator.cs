using Godot;
using System;
using System.Threading.Tasks;

public partial class Room_generator : Node2D
{
	// presets for generating rooms
	private int amountOfRooms = 100;
	private float maxXScale = 20f;
    private float minXScale = 8f;
    private float maxYScale = 20f;
    private float minYScale = 8f;
	//spreading rooms apart
	private bool spread = false;
	private int count = 0;
	private Vector2 direction = Vector2.Zero;
	private float step = 1f;
	//temp statistics stuff
	private int loopCount = 0;
    public override void _Ready()
	{
		var rng = new RandomNumberGenerator();
		rng.Randomize();
        var room = GD.Load<PackedScene>("res://Scenes/room_generator.tscn");

		for (int i = 0; i < amountOfRooms; i++) //makes a certain amount of "rooms"
		{
			var instance = room.Instantiate();
			AddChild(instance);
			instance.GetNode<CollisionShape2D>("Collision").Scale = new Vector2(Map(rng.Randf(),minXScale,maxXScale), Map(rng.Randf(), minYScale, maxYScale)); //generates random scale
            instance.GetNode<Area2D>(".").Position = new Vector2(rng.Randf()*10, rng.Randf())*10; //generates initial random position
        }
        GenerateRooms();
	}
	public async void GenerateRooms()
	{
        //this only runs once at the start of the game so the inefficiency is loading time
        while (!spread) //if the rooms have overlapping areas
		{
            loopCount += 1;
			step = 2 + (4000/(((0.1f*MathF.Pow(loopCount,2f))+200)));
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            count = 0; //resets count to check overlapping areas of child
            for (int i = 0; i < amountOfRooms; i++)
            {
                if (GetChild<Area2D>(i).HasOverlappingAreas()) //if there are overlapping areas on the child
                {
					direction = Vector2.Zero; //resets direction to 0
					for (int j = 0; j < GetChild<Area2D>(i).GetOverlappingAreas().Count; j++) //check every overlapping area
					{
                        direction += GetChild<Area2D>(i).Position - GetChild<Area2D>(i).GetOverlappingAreas()[j].Position; //finds difference between original area and overlapping area
                    }
                    direction = direction.Normalized();
					GetChild<Area2D>(i).Position += direction * step;
                }
				else
				{
					count += 1; //adds 1 to count if no overlapping areas
				}
            }
			if (count == amountOfRooms)
			{
				spread = true;
                GD.Print("fin");
				GD.Print(loopCount);
            }
        }
	}
	private float Map(float from, float min, float max)
	{
		return min+ (from * (max-min));
	}
}

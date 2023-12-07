using Godot;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

public partial class Room_generator : Node2D
{
	// presets for generating rooms
	private int amountOfRooms = 100;
	private float maxXScale = 20f;
    private float minXScale = 8f;
    private float maxYScale = 20f;
    private float minYScale = 8f;
	private float spreadFactor = 100f;
	//spreading rooms apart
	private bool spread = false;
	private int count = 0;
	private Vector2 direction = Vector2.Zero;
	private Vector2 displacement;
	private float step = 1f;
	//temp statistics stuff Remove at end
	private int loopCount = 0;
	//Deleting rooms
	private float deletingRoomsFactor = 0.5f; //Needs to be between 0-1
	private bool removedRooms;
	//Make edges
	List<triangle> triangulation = new List<triangle>();
	List<triangle> badTriangles = new List<triangle>();
	//
	class triangle
	{
		edge[] edges = new edge[3]; //3 edges make a triangle
		Vector2 circumcentre;
		public triangle(edge edge1,edge edge2, edge edge3) 
		{
			edges[0] = edge1; edges[1] = edge2; edges[2] = edge3; //set value of all the edges

		}
	}
	class edge
	{
		point[] points = new point[2]; //2 points make a edge
		edge(point point1,point point2)
		{
			points[0] = point1; points[1] = point2; //set value of the two points
		}
	}
	class point
	{
		Area2D area;
		Vector2 position;
		point(Area2D Area)
		{
			area = Area;
			position = Area.Position;
		}
	}
    public override void _Ready()
	{
		var rng = new RandomNumberGenerator();
		rng.Randomize();
        var room = GD.Load<PackedScene>("res://Scenes/room_generator.tscn");
		spreadFactor = amountOfRooms / 5f;

		for (int i = 0; i < amountOfRooms; i++) //makes a certain amount of "rooms"
		{
			var instance = room.Instantiate();
			AddChild(instance);
			instance.GetNode<CollisionShape2D>("Collision").Scale = new Vector2(Map(rng.Randf(),minXScale,maxXScale), Map(rng.Randf(), minYScale, maxYScale)); //generates random scale
            instance.GetNode<Area2D>(".").Position = new Vector2(rng.Randf()*spreadFactor, rng.Randf())*spreadFactor; //generates initial random position
        }
        GenerateRooms();
	}
	public async void GenerateRooms()
	{
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        #region spreading rooms
        //this only runs once at the start of the game so the inefficiency is loading time
        while (!spread) //if the rooms have overlapping areas
		{
			//step = 2 + (4000/(((0.1f*MathF.Pow(loopCount,2f))+200)));
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            count = 0; //resets count to check overlapping areas of child
            for (int i = 0; i < amountOfRooms; i++)
            {
                if (GetChild<Area2D>(i).HasOverlappingAreas()) //if there are overlapping areas on the child
                {
					direction = Vector2.Zero; //resets direction to 0
					for (int j = 0; j < GetChild<Area2D>(i).GetOverlappingAreas().Count; j++) //check every overlapping area
					{
						displacement = GetChild<Area2D>(i).Position - GetChild<Area2D>(i).GetOverlappingAreas()[j].Position;
                        direction += (10/displacement.Length()) * displacement.Normalized(); //finds difference between original area and overlapping area
                    }
                    direction = direction.Normalized();
					//loopCount += 1;
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
                //GD.Print("fin");
				//GD.Print(loopCount);
            }
        }
        #endregion
        #region delete rooms
        for (int i = 0; i < (amountOfRooms * deletingRoomsFactor); i++)
		{
			RemoveChild(GetChild<Area2D>(rng.RandiRange(0, (int)(amountOfRooms * deletingRoomsFactor) - i)));
		}
        #endregion
        #region delunay triangulation

        #endregion
    }
    private float Map(float from, float min, float max)
	{
		return min+ (from * (max-min));
	}
}

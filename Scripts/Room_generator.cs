using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public partial class Room_generator : Node2D
{
	// presets for generating rooms
    private enum generationState
    {
        spreadRooms,
        deleteRooms,
        triangulation,
        draw,
        done
    }
    private generationState currentState = generationState.spreadRooms;
    private int amountOfRooms = 10;
	private float maxXScale = 20f;
    private float minXScale = 8f;
    private float maxYScale = 20f;
    private float minYScale = 8f;
	private float spreadFactor = 100f;
	public PackedScene room;
	//spreading rooms apart
	private bool spread = false;
	private int count = 0;
	private Vector2 direction = Vector2.Zero;
	private Vector2 displacement;
	private float step = 5f;
	//temp statistics stuff Remove at end
	private int loopCount = 0;
	//Deleting rooms
	private float deletingRoomsFactor = 0.5f; //Needs to be between 0-1
	private bool removedRooms;
    private List<Area2D> roomsToDelete = new List<Area2D>();
	//Make edges
	public List<Triangle> triangulation =  new List<Triangle>();
	public List<Triangle> badTriangles = new List<Triangle>();
	public List<Edge> polygon = new List<Edge>();
	public List<Edge> polygonEdgeList = new List<Edge>();
    RandomNumberGenerator rng = new RandomNumberGenerator();
	public class Triangle
	{
		public Edge[] edges = new Edge[3]; //3 edges make a triangle
		public Point[] points = new Point[3];
		public Vector2 circumcentre;
		public float radius;
		public Triangle(List<Triangle> triangulation ,Point point1,Point point2, Point point3) 
		{
			points[0] = point1 ; points[1] = point2 ; points[2] = point3 ;
            #region edge check
            //checks weather the edge already exists and if not it creates a new one
            int edge1index = -1;
			int edge2index = -1;
			int edge3index = -1;
            for (int i = 0; i < triangulation.Count;i++) 
			{
				if (triangulation[i].ContainsEdge(new Edge(point1, point2)) != null && edge1index == -1)
                {
					edge1index = i;
                }
                if (triangulation[i].ContainsEdge(new Edge(point2, point3)) != null && edge2index == -1)
                {
					edge2index = i;
                }
                if (triangulation[i].ContainsEdge(new Edge(point1, point3)) != null)
                {
					edge3index = i;
                }
            }
			if (edge1index != -1)
			{
                edges[0] = triangulation[edge1index].ContainsEdge(new Edge(point1, point2));
            }
			else
			{
                edges[0] = new Edge(point1, point2);
            }
            if (edge2index != -1)
            {
                edges[1] = triangulation[edge2index].ContainsEdge(new Edge(point2, point3));
            }
            else
            {
                edges[1] = new Edge(point2, point3);
            }
            if (edge3index != -1)
            {
                edges[2] = triangulation[edge3index].ContainsEdge(new Edge(point1, point3));
            }
            else
            {
                edges[2] = new Edge(point1, point3);
            }
            #endregion
            findCircumcentre();
            findRadius();
        }
		public Edge ContainsEdge(Edge edgeToCompare)
		{
			Point point1 = edgeToCompare.points[0];
			Point point2 = edgeToCompare.points[0];
			for (int i = 0;i<3;i++)
			{
				if (edges[i].HasPoints(point1, point2))
				{
					return edges[i];
				}
			}
			return null;
		}
		public bool isWithin(Point newNode)
		{
			// check weather the new node lies within the circumcentre
			// pythag to check if it lies within
			if (Mathf.Pow(newNode.position.X - circumcentre.X, 2f) + Mathf.Pow(newNode.position.Y - circumcentre.Y, 2f) < MathF.Pow(radius, 2f))
			{
				return true;
			}
			else
			{
                return false;
            }
		}
		private void findCircumcentre()
		{
			//find circumcentre of the circumcircle
			//finds x coordinate of circumcentre
			circumcentre.X = ((edges[1].midpoint().X * edges[1].perpendicular()) - (edges[2].midpoint().X * edges[2].perpendicular()) + (edges[2].midpoint().Y - edges[1].midpoint().Y)) / (edges[1].perpendicular() - edges[2].perpendicular());
			//finds y coordinate of circumcentre
			circumcentre.Y = (edges[2].perpendicular() * (circumcentre.X - edges[2].midpoint().X)) + edges[2].midpoint().Y;
		}
		private void findRadius()
		{
			//find radius of circumcircle that the edges lie on
			//pythag to find distance between points and centre 
			radius = Mathf.Sqrt(MathF.Pow((circumcentre.X - edges[0].points[0].position.X),2f) + MathF.Pow((circumcentre.Y - edges[0].points[0].position.Y),2f));
		}
	}
	public class Edge
	{
		public Point[] points = new Point[2]; //2 points make a edge
		public Edge (Point point1,Point point2)
		{
			points[0] = point1; points[1] = point2; //set value of the two points
		}
		public Vector2 midpoint() //finds midpoint of edges
		{
			Vector2 mid;
			mid.X = 0.5f * (points[0].position.X + points[1].position.X);
            mid.Y = 0.5f * (points[0].position.Y + points[1].position.Y);
			return mid;
        }
		public float perpendicular() //finds perepndicular gradient of line that links the two points
		{
			float gradient = (points[0].position.X - points[1].position.X)/ (points[0].position.Y - points[1].position.Y);
			return (-1f / gradient);
        }
		public bool HasPoints(Point point1, Point point2)//checks weather the edge is composed of specified points
		{
			if ((points[0] == point1 && points[1] == point2) || (points[0] == point2 && points[1] == point1))
			{
				return true;
			}
			else
			{
				return false;
			}
		}
	}
	public class Point
	{
		public Area2D area;
		public Vector2 position;
		public Point(Area2D Area)
		{
			area = Area;
			position = Area.Position;
		}
		public void AlterPos(Vector2 newPos)
		{
			position = newPos;
		}
	}
    public override void _Ready()
	{
        rng.Seed = 69420;
        //rng.Randomize();  
        room = GD.Load<PackedScene>("res://Scenes/room_generator.tscn");
		spreadFactor = amountOfRooms / 5f;

        #region make rooms
        for (int i = 0; i < amountOfRooms; i++) //makes a certain amount of "rooms"
        {
            var instance = room.Instantiate();
            AddChild(instance);
            instance.GetNode<CollisionShape2D>("Collision").Scale = new Vector2(Map(rng.Randf(), minXScale, maxXScale), Map(rng.Randf(), minYScale, maxYScale)); //generates random scale
            instance.GetNode<Area2D>(".").Position = new Vector2(rng.Randf() * spreadFactor, rng.Randf()) * spreadFactor; //generates initial random position
        }
        #endregion
    }
    public override void _Process(double delta)
    {
        if (currentState == generationState.spreadRooms)
        {
            #region spreading rooms
            //step = 2 + (4000/(((0.1f*MathF.Pow(loopCount,2f))+200)));
            //Thread.Sleep(1);
            count = 0; //resets count to check overlapping areas of child
            for (int i = 0; i < amountOfRooms; i++)
            {
                if (GetChild<Area2D>(i).HasOverlappingAreas()) //if there are overlapping areas on the child
                {
                    direction = Vector2.Zero; //resets direction to 0
                    for (int j = 0; j < GetChild<Area2D>(i).GetOverlappingAreas().Count; j++) //check every overlapping area
                    {
                        displacement = GetChild<Area2D>(i).Position - GetChild<Area2D>(i).GetOverlappingAreas()[j].Position;
                        direction += (10 / displacement.Length()) * displacement.Normalized(); //finds difference between original area and overlapping area
                    }
                    direction = direction.Normalized();
                    //loopCount += 1;
                    GetChild<Area2D>(i).Position += direction * step * rng.Randf();
                }
                else
                {
                    count += 1; //adds 1 to count if no overlapping areas
                }
            }
            if (count == amountOfRooms)
            {
                currentState = generationState.deleteRooms;
                return;
                //GD.Print("fin");
                //GD.Print(loopCount);
            }
            /* moves the rooms one a time and often breaks because room gets stuck in a loop
            for (int i = 0; i < GetChildCount();i++)
            {
                Thread.Sleep(1);
                while (GetChild<Area2D>(i).HasOverlappingAreas())
                {
                    Thread.Sleep(1);
                    direction = Vector2.Zero; //resets direction to 0
                    for (int j = 0; j < GetChild<Area2D>(i).GetOverlappingAreas().Count; j++) //check every overlapping area
                    {
                        displacement = GetChild<Area2D>(i).Position - GetChild<Area2D>(i).GetOverlappingAreas()[j].Position;
                        direction += (10 / displacement.Length()) * displacement.Normalized(); //finds difference between original area and overlapping area
                    }
                    direction = direction.Normalized();
                    //loopCount += 1;
                    GetChild<Area2D>(i).Position += direction * step;
                }
            }
            */
            #endregion
        }
        if (currentState == generationState.deleteRooms)
        {
            GD.Print("aasdasdassglisdfosi;rgliydrhoslo");
            GD.Print((int)(amountOfRooms * deletingRoomsFactor));
            #region delete rooms
            for (int i = 0; i < (int)(amountOfRooms * deletingRoomsFactor); i++)
            {
                count = rng.RandiRange(0, GetChildCount());
                RemoveChild(GetChild<Area2D>(count));
            }
            /*
            for (int i = 0; i <roomsToDelete.Count; i++)
            {
                RemoveChild(roomsToDelete[i]);
            }
            */
            currentState = generationState.triangulation;
            #endregion
        }
        if (currentState == generationState.triangulation)
        {
            #region delunay triangulation
            //creates supertriangle that should be large enough to hold all points
            GD.Print("make super tri");
            Point superTriPoint1 = new Point(new Area2D());
            superTriPoint1.AlterPos(new Vector2(-999999f, -999999f));
            Point superTriPoint2 = new Point(new Area2D());
            superTriPoint2.AlterPos(new Vector2(-999999f, 999999f));
            Point superTriPoint3 = new Point(new Area2D());
            superTriPoint3.AlterPos(new Vector2(999999f, 0f));
            Triangle superTri = new Triangle(triangulation, superTriPoint1, superTriPoint2, superTriPoint3);
            triangulation.Add(superTri);
            GD.Print("start big looop");
            for (int i = 0; i < GetChildCount(); i++)
            {
                GD.Print("gerererge " + i);
                //resets badtriangles
                badTriangles = new List<Triangle>();
                GD.Print("badtriangles list fin");
                // makes new point
                Point newPoint = new Point(GetChild<Area2D>(i));
                GD.Print("newPoint made");
                // checks which triangles are invalid because new point
                GD.Print(triangulation.Count());
                for (int y = 0; y < triangulation.Count(); i++)
                {
                    if (triangulation[y].isWithin(newPoint))
                    {
                        badTriangles.Add(triangulation[y]);
                    }
                }
                GD.Print("made it past badtrianges added");
                // for remeshing when new point is added
                polygon = new List<Edge>();
                polygonEdgeList = new List<Edge>();
                for (int y = 0; y < badTriangles.Count(); y++)
                {
                    triangulation.Remove(badTriangles[y]);
                    for (int x = 0; x < 3; x++)
                    {
                        polygonEdgeList.Add(badTriangles[y].edges[x]);
                    }
                }
                for (int y = 0; y < polygonEdgeList.Count(); y++) //checks if there is one or more item in list
                {
                    int count = 0;
                    for (int x = 0; x < polygonEdgeList.Count(); x++) //checks if values are equal
                    {
                        if (polygonEdgeList[y] == polygonEdgeList[x] && x != y)
                        {
                            count += 1;
                        }
                    }
                    if (count == 1) //if there is one of an item type add it to polygon
                    {
                        polygon.Add(polygonEdgeList[y]);
                    }
                }
                // re triangulate mesh from polygon
                for (int y = 0; y < polygon.Count; y++)
                {
                    triangulation.Add(new Triangle(triangulation, newPoint, polygon[y].points[0], polygon[y].points[1])); // add new triangle to triangulation
                }
            }
            // clean up points from superTri
            badTriangles = new List<Triangle>();
            for (int i = 0; i < triangulation.Count; i++)
            {
                for (int y = 0; i < 3; y++)
                {
                    if (triangulation[i].points[y] == superTriPoint1 || triangulation[i].points[y] == superTriPoint2 || triangulation[i].points[y] == superTriPoint3)
                    {
                        badTriangles.Add(triangulation[i]);
                    }
                }
            }
            for (int i = 0; i < badTriangles.Count; i++)
            {
                triangulation.Remove(badTriangles[i]);
            }
            currentState = generationState.draw;
            #endregion
        }
        if (currentState == generationState.draw)
        {
            #region draw result
            GD.Print("ligma");
            QueueRedraw();
            currentState = generationState.done;
            #endregion
        }
        if (currentState == generationState.done)
        {
            return;
        }
    }
    private float Map(float from, float min, float max)
	{
		return min+ (from * (max-min));
	}
    public override void _Draw()
    {
		if (triangulation.Count == 0)
		{
			return;
		}
		Vector2 position1;
		Vector2 position2;
		for (int i = 0;i < triangulation.Count;i++)
		{
			for (int y = 0; y <3; y++) 
			{
				position1 = triangulation[i].edges[y].points[0].position;
				position2 = triangulation[i].edges[y].points[1].position;
				DrawLine(position1,position2, new Color(1f, 0.752941f, 0.796078f, 1f), 10f);
            }
		}
    }
}	
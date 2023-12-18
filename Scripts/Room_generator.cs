using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    private int amountOfRooms = 500;
	private float maxXScale = 20f;
    private float minXScale = 8f;
    private float maxYScale = 20f;
    private float minYScale = 8f;
	private float spreadFactor;
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
	private float deletingRoomsFactor = 0.8f; //Needs to be between 0-1 (if its 0.8 it will delete 80% of rooms)
	private bool removedRooms;
    private List<Area2D> roomsToDelete = new List<Area2D>();
    //Deluaney triangulation
    public int loops = -1;
	public List<Triangle> triangulation =  new List<Triangle>();
	public List<Triangle> badTriangles = new List<Triangle>();
	public List<Edge> polygon = new List<Edge>();
	public List<Edge> polygonEdgeList = new List<Edge>();
    Point superTriPoint1;
    Point superTriPoint2;
    Point superTriPoint3;
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
            // equation from https://www.omnicalculator.com/math/circumcenter-of-a-triangle
            float t = Mathf.Pow(points[0].position.X, 2f) + Mathf.Pow(points[0].position.Y, 2f) - Mathf.Pow(points[1].position.X, 2f) - Mathf.Pow(points[1].position.Y, 2f);
            float u = Mathf.Pow(points[0].position.X, 2f) + Mathf.Pow(points[0].position.Y, 2f) - Mathf.Pow(points[2].position.X, 2f) - Mathf.Pow(points[2].position.Y, 2f);
            float J = ((points[0].position.X - points[1].position.X) * (points[0].position.Y - points[2].position.Y)) - ((points[0].position.X - points[2].position.X) * (points[0].position.Y - points[1].position.Y));
            circumcentre.X = ((t * (points[0].position.Y - points[2].position.Y)) - (u * (points[0].position.Y - points[1].position.Y))) / (2 * J);
            circumcentre.Y = ((u * (points[0].position.X - points[1].position.X)) - (t * (points[0].position.X - points[2].position.X))) / (2 * J);
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
		spreadFactor = amountOfRooms * 5f;

        #region make rooms
        for (int i = 0; i < amountOfRooms; i++) //makes a certain amount of "rooms"
        {
            var instance = room.Instantiate();
            AddChild(instance);
            instance.GetNode<CollisionShape2D>("Collision").Scale = new Vector2(Map(rng.Randf(), minXScale, maxXScale), Map(rng.Randf(), minYScale, maxYScale)); //generates random scale
            instance.GetNode<Area2D>(".").Position = new Vector2(rng.Randf() * spreadFactor, rng.Randf() * spreadFactor); //generates initial random position
        }
        #endregion
    }
    public override void _Process(double delta)
    {
        if (currentState == generationState.spreadRooms)
        {
            #region spreading rooms
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
            /* other design that moves the rooms one a time and often breaks because room gets stuck in a loop
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
            if (loops == -1)
            {
                //creates supertriangle that should be large enough to hold all points
                superTriPoint1 = new Point(new Area2D());
                superTriPoint1.AlterPos(new Vector2(-999999f, -999999f));
                superTriPoint2 = new Point(new Area2D());
                superTriPoint2.AlterPos(new Vector2(-999999f, 999999f));
                superTriPoint3 = new Point(new Area2D());
                superTriPoint3.AlterPos(new Vector2(999999f, 0f));
                Triangle superTri = new Triangle(triangulation, superTriPoint1, superTriPoint2, superTriPoint3);
                triangulation.Add(superTri);
                loops = 0;
            }
            if (loops < GetChildCount() && loops != -1)
            {
                //resets badtriangles
                badTriangles = new List<Triangle>();
                // makes new point
                Point newPoint = new Point(GetChild<Area2D>(loops));
                // checks which triangles are invalid because new point
                for (int y = 0; y < triangulation.Count(); y++)
                {
                    if (triangulation[y].isWithin(newPoint))
                    {
                        badTriangles.Add(triangulation[y]);
                    }
                }
                // for remeshing when new point is added
                polygon = new List<Edge>();
                polygonEdgeList = new List<Edge>();
                for (int y = 0; y < badTriangles.Count(); y++)
                {
                    triangulation.Remove(badTriangles[y]); //remove bad triangles from triangulation
                    for (int x = 0; x < 3; x++)
                    {
                        polygonEdgeList.Add(badTriangles[y].edges[x]);
                    }
                }
                for (int y = 0; y < polygonEdgeList.Count(); y++) //checks if there is one or more item in list
                {
                    count = 0;
                    for (int x = 0; x < polygonEdgeList.Count(); x++) //checks if values are equal
                    {
                        if (AreTwoEdgesTheSame(polygonEdgeList[x], polygonEdgeList[y]) && x != y)
                        {
                            count += 1;
                        }
                    }
                    if (count == 0) //if there is one of an item type add it to polygon
                    {
                        polygon.Add(polygonEdgeList[y]);
                        //GD.Print("polygon edge added");
                    }
                }
                // re triangulate mesh from polygon
                for (int y = 0; y < polygon.Count; y++)
                {
                    triangulation.Add(new Triangle(triangulation, newPoint, polygon[y].points[0], polygon[y].points[1])); // add new triangle to triangulation
                }
            }
            loops += 1;
            if (loops == GetChildCount())
            //if (loops == 3)
            {
                // clean up points from superTri
                badTriangles = new List<Triangle>();
                for (int i = 0; i < triangulation.Count; i++)
                {
                    for (int y = 0; y < 3; y++)
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
            }
            #endregion
        }
        if (currentState == generationState.draw)
        {
            #region draw result
            QueueRedraw();
            currentState = generationState.done;
            #endregion
        }
        if (currentState == generationState.done)
        {
            return;
        }
    }
    private bool AreTwoEdgesTheSame(Edge edge1, Edge edge2)
    {
        // checks weather two edges have points which are the same
        return (edge1.points[0].position == edge2.points[0].position || edge1.points[0].position == edge2.points[1].position) 
            && (edge1.points[1].position == edge2.points[0].position || edge1.points[1].position == edge2.points[1].position);
    }
    private float Map(float from, float min, float max)
	{
		return min+ (from * (max-min));
	}
    public override void _Draw()
    {
		if (triangulation.Count == 0) //gets called once at the start of the script so we wanna make sure not to do that one
		{
			return;
		}
		Vector2 position1;
		Vector2 position2;
		for (int i = 0;i < triangulation.Count;i++) // for each triangle in triangulation
		{
			for (int y = 0; y <3; y++)  //for each edge in the triangle
			{
                //get position of both points on the edge
				position1 = triangulation[i].edges[y].points[0].position;
				position2 = triangulation[i].edges[y].points[1].position;
                //draw line between two points
                DrawLine(position1,position2, new Color(1f, 0.752941f, 0.796078f, 1f), 10f);
            }
		}
    }
}	
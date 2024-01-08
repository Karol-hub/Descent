using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
        removeEdges,
        connectSections,
        makeCoridoors,
        draw,
        done
    }
    private generationState currentState = generationState.spreadRooms;
    private int amountOfRooms = 5;
	private float maxXScale = 20f;
    private float minXScale = 10f;
    private float maxYScale = 15f;
    private float minYScale = 8f;
	private float spreadFactor;
	public PackedScene room1;
    public PackedScene room2;
	//spreading rooms apart
	private bool spread = false;
	private int count = 0;
	private Vector2 direction = Vector2.Zero;
	private Vector2 displacement;
	private float step = 4f;
	//temp statistics stuff Remove at end
	private int loopCount = 0;
	//Deleting rooms
	private float deletingRoomsFactor = 0f; //Needs to be between 0-1 (if its 0.8 it will delete 80% of rooms)
	private bool removedRooms;
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
    //Remove Edges
    public List<Point> roomList = new List<Point>();
    public float edgeDeleteFactor = 0.8f;
    //Section stuff
    public List<List<Point>> sections = new List<List<Point>>();
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
        //point in a graph also represents a room in the code
		public Area2D area;
		public Vector2 position;
        public List<Point> connectedPoints = new List<Point>();
		public Point(Area2D Area)
		{
			area = Area;
			position = Area.Position;
		}
		public void AlterPos(Vector2 newPos)
		{
			position = newPos;
		}
        public void connectPoint(Point newPoint)
        {
            if (newPoint == null || newPoint.position == this.position || this.connectedPoints.Where(x => x.position == newPoint.position).Count() != 0)
            {
                GD.Print("invalid point");
                return;
            }
            this.connectedPoints.Add(newPoint);
        }
	}
    public override void _Ready()
	{
        rng.Seed = 69420;
        //rng.Randomize();  
        room1 = GD.Load<PackedScene>("res://Scenes/RoomVars/room_var_1.tscn");
        room2 = GD.Load<PackedScene>("res://Scenes/RoomVars/room_var_2.tscn");
        spreadFactor = amountOfRooms * 5f;
        int randNum;
        #region make rooms
        for (int i = 0; i < amountOfRooms; i++) //makes a certain amount of "rooms"
        {
            Node instance;
            randNum = rng.RandiRange(0, 1);
            if (randNum == 0)
            {
                instance = room1.Instantiate();
            }
            else if (randNum == 1)
            {
                instance = room2.Instantiate();
            }
            else
            {
                instance = room1.Instantiate();
            }
            AddChild(instance);
            //instance.GetNode<CollisionShape2D>("Collision").Scale = new Vector2(Map(rng.Randf(), minXScale, maxXScale), Map(rng.Randf(), minYScale, maxYScale)); //generates random scale
            instance.GetNode<Area2D>(".").Position = new Vector2((float)Math.Round(rng.Randf() * spreadFactor), (float)Math.Round(rng.Randf() * spreadFactor)); //generates initial random position
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
                    direction = direction.Normalized() * step;
                    direction.X = (float)Math.Round(direction.X) * step;
                    direction.Y = (float)Math.Round(direction.Y) * step;
                    GetChild<Area2D>(i).Position += direction;
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
        else if (currentState == generationState.deleteRooms)
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
        else if (currentState == generationState.triangulation)
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
                //Dumps all edges into polygon for use
                polygon = new List<Edge>();
                
                for (int triangle = 0; triangle < triangulation.Count();triangle++)
                {
                    for (int edges = 0; edges < 3;edges++)
                    {
                        count = 0;
                        for (int poly = 0;poly < polygon.Count;poly++)
                        {
                            if (AreTwoEdgesTheSame(triangulation[triangle].edges[edges], polygon[poly])) //check if item is already in list
                            {
                                count += 1;
                            }
                        }
                        if (count == 0) //if item isnt already in list add it to list
                        {
                            polygon.Add(triangulation[triangle].edges[edges]);
                        }
                    }
                }
                currentState = generationState.removeEdges;
                roomList = MakeGraph(polygon);
                polygonEdgeList = polygon;
                loopCount = 0;
            }
            #endregion
        }
        else if (currentState == generationState.removeEdges)
        {
            //removes a certain amount of edges
            int polyCount = polygon.Count;
            for (int i = 0; i < (int)(edgeDeleteFactor * polyCount); i++)
            {
                polygon.RemoveAt(rng.RandiRange(0,polygon.Count-1));
            }
            currentState = generationState.connectSections;
            GD.Print("polygon count: "+polygon.Count);
            sections = DetectSections(polygon);
        }
        else if (currentState == generationState.connectSections)
        {
            if (sections.Count != 1)
            {
                // add some edges
                
                // Detect sections
                //sections = DetectSections(polygon);
                GD.Print("sections: "+sections.Count);
                currentState = generationState.draw;
            }
            else
            {
                currentState = generationState.draw;
            }
        }
        else if (currentState == generationState.draw)
        {
            #region draw result
            QueueRedraw();
            currentState = generationState.done;
            #endregion
        }
        else if (currentState == generationState.done)
        {
            return;
        }
    }
    public List<List<Point>> DetectSections(List<Edge> edges)
    {
        List<List<Point>> sects = new List<List<Point>>(); //we alr have a variable named section so we shorten it to this
        List<Point> newSection = new List<Point>();
        // each new item in sections is a new section (stray edeges not connected to eachother)
        GD.Print("Edge count: "+edges.Count);
        List<Point> graph = MakeGraph(edges);
        bool exists = false;
        GD.Print("Graph count: "+graph.Count);
        for (int i = 0; i < graph.Count;i++)
        {
            //checks weather point exists in sects
            exists = false;
            for (int j = 0; j < sects.Count;j++)
            {
                for (int k = 0; k < sects[j].Count;k++)
                {
                    if (graph[i] == sects[j][k])
                    {
                        exists = true;
                        break;
                    }
                }
                if (exists)
                {
                    break;
                }
            }
            // if it doesnt alr exists in sects
            if (!exists)//make a section with all the new connected edges
            {
                sects.Add(MakeSection(graph[i], new List<Point>()));
            }
        }
        return sects;
    }
    public List<Point> MakeSection (Point point, List<Point> exclude)
    {
        List<Point> pts = new List<Point>();
        List<Point> allPoints = new List<Point>(); 
        bool visited = false;
        GD.Print("connected points: "+point.connectedPoints.Count);
        for (int i = 0; i < point.connectedPoints.Count;i++)
        {
            exclude.Add(point.connectedPoints[i]);
        }
        for (int i = 0; i < point.connectedPoints.Count; i++)
        {
            visited = false;
            for (int j = 0; j < exclude.Count; j++)
            {
                if (exclude[j].position == point.connectedPoints[i].position)
                {
                    visited = true; 
                    break;
                }
            }
            if (!visited)
            {
                pts = MakeSection(point.connectedPoints[i], exclude);
                for (int k = 0; k < pts.Count; k++)
                {
                    allPoints.Add(pts[k]);
                }
            }
        }
        return allPoints;
    }
    public List<Edge> MakeEdges(List<Point> graph)
    {
        //turns a graph into a list of edges
        List<Edge> edges = new List<Edge>();
        Edge newEdge;
        bool exists;
        for (int i = 0; i < graph.Count; i++)
        {
            for (int j = 0; j < graph[i].connectedPoints.Count;j++)
            {
                exists = false;
                newEdge = new Edge(graph[i], graph[i].connectedPoints[j]);
                for (int k = 0; k < edges.Count; k++)
                {
                    if (AreTwoEdgesTheSame(edges[k],newEdge))
                    {
                        exists=true;
                        break;
                    }
                }
                if (!exists)
                {
                    edges.Add(newEdge);
                }
            }
        }
        return edges;
    }
    public List<Point> MakeGraph(List<Edge> EdgeList)
    {
        GD.Print(EdgeList.Count);
        // turns a list of edges into a graph
        List<Point> graph = new List<Point>();
        for (int i = 0; i < EdgeList.Count;i++)
        {
            //connect both points to eachother for each polygon
            bool newPoint1 = true;
            bool newPoint2 = true;
            //GD.Print("point1 is: " + polygon[loopCount].points[0].position);
            //GD.Print("point2 is: " + polygon[loopCount].points[1].position);
            for (int j = 0; j < graph.Count(); j++)
            {
                //check if point alr exists
                //GD.Print("roomList: " + roomList[j].position);
                if (graph[j].position == EdgeList[i].points[0].position)
                {
                    //point already exists so we want to connect other point to it
                    //graph[j].connectedPoints.Add(EdgeList[i].points[1]);
                    graph[j].connectPoint(EdgeList[i].points[1]);
                    newPoint1 = false;
                }
                if (graph[j].position == EdgeList[i].points[1].position)
                {
                    //want to do the same for other point
                    //graph[j].connectedPoints.Add(EdgeList[i].points[0]);
                    graph[j].connectPoint(EdgeList[i].points[0]);
                    newPoint2 = false;
                }
                if (!newPoint1 && !newPoint2)
                {
                    //GD.Print("both are false so break");
                    break;
                }
            }
            if (newPoint1)
            {
                //if point doesnt exist make it and connect other point to it
                graph.Add(EdgeList[i].points[0]);
                EdgeList[i].points[0].connectPoint(EdgeList[i].points[1]);
            }
            if (newPoint2)
            {
                //same thing for other point
                graph.Add(EdgeList[i].points[1]);
                EdgeList[i].points[1].connectPoint(EdgeList[i].points[0]);
            }
        }
        return graph;
    }
    private bool AreTwoEdgesTheSame(Edge edge1, Edge edge2)
    {
        // checks weather two edges have points which are the same
        //return ((edge1.points[0].position == edge2.points[0].position && edge1.points[1].position == edge2.points[1].position)
        //    ||  (edge1.points[0].position == edge2.points[1].position && edge1.points[0].position == edge2.points[1].position));
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
		for (int poly = 0;poly < polygon.Count;poly++) // for each triangle in triangulation
		{
            //get position of both points on the edge
			position1 = polygon[poly].points[0].position;
			position2 = polygon[poly].points[1].position;
            //draw line between two points
            DrawLine(position1,position2, new Color(1f, 0.752941f, 0.796078f, 1f), 10f);
		}
    }
}	
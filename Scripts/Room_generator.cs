using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Room_generator : Node2D
{
	// presets for generating rooms
    private enum generationState
    {
        spreadRooms,
        deleteRooms,
        triangulation,
        removeEdges,
        spawnBossRoom,
        connectSections,
        makeCoridoors,
        makeTilemap,
        spawnPlayer,
        draw,
        done
    }
    private generationState currentState = generationState.spreadRooms;
    private int amountOfRooms = 50;
    private Vector2 scaleRange = new Vector2(1f, 1.5f); //min and max of scalse of rooms
    private int roomVars = 10;
	//spreading rooms apart
	private bool spread = false;
	private int count = 0;
	private Vector2 direction = Vector2.Zero;
	private Vector2 displacement;
	private float step = 4f;
    private float roomSpreadFactor = 2f;
	//temp statistics stuff Remove at end
	private int loopCount = 0;
	//Deleting rooms
	private float deletingRoomsFactor = 0.6f; //Needs to be between 0-1 (if its 0.8 it will delete 80% of rooms)
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
    public float edgeDeleteFactor = 0.3f; //Needs to be between 0-1 (if its 0.8 it will keep 80% of edges)
    //Spawn spawn/boss room
    float roomDist = 100;
    //Section stuff
    public List<Edge> polygonDifference = new List<Edge>();
    public List<List<Point>> sections = new List<List<Point>>();
    //Coridoor stuff
    List<Point> coridoorGraph = new List<Point>();
    public Vector2 coridoorRadius = new Vector2(80f,40f);
    //make Tilemap
    TileMap tile;
    Vector2 minCoord = new Vector2(9999, 9999);
    Vector2 maxCoord = new Vector2(-9999, -9999);
    Vector2 currentCoord = new Vector2();
    PackedScene tileMapCheckScene;
    List<TileMapGeneration> tileMapCheck = new List<TileMapGeneration>();
    public int checkAmount;
    //spawn player
    Vector2 spawnCoords = new Vector2(-9999f, -9999f);
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
            FindCircumcentre();
            FindRadius();
        }
		public bool IsWithin(Point newNode)
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
        private Edge ContainsEdge(Edge edgeToCompare)
        {
            Point point1 = edgeToCompare.points[0];
            Point point2 = edgeToCompare.points[0];
            for (int i = 0; i < 3; i++)
            {
                //If edges contains the two points
                if (edges[i].HasPoints(point1, point2))
                {
                    return edges[i];
                }
            }
            return null;
        }
        private void FindCircumcentre()
		{
            //find circumcentre of the circumcircle
            // equation from https://www.omnicalculator.com/math/circumcenter-of-a-triangle
            float t = Mathf.Pow(points[0].position.X, 2f) + Mathf.Pow(points[0].position.Y, 2f) - Mathf.Pow(points[1].position.X, 2f) - Mathf.Pow(points[1].position.Y, 2f);
            float u = Mathf.Pow(points[0].position.X, 2f) + Mathf.Pow(points[0].position.Y, 2f) - Mathf.Pow(points[2].position.X, 2f) - Mathf.Pow(points[2].position.Y, 2f);
            float J = ((points[0].position.X - points[1].position.X) * (points[0].position.Y - points[2].position.Y)) - ((points[0].position.X - points[2].position.X) * (points[0].position.Y - points[1].position.Y));
            circumcentre.X = ((t * (points[0].position.Y - points[2].position.Y)) - (u * (points[0].position.Y - points[1].position.Y))) / (2 * J);
            circumcentre.Y = ((u * (points[0].position.X - points[1].position.X)) - (t * (points[0].position.X - points[2].position.X))) / (2 * J);
		}
		private void FindRadius()
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
            if (area == null)
            {
                position = new Vector2();
            }
            else
            {
                position = Area.Position;
            }
		}
		public void AlterPos(Vector2 newPos)
		{
			position = newPos;
		}
        public void ConnectPoint(Point newPoint)
        {
            if (newPoint == null || newPoint.position == position || connectedPoints.Where(x => x.position == newPoint.position).Count() != 0)
            {
                GD.Print("invalid point");
                return;
            }
            this.connectedPoints.Add(newPoint);
        }
	}
    public class TileMapGeneration
    {
        public Area2D area;
        public int platSize = 0;
        public int holeSize = 0;
        public int maxHoleSize;
        public int maxPlatSize;
        public TileMapGeneration(Area2D newArea)
        {
            RandomNumberGenerator rng = new RandomNumberGenerator();
            area = newArea;
            maxHoleSize = rng.RandiRange(2, 3);
            maxPlatSize = rng.RandiRange(4, 6);
        }
        public void checkTile(Vector2 newCoords)
        {
            //update position of checking box
            
        }
    }
    public override void _Ready()
	{
        tile = GetNode<TileMap>("../TileMap");
        tileMapCheckScene = GD.Load<PackedScene>("res://Scenes/tile_map_check.tscn");
        rng.Seed = 69420;
        //rng.Randomize();  
        float spreadFactor = amountOfRooms * 10f;
        int randNum;
        float scaleRand;
        #region make rooms
        Node instance;
        List<PackedScene> rooms = new List<PackedScene>();
        for (int i = 0; i < roomVars; i++)
        {
            rooms.Add(GD.Load<PackedScene>("res://Scenes/RoomVars/room_var_"+i+".tscn"));
        }
        for (int i = 0; i < amountOfRooms; i++) //makes a certain amount of "rooms"
        {
            randNum = rng.RandiRange(0, roomVars-1);
            instance = rooms[randNum].Instantiate();
            instance.Name = ("rm" + i);
            AddChild(instance);
            scaleRand = rng.RandfRange(scaleRange.X, scaleRange.Y);
            if (rng.RandiRange(0,1) == 0)
            {
                scaleRand *= -1;
            }
            instance.GetNode<Area2D>(".").Scale = new Vector2(scaleRand, scaleRand);
            instance.GetNode<Area2D>(".").Position = new Vector2(Mathf.Round(rng.Randf() * spreadFactor * 3f), Mathf.Round(rng.Randf() * spreadFactor)); //generates initial random position
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
                if (GetChild<Area2D>(i).GetOverlappingAreas().Where(x => x.Name.ToString().Substring(0,2) == "rm").Any() && GetChild<Area2D>(i) != null) //if there are overlapping areas on the child
                {
                    direction = Vector2.Zero; //resets direction to 0
                    for (int j = 0; j < GetChild<Area2D>(i).GetOverlappingAreas().Count; j++) //check every overlapping area
                    {
                        displacement = GetChild<Area2D>(i).Position - GetChild<Area2D>(i).GetOverlappingAreas()[j].Position;
                        direction += (10 / displacement.Length()) * displacement.Normalized(); //finds difference between original area and overlapping area
                    }
                    // Rounds the result to make the numbers easier to work with
                    direction = direction.Normalized() * step;
                    direction.X = Mathf.Round(direction.X) * step;
                    direction.Y = Mathf.Round(direction.Y) * step;
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
                count = rng.RandiRange(0, GetChildCount()-1);
                RemoveChild(GetChild<Area2D>(count));
            }
            /*
            for (int i = 0; i <roomsToDelete.Count; i++)
            {
                RemoveChild(roomsToDelete[i]);
            }
            */
            //spread each room
            for (int i = 0; i < GetChildCount(); i++)
            {
                GetChild<Area2D>(i).Position *= roomSpreadFactor;
            }
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
                    if (triangulation[y].IsWithin(newPoint))
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
                //After Alg is finished do other stuff
                currentState = generationState.removeEdges;
                loopCount = 0;
                //GD.Print("Polygon Edge Count: " + polygon.Count);
                //GD.Print("Edge List: " + polygonEdgeList.Count);
            }
            #endregion
        }
        else if (currentState == generationState.removeEdges)
        {
            //removes a certain amount of edges
            //GD.Print("Edges Removed: "+ (int)(polygon.Count*edgeDeleteFactor));
            polygonEdgeList.Clear();
            List<int> indexList = new List<int>();
            int newIndex;
            for (int i = 0; polygonEdgeList.Count() < (int)(polygon.Count * edgeDeleteFactor);i++)
            {
                // make it so that it takes a random edge and not in ordrer as it has too much structure
                rng.Seed = (ulong)(i);
                newIndex = rng.RandiRange(0,polygon.Count()-1);
                if (!indexList.Contains(newIndex))
                {
                    //GD.Print(newIndex);
                    //if the edge doesnt already exist
                    polygonEdgeList.Add(polygon[newIndex]);
                    indexList.Add(newIndex);
                }
                //GD.Print("loop: "+ polygonEdgeList.Count);
            }
            //GD.Print("Count: "+indexList.Count);
            //GD.Print("Poly Count: " +polygonEdgeList.Count);
            currentState = generationState.connectSections;
            //does stuff for next section
            //GD.Print("loop: " + polygonEdgeList.Count);
            polygonDifference = EdgeDiff(polygon,polygonEdgeList);
            sections = DetectSections(polygonEdgeList);
            //polygon is the entire triangulated thing
            //polygonEdgeList is edges left over after deleting
            //polygonDifference = polygon - sections
        }
        else if (currentState == generationState.connectSections)
        {
            if (sections.Count != 1)
            {
                // add some edges
                int index = rng.RandiRange(0, polygonDifference.Count - 1);
                polygonEdgeList.Add(polygonDifference[index]);
                polygonDifference.RemoveAt(index);
                // Detect sections
                sections = DetectSections(polygonEdgeList);
                //GD.Print("sections: "+sections.Count);
                //currentState = generationState.draw;
            }
            else
            {
                //remove rooms with no edges connecting to them
                List<Point> graph = MakeGraph(polygonEdgeList);
                List<Area2D> removeThese = new List<Area2D>();
                //check all children to see if they are a point
                for (int i = 0; i < GetChildCount(); i++)
                {
                    if (!graph.Where(x => x.position == GetChild<Area2D>(i).Position).Any())
                    {
                        //add to list of items to remove to not mess up the loop
                        removeThese.Add(GetChild<Area2D>(i));
                    }
                    //make evety position a multiple of 16
                }
                //remove items detected in loop
                for (int i = 0; i < removeThese.Count; i++)
                {
                    RemoveChild(removeThese[i]);
                }
                currentState = generationState.spawnBossRoom;
            }
        }
        else if (currentState == generationState.spawnBossRoom)
        {
            //polygonEdgeList is where we have to connect new room
            FindMinMax(polygonEdgeList);
            //set up variables needed
            Point closestPoint = new Point(null);
            List<Point> graph = MakeGraph(polygonEdgeList);
            float currentLength = 999999f;
            Node bossRoom = GD.Load<PackedScene>("res://Scenes/RoomVars/boss.tscn").Instantiate();
            AddChild(bossRoom);
            //genereate position between min and max X coord but bias center more
            //Make Y coord certain amount below other rooms.
            bossRoom.GetNode<Area2D>(".").Position = new Vector2(minCoord.X + rng.RandfRange(0.2f, 0.8f) * (maxCoord.X - minCoord.X), maxCoord.Y + roomDist);
            Vector2 bossRoomPos = bossRoom.GetNode<Area2D>(".").Position;
            //Make boss room into a point to add it to graph
            Point bossPoint = new Point(bossRoom.GetNode<Area2D>("."));
            graph.Add(bossPoint);
            foreach (Point room in graph)
            {
                if (currentLength > (room.position - bossRoomPos).Length() && room != bossPoint)
                {
                    //if room is closer to currently closest known room
                    closestPoint = room;
                    currentLength = (room.position - bossRoomPos).Length();
                }
            }
            GD.Print("boss distance between: "+currentLength);
            //connect boss to closest room
            bossPoint.ConnectPoint(closestPoint);
            closestPoint.ConnectPoint(bossPoint);
            currentLength = 999999f;
            rng.Randomize();
            //Do same thing for spawn room
            Node spawnRoom = GD.Load<PackedScene>("res://Scenes/RoomVars/spawn.tscn").Instantiate();
            AddChild(spawnRoom);
            //genereate position between min and max X coord but bias center more
            //Make Y coord certain amount above other rooms.
            spawnRoom.GetNode<Area2D>(".").Position = new Vector2(minCoord.X + rng.RandfRange(0.2f, 0.8f) * (maxCoord.X - minCoord.X), minCoord.Y - roomDist);
            Vector2 spawnRoomPos = spawnRoom.GetNode<Area2D>(".").Position;
            //Make spawn room into a point to add it to graph
            Point spawnPoint = new Point(spawnRoom.GetNode<Area2D>("."));
            graph.Add(spawnPoint);
            foreach (Point room in graph)
            {
                if (currentLength > (room.position - spawnRoomPos).Length() && room != spawnPoint)
                {
                    //if room is closer to currently closest known room
                    closestPoint = room;
                    currentLength = (room.position - spawnRoomPos).Length();
                }
            }
            GD.Print("spawn distance between: " + currentLength);
            //connect spawn to closest room
            spawnPoint.ConnectPoint(closestPoint);
            closestPoint.ConnectPoint(spawnPoint);
            //prepare for next 
            spawnCoords = spawnRoomPos;
            polygonEdgeList = MakeEdges(graph);
            currentState = generationState.makeCoridoors;
        }
        else if (currentState == generationState.makeCoridoors)
        {
            //make the coridoors horizontal and vertical
            polygon.Clear();
            coridoorGraph = MakeGraph(polygonEdgeList);
            for (int i = 0;i < polygonEdgeList.Count; i++)
            {
                Point point1 = polygonEdgeList[i].points[0];
                Point point2 = polygonEdgeList[i].points[1];
                Point newPoint1 = new Point(null);
                Point newPoint2 = new Point(null);
                newPoint1.AlterPos(new Vector2(point1.position.X, point2.position.Y));
                newPoint2.AlterPos(new Vector2(point2.position.X, point1.position.Y));
                //all possible paths made
                polygon.Add(new Edge(point1, newPoint1));
                polygon.Add(new Edge(point2, newPoint1));
                polygon.Add(new Edge(point1, newPoint2));
                polygon.Add(new Edge(point2, newPoint2));
            }
            
            PackedScene coridoor = GD.Load<PackedScene>("res://Scenes/RoomVars/coridoor.tscn");
            for (int i = 0; i < polygon.Count; i++)
            {
                //make area over all the edges
                //make sure they are tagges so that they can be recognised later
                Vector2 pos1 = polygon[i].points[0].position;
                Vector2 pos2 = polygon[i].points[1].position;
                Node instance;
                instance = coridoor.Instantiate();
                AddChild(instance);
                instance.Name = "cr" + i;
                instance.GetNode<Area2D>(".").Position = (pos1 + pos2) / 2;
                if (pos1 < pos2)
                {
                    instance.GetNode<Area2D>(".").Scale = ((pos1 - coridoorRadius) - (pos2 + coridoorRadius))/20; 
                }
                else
                {
                    instance.GetNode<Area2D>(".").Scale = ((pos1 + coridoorRadius) - (pos2 - coridoorRadius))/20;
                }
                instance.GetNode<Area2D>(".").Scale = new Vector2(Mathf.Abs(instance.GetNode<Area2D>(".").Scale.X), Mathf.Abs(instance.GetNode<Area2D>(".").Scale.Y));
            }
            
            //Do stuff to preparae for next stage
            currentState = generationState.makeTilemap;
            //Find the bottom left and top right of the ponts
            loops = 0;
            foreach (Area2D child in GetChildren())
            {
                child.Position = ((Vector2)ToTileCoords(child.Position) * 16f) + new Vector2(8f, 8f);
            }
            FindMinMax(polygon);
            currentCoord = minCoord;
            checkAmount = (int)Mathf.Ceil((maxCoord.Y - minCoord.Y) / 16);
            Node tempNode;
            for (int i = 0; i < checkAmount; i++)
            {
                tempNode = tileMapCheckScene.Instantiate();
                AddChild(tempNode);
                tempNode.GetNode<Area2D>(".").Position = currentCoord + new Vector2(0f, 16f * i);
                tileMapCheck.Add(new TileMapGeneration(tempNode.GetNode<Area2D>(".")));
            }
            GD.Print("minCoord: "+minCoord);
            GD.Print("maxCoord: " +maxCoord);
            loops = 0;
        }
        else if (currentState == generationState.makeTilemap)
        {
            //old Method very slow
            //if it hasnt reached the side yet
            if (currentCoord.X < maxCoord.X)
            {
                //GD.Print(currentCoord);
                //check each box
                for (int i = 0; i < tileMapCheck.Count(); i++)
                {
                    //room tiles
                    tileMapCheck[i].area.Position = currentCoord + new Vector2(0f, 16f * i);
                    if (tileMapCheck[i].area.GetOverlappingAreas().Where(x => x.Name.ToString() == "bd").Any() &&
                        !tileMapCheck[i].area.GetOverlappingAreas().Where(x => x.Name.ToString().Substring(0, 2) == "cr").Any())
                    {
                        tile.SetCell(0, ToTileCoords(tileMapCheck[i].area.Position), 0, new Vector2I(1, 1));
                        //if not then an empty space because coridoor leads into it.   
                    }
                    else if (tileMapCheck[i].area.GetOverlappingAreas().Where(x => x.Name.ToString() == "pl").Any())
                    {
                        //checks weather tile should be a platform
                        tile.SetCell(0, ToTileCoords(tileMapCheck[i].area.Position), 0, new Vector2I(4, 4));
                    }
                    else if (tileMapCheck[i].area.GetOverlappingAreas().Where(x => x.Name.ToString() == "rm").Any())
                    {
                        //do nothing just need to not do other stuff
                        tile.SetCell(0, ToTileCoords(tileMapCheck[i].area.Position), 0, new Vector2I(-1, -1));
                    }

                    //coridoors
                    if (tileMapCheck[i].area.GetOverlappingAreas().Where(x => x.Name.ToString().Substring(0, 2) == "cr").Where(x => x.Scale.X < x.Scale.Y).Any() &&
                        !tileMapCheck[i].area.GetOverlappingAreas().Where(x => x.Name.ToString().Substring(0, 2) == "rm").Any() &&
                        (((tileMapCheck[i].area.Position.Y - 8) / 16) % 5) == 0f)
                    {
                        //make platform in vertical coridoors
                        if (tileMapCheck[i].platSize < tileMapCheck[i].maxPlatSize)
                        {
                            //fill with tile
                            tile.SetCell(0, ToTileCoords(tileMapCheck[i].area.Position), 0, new Vector2I(1, 1));
                            tileMapCheck[i].platSize += 1;
                            tileMapCheck[i].holeSize = 99999;
                        }
                        else if (tileMapCheck[i].holeSize < tileMapCheck[i].maxHoleSize)
                        {
                            //make hole
                            tileMapCheck[i].platSize = 0;
                            tileMapCheck[i].holeSize += 1;
                        }
                        else
                        {
                            tileMapCheck[i].holeSize = 0;
                        }
                    }
                    else
                    {
                        tileMapCheck[i].platSize += 1;
                        tileMapCheck[i].holeSize = 0;
                    }

                    //checks for empty space
                    if (!tileMapCheck[i].area.HasOverlappingAreas())
                    {
                        //checks weather tile should exist or not
                        tile.SetCell(0, ToTileCoords(tileMapCheck[i].area.Position), 0, new Vector2I(4, 7));
                    }
                }
                
                
                currentCoord.X += 16f;
            }
            else
            {
                //GD.Print("Y coord Iterated: " + currentCoord);
                //remove all the children
                foreach (TileMapGeneration child in tileMapCheck)
                {
                    RemoveChild(child.area);
                }
                currentState = generationState.spawnPlayer;
            }
        }
        else if (currentState == generationState.spawnPlayer)
        {
            GD.Print("Got to spawn player");
            currentState = generationState.done;
            //GetNode("/root").RemoveChild(GetNode("../Camera_holder"));
            PackedScene character = GD.Load<PackedScene>("res://Scenes/character.tscn");
            Node player;
            player = character.Instantiate();
            GetNode("/root").AddChild(player);
            GD.Print("Spawn Coords: "+spawnCoords);
            player.GetNode<CharacterBody2D>(".").Position = spawnCoords;
            player.GetNode<Camera2D>("./Camera2D").MakeCurrent();
            
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
    private void FindMinMax(List<Edge> edges)
    {
        minCoord = new Vector2(9999, 9999);
        maxCoord = new Vector2(-9999, -9999);
        List<Point> graph = MakeGraph(edges);
        Vector2 pos = new Vector2();
        for (int i = 0; i < graph.Count; i++)
        {
            pos = graph[i].position;
            if (pos.X < minCoord.X)
            { minCoord.X = pos.X; }
            if (pos.Y < minCoord.Y)
            { minCoord.Y = pos.Y; }
            if (pos.X > maxCoord.X)
            { maxCoord.X = pos.X; }
            if (pos.Y > maxCoord.Y)
            { maxCoord.Y = pos.Y; }
        }
        //one tile is 16 units so need to int division it so it alligns with tilemap
        minCoord = ((Vector2)ToTileCoords(minCoord) * 16f) - new Vector2(1032f, 1032f);
        maxCoord = ((Vector2)ToTileCoords(maxCoord) * 16f) + new Vector2(1032f, 1032f);
    }
    private Vector2I ToTileCoords(Vector2 coords)
    {
        coords += new Vector2(8f, 8f);
        Vector2I newCoords = new Vector2I();
        newCoords.X = (int)((coords.X - (coords.X % 16))/16);
        newCoords.Y = (int)((coords.Y - (coords.Y % 16))/16);
        return newCoords;
    }
    public List<List<Point>> DetectSections(List<Edge> edges)
    {
        List<List<Point>> sects = new List<List<Point>>(); //we alr have a variable named section so we shorten it to this
        // each new item in sections is a new section (stray edeges not connected to eachother)
        //GD.Print("Edge count: "+edges.Count);
        List<Point> graph = MakeGraph(edges);
        bool exists = false;
        //GD.Print("Graph count: "+graph.Count);
        for (int i = 0; i < graph.Count;i++)
        {
            //checks weather point exists in sects
            exists = false;
            for (int j = 0; j < sects.Count;j++)
            {
                for (int k = 0; k < sects[j].Count;k++)
                {
                    if (graph[i].position == sects[j][k].position)
                    {
                        //GD.Print("exists");
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
        List<Point> NewPoints = new List<Point>();
        exclude.Add(point);
        for (int i = 0; i < point.connectedPoints.Count; i++)
        {
            if (exclude.Where(x => x.position == point.connectedPoints[i].position).Count() == 0)
            {
                // check if item alr exists in list, if it does then dont add it
                NewPoints = MakeSection(point.connectedPoints[i], exclude);
                for (int j = 0; j < NewPoints.Count; j++)
                {
                    if (exclude.Where(x => x.position == NewPoints[j].position).Count() == 0)
                    {
                        exclude.Add(NewPoints[j]);
                    }
                }
            }
        }
        return exclude;
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
        //GD.Print(EdgeList.Count);
        // turns a list of edges into a graph
        List<Point> graph = new List<Point>();
        //clear connected points of all of the points first
        for (int i = 0; i < EdgeList.Count; i++)
        {
            EdgeList[i].points[0].connectedPoints.Clear();
            EdgeList[i].points[1].connectedPoints.Clear();
        }
        for (int i = 0; i < EdgeList.Count;i++)
        {
            //connect both points to eachother for each polygon
            bool newPoint1 = true;
            bool newPoint2 = true;
            //adding to existing points
            //Old method that DOES WORK
            for (int j = 0; j < graph.Count(); j++)
            {
                //check if point alr exists
                if (graph[j].position == EdgeList[i].points[0].position)
                {
                    //point already exists so we want to connect other point to it
                    graph[j].ConnectPoint(EdgeList[i].points[1]);
                    newPoint1 = false;
                }
                if (graph[j].position == EdgeList[i].points[1].position)
                {
                    //want to do the same for other point
                    graph[j].ConnectPoint(EdgeList[i].points[0]);
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
                EdgeList[i].points[0].ConnectPoint(EdgeList[i].points[1]);
            }
            if (newPoint2)
            {
                //same thing for other point
                graph.Add(EdgeList[i].points[1]);
                EdgeList[i].points[1].ConnectPoint(EdgeList[i].points[0]);
            }
            
        }
        return graph;
    }
    public List<Edge> EdgeDiff(List<Edge> totalEdges,List<Edge> subEdges)
    {
        //subtracts subEdges from totalEdges
        List<Edge> edges = new List<Edge>();
        for (int i = 0; i < totalEdges.Count;i++)
        {
            edges.Add(totalEdges[i]);
        }
        for (int i = 0; i < subEdges.Count; i++)
        {
            edges.RemoveAll(x => AreTwoEdgesTheSame(x, subEdges[i]));
            //GD.Print("TotalEdges: "+totalEdges.Count);
        }
        return edges;
    }
    public bool AreTwoEdgesTheSame(Edge edge1, Edge edge2)
    {
        // checks weather two edges have points which are the same
        return (edge1.points[0].position == edge2.points[0].position || edge1.points[0].position == edge2.points[1].position) 
            && (edge1.points[1].position == edge2.points[0].position || edge1.points[1].position == edge2.points[1].position);
    }
    private float Map(float from, float min, float max)
	{
		return min+ (from * (max-min));
	}
    /*
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
            DrawLine(position1,position2, new Color(1f, 0.752941f, 0.796078f, 1f), -5f);
		}
    }
    */
}	
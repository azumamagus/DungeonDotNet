using System;
using System.Collections.Generic;
using System.Linq;
using MvcRDMG.Generator.Helpers;
using MvcRDMG.Generator.Models;

namespace MvcRDMG.Generator.Core
{
    public class Dungeon
    {
        private const int Movement = 10;
        internal List<DungeonTile> Rooms = new List<DungeonTile>();
        internal List<DungeonTile> Doors = new List<DungeonTile>();
        public List<RoomDescription> RoomDescription { get; set; }
        public List<TrapDescription> TrapDescription { get; set; }
        public DungeonTile[][] DungeonTiles { get; set; }
        private List<DungeonTile> Result;
        private List<DungeonTile> Corridors;
        private int RoomDensity;
        private int TrapPercent;
        private int TrapCount;
        private int RoomCount;
        private bool HasDeadEnds;
        public int DungeonWidth { get; set; }
        public int DungeonHeight { get; set; }
        public int DungeonSize { get; set; }
        public int RoomSizePercent { get; set; }
        public int RoomSize { get; set; }

        public Dungeon(){

        }

        public Dungeon(int dungeonWidth, int dungeonHeight, int dungeonSize, int roomDensity, int roomSizePercent, int trapPercent, bool hasDeadEnds)
        {
            DungeonWidth = dungeonWidth;
            DungeonHeight = dungeonHeight;
            DungeonSize = dungeonSize;
            RoomDensity = roomDensity;
            RoomSizePercent = roomSizePercent;
            TrapPercent = trapPercent;
            HasDeadEnds = hasDeadEnds;
        }

        public virtual void Generate()
        {
            Init();
            GenerateRoom();
            AddEntryPoint();
            GenerateCorridors();
            if (HasDeadEnds)
            {
                AddDeadEnds();
            }
            AddRandomTrap();
        }

        public void AddRandomTrap()
        {
            int count = 0;
            while (TrapCount > count)
            {
                int x = Utils.Instance.GetRandomInt(0, Corridors.Count);
                int i = Corridors[x].I;
                int j = Corridors[x].J;
                if (DungeonTiles[i][j].Texture == Textures.CORRIDOR)
                {
                    AddTrap(i, j);
                    count++;
                }
            }
        }

        private void AddTrap(int x, int y)
        {
            DungeonTiles[x][y].Texture = Textures.TRAP;
            Utils.Instance.AddTrapDescription(DungeonTiles, x, y, TrapDescription);
        }

        public void AddDeadEnds()
        {
            var deadEnds = GenerateDeadEnds();
            var firstDoor = Doors[0]; // get first door
            foreach (DungeonTile end in deadEnds)
            {
                Doors = new List<DungeonTile>(); // empty doors
                Doors.Add(firstDoor);
                Doors.Add(end);
                GenerateCorridors();
            }
        }

        private List<DungeonTile> GenerateDeadEnds()
        {
            int count = RoomCount / 2;
            int deadEndsCount = 0;
            var deadEnds = new List<DungeonTile>();
            var croppedDungeonTiles = new DungeonTile[DungeonTiles.Length - 4][];
            for (int i = 0; i < DungeonTiles.Length - 4; i++)
            {
                croppedDungeonTiles[i] = new DungeonTile[DungeonTiles.Length - 4];
            }
            for (int i = 2; i < DungeonTiles.Length - 2; i++)
            {
                Array.Copy(DungeonTiles[i], 2, croppedDungeonTiles[i - 2], 0, DungeonTiles[i].Length - 4);
            }
            var dungeonList = croppedDungeonTiles.SelectMany(T => T).ToList();
            dungeonList.RemoveAll(i => Rooms.Contains(i));
            dungeonList.RemoveAll(i => Doors.Contains(i));
            dungeonList.RemoveAll(i => Corridors.Contains(i));
            int maxAttempt = dungeonList.Count * 2;
            do
            {
                var tile = dungeonList[Utils.Instance.GetRandomInt(0, dungeonList.Count)];
                if (CheckTileForDeadEnd(tile.I, tile.J))
                {
                    DungeonTiles[tile.I][tile.J].Texture = Textures.CORRIDOR;
                    deadEnds.Add(DungeonTiles[tile.I][tile.J]);
                    deadEndsCount++;
                }
                maxAttempt--;
            }
            while (count != deadEndsCount && maxAttempt > 0);
            return deadEnds;
        }

        private bool CheckTileForDeadEnd(int x, int y)
        {
            for (int i = x - 1; i < x + 2; i++)
            {
                for (int j = y - 1; j < y + 2; j++)
                {
                    if (DungeonTiles[i][j].Texture != Textures.MARBLE)
                    { // check if any other tile is there
                        return false;
                    }
                }
            }
            return true;
        }

        public void GenerateCorridors()
        {
            for (int d = 0; d < Doors.Count - 1; d++)
            { // -1 because the end point
                Result = new List<DungeonTile>();
                var openList = new List<DungeonTile>();
                var closedList = new List<DungeonTile>();
                var start = Doors[d]; // set door as the starting point
                var end = Doors[d + 1]; // set the next door as the end point
                for (int i = 1; i < DungeonTiles.Length - 1; i++)
                { // preconfig H value + restore default values
                    for (int j = 1; j < DungeonTiles.Length - 1; j++)
                    {
                        DungeonTiles[i][j].H = Utils.Instance.Manhattan(Math.Abs(i - end.I), Math.Abs(j - end.J));
                        DungeonTiles[i][j].G = 0;
                        DungeonTiles[i][j].Parent = null;
                        DungeonTiles[i][j].F = 9999;
                    }
                }
                AddToClosedList(closedList, start); // add start point to closed list
                AddToOpen(start, openList, closedList, end); // add the nearby nodes to openList
                while (!Result.Any() && openList.Any())
                {
                    start = openList[0]; // get lowest F to repeat things (openList sorted)
                    AddToClosedList(closedList, start); // add to closed list this node
                    RemoveFromOpen(openList, start); // remove from open list this node
                    AddToOpen(start, openList, closedList, end); // add open list the nearby nodes
                }
                SetPath(); // modify tiles Texture with the path
            }
        }

        private void SetPath()
        {
            foreach (var tile in Result)
            {
                if (tile.Texture == Textures.MARBLE)
                { // only change the marble texture
                    DungeonTiles[tile.I][tile.J].Texture = Textures.CORRIDOR;
                    Corridors.Add(tile);
                }
            }
        }

        internal void RemoveFromOpen(List<DungeonTile> openList, DungeonTile node)
        {
            openList.Remove(node);
        }

        private void AddToOpen(DungeonTile node, List<DungeonTile> openList, List<DungeonTile> closedList, DungeonTile end)
        {
            AddToOpenList(node, node.I, node.J - 1, openList, closedList, end); // left
            AddToOpenList(node, node.I - 1, node.J, openList, closedList, end); // top
            AddToOpenList(node, node.I + 1, node.J, openList, closedList, end); // bottom
            AddToOpenList(node, node.I, node.J + 1, openList, closedList, end); // right
            CalcGValue(openList); // calc G value Parent G + Movement
            CalcFValue(openList); // calc F value (G + H)
        }

        private void CalcFValue(List<DungeonTile> openList)
        {
            foreach (var tile in openList)
            {
                tile.F = (tile.G + tile.H);
            }
            // sort it
            openList.Sort((dt1, dt2) => dt1.F - dt2.F);
            //openList.Sort((x, y) => x.F.CompareTo(y.F));

        }

        private void CalcGValue(List<DungeonTile> openList)
        {
            foreach (var tile in openList)
            {
                tile.G = tile.Parent.G + Movement;
            }
        }

        private void AddToOpenList(DungeonTile node, int x, int y, List<DungeonTile> openList, List<DungeonTile> closedList, DungeonTile end)
        {
            if (!CheckEnd(node, x, y, end))
            {
                CheckG(node, x, y, openList); // check if it needs reparenting
                if (CheckTileForOpenList(x, y) && !closedList.Contains(DungeonTiles[x][y]) && !openList.Contains(DungeonTiles[x][y]))
                { // not in openlist/closedlist
                    SetParent(node, x, y);
                    openList.Add(DungeonTiles[x][y]);
                }
            }
        }

        private void SetParent(DungeonTile node, int x, int y)
        {
            DungeonTiles[x][y].Parent = node;
        }

        internal virtual bool CheckTileForOpenList(int x, int y)
        {
            return DungeonTiles[x][y].H != 0 && DungeonTiles[x][y].Texture != Textures.ROOM && DungeonTiles[x][y].Texture != Textures.ROOM_EDGE; // check its not edge/room/room_edge
        }

        private void CheckG(DungeonTile node, int x, int y, List<DungeonTile> openList)
        {
            if (openList.Contains(DungeonTiles[x][y]) && DungeonTiles[x][y].G > (node.G + Movement))
            {
                SetParent(node, x, y);
            }
        }

        private bool CheckEnd(DungeonTile node, int x, int y, DungeonTile end)
        {
            if (end.I == x && end.J == y)
            {
                SetParent(node, x, y);
                GetParents(node);
                return true;
            }
            return false;
        }

        private void GetParents(DungeonTile node)
        {
            Result.Add(node);
            var parent = node;
            while (parent.Parent != null)
            {
                parent = parent.Parent;
                Result.Add(parent);
            }
        }

        internal void AddToClosedList(List<DungeonTile> closedList, DungeonTile node)
        {
            closedList.Add(DungeonTiles[node.I][node.J]);
        }

        public virtual void AddEntryPoint()
        {
            bool entryIsOk;
            int x;
            int y;
            do
            {
                x = Utils.Instance.GetRandomInt(1, DungeonTiles.Length - 1);
                y = Utils.Instance.GetRandomInt(1, DungeonTiles.Length - 1);
                entryIsOk = DungeonTiles[x][y].Texture == Textures.MARBLE;
            }
            while (!entryIsOk);
            DungeonTiles[x][y].Texture = Textures.ENTRY;
            Doors.Add(DungeonTiles[x][y]);
        }

        public void GenerateRoom()
        {
            int[] coordinates;
            for (int i = 0; i < RoomCount; i++)
            {
                coordinates = SetTilesForRoom();
                if (coordinates[0] != 0)
                {
                    FillRoom(coordinates[0], coordinates[1], coordinates[2], coordinates[3]);
                }
            }
        }

        internal virtual void FillRoom(int x, int y, int right, int down)
        {
            int doorCount = GetDoorCount(down, right);
            for (int i = 0; i < down + 2; i++) // fill with room_edge texture the bigger boundaries
            {
                for (int j = 0; j < right + 2; j++)
                {
                    DungeonTiles[x + i - 1][y + j - 1].Texture = Textures.ROOM_EDGE;
                }
            }
            for (int i = 0; i < down; i++) // fill room texture
            {
                for (int j = 0; j < right; j++)
                {
                    DungeonTiles[x + i][y + j].Texture = Textures.ROOM;
                    Rooms.Add(DungeonTiles[x + i][y + j]);
                    DungeonTiles[x + i][y + j].Description = " ";
                    DungeonTiles[x + i][y + j].Index = RoomDescription.Count;
                }
            }
            int currentSize = Doors.Count;
            for (int d = 0; d < doorCount; d++)
            {
                AddDoor(x, y, down, right);
            }
            int newSize = Doors.Count;
            var currentDoors = Doors.GetRange(currentSize, newSize - currentSize);
            Utils.Instance.AddRoomDescription(DungeonTiles, x, y, RoomDescription, currentDoors);
        }

        internal void AddDoor(int x, int y, int down, int right)
        {
            bool doorIsOK;
            int doorX;
            int doorY;
            do
            {
                doorX = Utils.Instance.GetRandomInt(x, x + down);
                doorY = Utils.Instance.GetRandomInt(y, y + right);
                doorIsOK = CheckDoor(doorX, doorY);
            }
            while (!doorIsOK);
        }

        internal virtual bool CheckDoor(int x, int y)
        {
            for (int i = x - 1; i < x + 2; i++)
            {
                for (int j = y - 1; j < y + 2; j++)
                { // check nearby doors
                    if (DungeonTiles[i][j].Texture == Textures.DOOR || DungeonTiles[i][j].Texture == Textures.DOOR_LOCKED || DungeonTiles[i][j].Texture == Textures.DOOR_TRAPPED)
                    {
                        return false;
                    }
                }
            }
            return CheckEnvironment(x, y);
        }

        internal bool CheckEnvironment(int x, int y)
        {
            if (DungeonTiles[x][y - 1].Texture == Textures.ROOM_EDGE) // left
            {
                SetDoor(x, y - 1);
                return true;
            }
            else if (DungeonTiles[x][y + 1].Texture == Textures.ROOM_EDGE) // right
            {
                SetDoor(x, y + 1);
                return true;
            }
            else if (DungeonTiles[x + 1][y].Texture == Textures.ROOM_EDGE) // bottom
            {
                SetDoor(x + 1, y);
                return true;
            }
            else if (DungeonTiles[x - 1][y].Texture == Textures.ROOM_EDGE) // top
            {
                SetDoor(x - 1, y);
                return true;
            }
            return false;
        }

        internal virtual void SetDoor(int x, int y)
        {
            if (Utils.Instance.GetRandomInt(0, 101) < 40)
            {
                DungeonTiles[x][y].Texture = Textures.DOOR_TRAPPED;
            }
            else if (Utils.Instance.GetRandomInt(0, 101) < 50)
            {
                DungeonTiles[x][y].Texture = Textures.DOOR_LOCKED;
            }
            else
            {
                DungeonTiles[x][y].Texture = Textures.DOOR;
            }
            Doors.Add(DungeonTiles[x][y]);
        }

        internal virtual int GetDoorCount(int down, int right)
        {
            if (down < 4 || right < 4)
            {
                return Utils.Instance.GetRandomInt(1, 3);
            }
            else
            {
                return Utils.Instance.GetRandomInt(2, 5);
            }
        }

        private int[] SetTilesForRoom()
        {
            bool roomIsOk;
            int x;
            int y;
            int max = DungeonTiles.Length - (RoomSize + 2); // because of edge + room_edge
            int failSafeCount = DungeonTiles.Length * DungeonTiles.Length / 2;
            int right;
            int down;
            do
            {
                x = Utils.Instance.GetRandomInt(3, max); // 3 because of edge + room_edge
                y = Utils.Instance.GetRandomInt(3, max);
                right = Utils.Instance.GetRandomInt(2, RoomSize + 1);
                down = Utils.Instance.GetRandomInt(2, RoomSize + 1);
                roomIsOk = CheckTileGoodForRoom(x - 2, y - 2, right + 2, down + 2); // -2/+2 because i want min 3 tiles between rooms
                failSafeCount--;
            }
            while (!roomIsOk && failSafeCount > 0);
            if (failSafeCount > 0)
            {
                return new int[] { x, y, right, down };
            }
            else
            {
                return new int[] { 0, 0, 0, 0 }; // it can never be 0 if its a good coordinate
            }
        }

        private bool CheckTileGoodForRoom(int x, int y, int right, int down)
        {
            int maxX = x + down + 2; // +2 because of edges
            int maxY = y + right + 2;
            for (int i = x; i < maxX; i++) // check the room area + boundaries
            {
                for (int j = y; j < maxY; j++)
                {
                    if (CheckIsRoom(i, j))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool CheckIsRoom(int x, int y)
        {
            return DungeonTiles[x][y].Texture == Textures.ROOM || DungeonTiles[x][y].Texture == Textures.ROOM_EDGE;
        }

        public virtual void Init()
        {
            Corridors = new List<DungeonTile>();
            int ImgSizeX = DungeonWidth / DungeonSize;
            int ImgSizeY = DungeonHeight / DungeonSize;
            RoomCount = (int)Math.Round(((float)DungeonSize / 100) * (float)RoomDensity);
            RoomSize = (int)Math.Round((float)(DungeonSize - Math.Round(DungeonSize * 0.35)) / 100 * RoomSizePercent);
            RoomDescription = new List<RoomDescription>();
            TrapDescription = new List<TrapDescription>();
            TrapCount = DungeonSize * TrapPercent / 100;
            DungeonSize += 2; // because of boundaries
            DungeonTiles = new DungeonTile[DungeonSize][];
            for (int i = 0; i < DungeonSize; i++)
            {
                DungeonTiles[i] = new DungeonTile[DungeonSize];
            }
            for (int i = 0; i < DungeonSize; i++)
            {
                for (int j = 0; j < DungeonSize; j++)
                {
                    DungeonTiles[i][j] = new DungeonTile(i, j);
                }
            }
            for (int i = 1; i < DungeonSize - 1; i++) // set drawing area
            {
                for (int j = 1; j < DungeonSize - 1; j++)
                {
                    DungeonTiles[i][j] = new DungeonTile((j - 1) * ImgSizeX, (i - 1) * ImgSizeY, i, j, ImgSizeX, ImgSizeY, Textures.MARBLE);
                }
            }
        }
    }
}
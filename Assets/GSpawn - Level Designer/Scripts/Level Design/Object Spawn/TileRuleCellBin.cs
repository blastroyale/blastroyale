#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace GSpawn
{
    public class TileRuleCellBin
    {
        [SerializeField]
        public Dictionary<Vector3Int, GameObject> tileMap = new Dictionary<Vector3Int, GameObject>();

        public void clear()
        {
            tileMap.Clear();
        }

        public void add(GameObject tileObject, int x, int y, int z)
        {
            tileMap.Add(new Vector3Int(x, y, z), tileObject);
        }

        public void add(GameObject tileObject, Vector3Int cellCoords)
        {
            tileMap.Add(cellCoords, tileObject);
        }

        public void remove(int x, int y, int z)
        {
            tileMap.Remove(new Vector3Int(x, y, z));
        }

        public void remove(Vector3Int cellCoords)
        {
            tileMap.Remove(cellCoords);
        }

        public void removeIfExists(int x, int y, int z)
        {
            var cellCoords = new Vector3Int(x, y, z);
            if (tileMap.ContainsKey(cellCoords))
                tileMap.Remove(cellCoords);
        }

        public GameObject getTileObject(int x, int y, int z)
        {
            GameObject tile = null;
            tileMap.TryGetValue(new Vector3Int(x, y, z), out tile);
            return tile;
        }

        public GameObject getTileObject(Vector3Int cellCoords)
        {
            GameObject tile = null;
            tileMap.TryGetValue(cellCoords, out tile);
            return tile;
        }
    }
}
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileGrid : MonoBehaviour
{
    public TileRow[] rows { get; private set; }
    public TileCell[] cells { get; private set; }

    public int size => cells.Length;
    public int height => rows.Length;
    public int width => size/height;

    public static TileGrid instance { get; private set; }

    private void Awake()
    {
        instance = this;
        cells = GetComponentsInChildren<TileCell>();
        rows = GetComponentsInChildren<TileRow>();
    }

    private void Start()
    {
        for(int y = 0; y < rows.Length; y++) 
        {
            for (int x = 0; x < rows[y].cells.Length; x++)
            {
                rows[y].cells[x].coordinates = new Vector2Int(x, y);
            }
        }
    }

    public TileCell GetCell(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
            return rows[y].cells[x];
        return null;
    }

    public TileCell GetCell(Vector2Int coordinates)
    {
        return GetCell(coordinates.x, coordinates.y);
    }

    public TileCell GetCellDown(TileCell cell)
    {
        Vector2Int coordinates = cell.coordinates;
        coordinates.x = cell.coordinates.x;
        coordinates.y += 1;

        return GetCell(coordinates);
    }

    public TileCell GetCellUp(TileCell cell)
    {
        Vector2Int coordinates = cell.coordinates;
        coordinates.x = cell.coordinates.x;
        coordinates.y -= 1;

        return GetCell(coordinates);
    }

    public TileCell GetCellLeft(TileCell cell)
    {
        Vector2Int coordinates = cell.coordinates;
        coordinates.x -= 1;
        coordinates.y = cell.coordinates.x;

        return GetCell(coordinates);
    }

    public TileCell GetCellRight(TileCell cell)
    {
        Vector2Int coordinates = cell.coordinates;
        coordinates.x += 1;
        coordinates.y = cell.coordinates.x;

        return GetCell(coordinates);
    }


}

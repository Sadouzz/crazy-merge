using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public enum ConnectionType
{
    None,
    Horizontal,
    Vertical,
    L_Shape
}

[System.Serializable]
public class TileConnection
{
    public List<Tile> connectedTiles;
    public ConnectionType connectionType;
    public bool isBlocked; // Si une tuile de la liaison est bloquée

    public TileConnection()
    {
        connectedTiles = new List<Tile>();
        connectionType = ConnectionType.None;
        isBlocked = false;
    }

    public void AddTile(Tile tile)
    {
        if (!connectedTiles.Contains(tile))
        {
            connectedTiles.Add(tile);
            tile.connection = this;
        }
    }

    public void RemoveTile(Tile tile)
    {
        if (connectedTiles.Contains(tile))
        {
            connectedTiles.Remove(tile);
            tile.connection = null;

            // Si il ne reste qu'une tuile, supprimer la liaison
            if (connectedTiles.Count <= 1)
            {
                BreakConnection();
            }
            else
            {
                // Vérifier si on doit subdiviser la liaison
                CheckForSubdivision();
            }
        }
    }

    public void BreakConnection()
    {
        foreach (var tile in connectedTiles)
        {
            tile.connection = null;
        }
        connectedTiles.Clear();
        connectionType = ConnectionType.None;
    }

    private void CheckForSubdivision()
    {
        // Pour les liaisons en L, vérifier si on doit créer des liaisons séparées
        if (connectionType == ConnectionType.L_Shape)
        {
            // Logique pour subdiviser une liaison en L si nécessaire
            // Par exemple, si le point central de la L est supprimé
        }
    }

    public bool CanMoveDown()
    {
        if (connectionType == ConnectionType.Horizontal)
        {
            // Pour une liaison horizontale, toutes les tuiles doivent pouvoir descendre
            foreach (var tile in connectedTiles)
            {
                TileCell cellBelow = TileGrid.instance.GetCellDown(tile.cell);
                if (cellBelow == null || cellBelow.occupied)
                {
                    return false;
                }
            }
            return true;
        }
        else if (connectionType == ConnectionType.Vertical)
        {
            // Pour une liaison verticale, seule la tuile du bas doit être vérifiée
            Tile bottomTile = GetBottomTile();
            if (bottomTile != null)
            {
                TileCell cellBelow = TileGrid.instance.GetCellDown(bottomTile.cell);
                return cellBelow != null && !cellBelow.occupied;
            }
        }
        return false;
    }

    public void MoveAllTilesDown()
    {
        if (connectionType == ConnectionType.Horizontal && CanMoveDown())
        {
            foreach (var tile in connectedTiles)
            {
                TileCell cellBelow = TileGrid.instance.GetCellDown(tile.cell);
                if (cellBelow != null)
                {
                    tile.MoveTo(cellBelow, false);
                }
            }
        }
        else if (connectionType == ConnectionType.Vertical && CanMoveDown())
        {
            // Déplacer toute la liaison verticale d'une case vers le bas
            foreach (var tile in connectedTiles)
            {
                TileCell cellBelow = TileGrid.instance.GetCellDown(tile.cell);
                if (cellBelow != null)
                {
                    tile.MoveTo(cellBelow, false);
                }
            }
        }
    }

    private Tile GetBottomTile()
    {
        if (connectedTiles.Count == 0) return null;

        Tile bottomTile = connectedTiles[0];
        foreach (var tile in connectedTiles)
        {
            if (GetRowIndex(tile.cell) > GetRowIndex(bottomTile.cell))
            {
                bottomTile = tile;
            }
        }
        return bottomTile;
    }

    private static int GetRowIndex(TileCell cell)
    {
        for (int i = 0; i < TileGrid.instance.rows.Length; i++)
        {
            for (int j = 0; j < TileGrid.instance.rows[i].cells.Length; j++)
            {
                if (TileGrid.instance.rows[i].cells[j] == cell)
                    return i;
            }
        }
        return -1;
    }
}
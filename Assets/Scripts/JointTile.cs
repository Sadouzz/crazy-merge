using UnityEngine;

public class JointTile : MonoBehaviour
{
    public Tile[] jointTiles;
    public static JointTile instance { get; private set; }

    private void Awake()
    {
        instance = this;
    }

    public bool isMyTileDragged()
    {
        for (int i = 0; i < jointTiles.Length; i++)
        {
            if (jointTiles[i].isDragged)
                return true;
        }
        return false;
    }

    public int whichTileDragged()
    {
        for (int i = 0; i < jointTiles.Length; i++)
        {
            if (jointTiles[i].isDragged)
                return i;
        }
        return -1;
    }

    public void ParentTile(Tile tileA, Tile tileB)
    {
        tileB.transform.SetParent(tileA.transform);
    }

    // Nouvelle méthode utile
    public Tile GetOtherTile(Tile currentTile)
    {
        for (int i = 0; i < jointTiles.Length; i++)
        {
            if (jointTiles[i] != currentTile)
            {
                return jointTiles[i];
            }
        }
        return null;
    }
}
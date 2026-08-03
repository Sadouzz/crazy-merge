using System.Collections;
using System.Collections.Generic;

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TileBoard : MonoBehaviour
{
    public GameObject mergeParticle;
    public GameObject[] tilePrefabs;
    public Sprite[] tileSprites;
    public List<TileState> tileStates;
    public TileGrid grid;

    public List<Tile> tiles;
    public Slider timerSlider;

    public bool movingTiles, waiting;
    public float pushTimer = 0f, pushInterval = 15f;

    public bool isMerging = false, oneTileIsDragged, noMoreMove; // Nouveau champ
    public List<int> tileNumbers;

    public void Merge(Tile a, Tile b)
    {
        if (a == b) return;

        tiles.Remove(a);
        a.Merge(b.cell);

        b.gameObject.GetComponent<Image>().color = tileStates[b.number].backgroundColor;
        b.number++;
        b.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = b.number.ToString();
        Inventory.instance.AddToScore(b.number);
        Instantiate(mergeParticle, b.transform.position, new Quaternion(-90, 0, 0, 0));
        StartCoroutine(MergeComplete(b));
        noMoreMove = CheckForTileNumbers();
    }

    public static TileBoard instance { get; private set; }

    private void Awake()
    {
        instance = this;
        grid = GetComponentInChildren<TileGrid>();
        tiles = new List<Tile>(54);
    }

    private void Start()
    {
        Time.timeScale = 1f;
        timerSlider.maxValue = pushInterval;
        Application.targetFrameRate = 60;
        //Tile tile1 = SpawnSpecificTile(3, TileGrid.instance.rows[TileGrid.instance.rows.Length - 1].cells[2]);
        SaveManager.instance.LoadGame();
    }

    bool CheckForTileNumbers()
    {
        tileNumbers = new List<int>(new int[15]);
        foreach (var tile in tiles)
        {
            tileNumbers[tile.number - 1] += 1;
        }

        foreach (var tileNb in tileNumbers)
        {
            if (tileNb > 1)
            {
                return false;
            }
        }
        return true;
    }

    public bool IsGameOver()
    {
        int cols = TileGrid.instance.rows[0].cells.Length;

        for (int i = 0; i < cols; i++)
        {
            if (TileGrid.instance.rows[0].cells[i].tile != null)
                return true;
        }
        return false;
    }

    /*IEnumerator CheckTileDragged()
    {

    }*/


    public void NewPartyStart()
    {
        tileNumbers = new List<int>(new int[15]);
        CreateTile(0, true);
        CreateTile(1, true);
        noMoreMove = CheckForTileNumbers();

        Tile tile1 = SpawnSpecificTile(3, TileGrid.instance.rows[TileGrid.instance.rows.Length - 1].cells[2]);
        Tile tile2 = SpawnSpecificTile(3, TileGrid.instance.rows[TileGrid.instance.rows.Length - 1].cells[3]);

        //TileGroup group = TileGroupManager.instance.CreateGroup(tile1, tile2);
    }

    private void CreateTile(int index, bool isStart = false)
    {
        TileCell cell = TileGrid.instance.rows[TileGrid.instance.rows.Length - 1].cells[index];
        TileCell cellAbove = TileGrid.instance.GetCellUp(cell);
        int r = 0;
        if (cellAbove.tile != null)
        {
            r = GetRandomExcluding(0, tilePrefabs.Length - 1, cellAbove.tile.number - 1);
            Debug.Log("Excluding " + cellAbove.tile.number);
            Debug.Log("Obtenu " + r);
        }
        else
            r = Random.Range(0, tilePrefabs.Length - 1);



        Tile tile = Instantiate(tilePrefabs[r], grid.transform).GetComponent<Tile>();

        // 2. Then set its initial position
        tile.transform.position = new Vector2(GetPositionFromIndex(index), -450f);

        if (!isStart)
            tile.MoveTo(cell, false);
        else
            tile.MoveToWithoutAnim(cell, false);
        tiles.Add(tile);
    }

    public Tile SpawnSpecificTile(int value, TileCell cell)
    {
        Tile tile = Instantiate(tilePrefabs[value - 1], grid.transform).GetComponent<Tile>();
        //tile.transform.position = new Vector2(GetPositionFromIndex(index), -450f);
        tile.MoveToWithoutAnim(cell);
        tiles.Add(tile);

        return tile;
    }

    int GetPositionFromIndex(int index)
    {
        return -240 + 96 * index;
    }

    int GetRandomExcluding(int min, int max, int exclude = -1)
    {
        int result;
        do
        {
            result = Random.Range(min, max);
        } while (result == exclude);
        return result;
    }


    private void FixedUpdate()
    {
        timerSlider.value = pushInterval - pushTimer;

        pushTimer += Time.deltaTime;
        if (noMoreMove || (pushTimer >= pushInterval && !oneTileIsDragged))
        {
            pushTimer = 0f;
            if (!IsGameOver())
                StartCoroutine(PushNewRow());
            else
                PlayManager.instance.GameOver();
        }
    }

    private IEnumerator PushNewRow()
    {
        MoveTilesUp();
        NewRow();
        noMoreMove = CheckForTileNumbers();
        yield return StartCoroutine(WaitForChanges());
        CascadeTilesDown();
        SaveManager.instance.SaveGame();
    }

    private void CheckAllTilesBelow()
    {
        foreach (var cell in TileGrid.instance.cells)
        {
            if (cell != null && cell.tile != null)
            {
                cell.tile.CheckCellDown();
            }
        }
    }

    private void MoveTile(Tile tile)
    {
        TileCell newCell = null;
        TileCell downCell = grid.GetCellDown(tile.cell);

        while (downCell != null)
        {
            if (downCell.occupied)
            {
                break;
            }

            newCell = downCell;
            downCell = grid.GetCellDown(downCell);
        }

        if (newCell != null)
        {
            tile.MoveTo(newCell);
        }
    }

    public bool CanMerge(Tile a, Tile b)
    {
        return a != b && a.number == b.number;
    }

    /*public void Merge(Tile a, Tile b)
    {
        if (a == b) return;
        tiles.Remove(a);
        a.Merge(b.cell);

        b.gameObject.GetComponent<Image>().color = tileStates[b.number].backgroundColor;
        //b.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = tileStates[b.number].textColor;
        //b.gameObject.GetComponent<Image>().sprite = tileSprites[b.number];
        b.number++;
        
        b.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = b.number.ToString();
        Inventory.instance.AddToScore(b.number);
        Instantiate(mergeParticle, b.transform.position, new Quaternion(-90, 0, 0, 0));
        StartCoroutine(MergeComplete(b));
        noMoreMove = CheckForTileNumbers();
    }*/

    public IEnumerator MergeComplete(Tile mergedTile)
    {
        isMerging = true; // Bloque les inputs
        yield return StartCoroutine(WaitForChanges());
        CascadeTilesDown();
        SaveManager.instance.SaveGame();
        isMerging = false; // D�bloque les inputs
    }

    private void MoveTilesUp()
    {
        movingTiles = true;
        for (int i = 1; i < TileGrid.instance.rows.Length; i++)
        {
            for (int j = 0; j < TileGrid.instance.rows[i].cells.Length; j++)
            {
                TileCell currentCell = TileGrid.instance.rows[i].cells[j];

                if (currentCell.tile != null)
                {
                    TileCell cellAbove = TileGrid.instance.GetCellUp(currentCell);

                    if (cellAbove != null && cellAbove.tile == null)
                    {
                        if (currentCell.tile.isDragged)
                            currentCell.tile.ChangeCell(cellAbove);
                        //currentCell.tile.originalPosition.y += 97.33337f;
                        else
                            currentCell.tile.MoveTo(cellAbove, false);
                    }
                }
            }
        }
        movingTiles = false;
    }

    public void CascadeTilesDown()
    {
        bool movedAnyTile;

        do
        {
            movedAnyTile = false;

            // Check from bottom to top
            for (int i = TileGrid.instance.rows.Length - 2; i >= 0; i--)
            {
                for (int j = 0; j < TileGrid.instance.rows[i].cells.Length; j++)
                {
                    TileCell currentCell = TileGrid.instance.rows[i].cells[j];

                    if (currentCell.tile != null && !currentCell.tile.isDragged)
                    {
                        TileCell cellBelow = TileGrid.instance.GetCellDown(currentCell);

                        if (cellBelow != null && cellBelow.tile != currentCell.tile &&
                            (cellBelow.empty || TileBoard.instance.CanMerge(currentCell.tile, cellBelow.tile)))
                        {
                            if (cellBelow.empty)
                            {
                                currentCell.tile.MoveTo(cellBelow, false);
                            }
                            else
                            {
                                TileBoard.instance.Merge(currentCell.tile, cellBelow.tile);
                            }
                            movedAnyTile = true;
                        }
                    }
                }
            }

            // Small delay to allow animations to complete
            if (movedAnyTile)
            {
                StartCoroutine(WaitForChanges());
                return; // Exit and let the next Update cycle continue the cascade
            }
        }
        while (movedAnyTile);
    }

    public void NewRow()
    {
        for (int i = 0; i < TileGrid.instance.rows[TileGrid.instance.rows.Length - 1].cells.Length; i++)
        {
            CreateTile(i);
        }
    }

    public IEnumerator WaitForChanges()
    {
        waiting = true;
        yield return new WaitForSecondsRealtime(.1f);
        waiting = false;
    }
}

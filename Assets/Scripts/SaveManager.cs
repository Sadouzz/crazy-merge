using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public int score;
    public List<int> tilesNumbersInCells; // liste des entiers sauvegardés
}

public class SaveManager : MonoBehaviour
{
    public List<int> tilesNumbersInCells = new List<int>();

    string path;
    public static SaveManager instance { get; private set; }

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        path = Application.persistentDataPath + "/save.json";
    }

    //  Récupère l’état actuel de la grille et remplit tilesNumbersInCells
    public void CaptureGridState()
    {
        tilesNumbersInCells.Clear();

        int rows = TileGrid.instance.rows.Length;
        int cols = TileGrid.instance.rows[0].cells.Length;

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                TileCell currentCell = TileGrid.instance.rows[i].cells[j];
                if (currentCell.tile != null)
                {
                    tilesNumbersInCells.Add(currentCell.tile.number); // ta tuile doit avoir une variable "number"
                }
                else
                {
                    tilesNumbersInCells.Add(0); // case vide
                }
            }
        }
    }

    public void SaveGame()
    {
        CaptureGridState(); //  Avant de sauvegarder, on capture l’état

        SaveData data = new SaveData();
        data.score = Inventory.instance.score;
        data.tilesNumbersInCells = new List<int>(tilesNumbersInCells);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);

        Debug.Log("Sauvegarde effectuée : " + path);
    }

    public void LoadGame()
    {
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            Inventory.instance.score = data.score;
            tilesNumbersInCells = new List<int>(data.tilesNumbersInCells);

            Debug.Log("Chargement réussi !");
            ApplyGridState(); //  On applique l’état chargé
        }
        else
        {
            Debug.Log("Aucune sauvegarde trouvée !");
            Debug.Log("Nouvelle partie !");
            TileBoard.instance.NewPartyStart();
        }
    }

    public void DeleteSave()
    {
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("Fichier de sauvegarde supprimé : " + path);
        }
        else
        {
            Debug.Log("Aucune sauvegarde à supprimer !");
        }
    }


    // Recrée les tuiles dans la grille à partir de tilesNumbersInCells
    private void ApplyGridState()
    {
        int rows = TileGrid.instance.rows.Length;
        int cols = TileGrid.instance.rows[0].cells.Length;

        int index = 0;

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                int value = tilesNumbersInCells[index];
                TileCell cell = TileGrid.instance.rows[i].cells[j];

                // Supprime l’ancienne tuile si elle existe
                if (cell.tile != null)
                {
                    Destroy(cell.tile.gameObject);
                    Debug.Log("Prob");
                    cell.tile = null;
                }

                // Si la valeur est > 0, on recrée une tuile
                if (value > 0)
                {
                    Tile newTile = TileBoard.instance.SpawnSpecificTile(value, cell);
                    cell.tile = newTile;
                }

                index++;
            }
        }
    }
}

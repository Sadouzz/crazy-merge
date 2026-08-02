using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class Tile : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerUpHandler
{
    public TileCell cell { get; private set; }
    public TileCell previousCell;
    public int number;
    public int tileSize, smoothSpeed;

    public Collider2D myCollider;
    public Camera uiCamera;

    private RectTransform rectTransform;
    private Canvas canvas;
    public Vector2 originalPos, originalPosition, proposedPosition, previousValidPos;
    public bool isMerging = false, isDragged = false, isColliding, collidesWithOtherTile;

    public Rigidbody2D rb;
    public Vector3 offset;
    public bool isJoint;
    public JointTile jointParent;

    public TileConnection connection;
    public bool isConnected => connection != null && connection.connectedTiles.Count > 1;

    // Méthode pour créer une liaison horizontale
    public static TileConnection CreateHorizontalConnection(List<Tile> tiles)
    {
        if (tiles.Count < 2) return null;

        // Vérifier que toutes les tuiles sont sur la même ligne
        int targetRow = -1;
        foreach (var tile in tiles)
        {
            int row = GetRowIndex(tile.cell);
            if (targetRow == -1)
                targetRow = row;
            else if (row != targetRow)
                return null; // Les tuiles ne sont pas sur la même ligne
        }

        TileConnection connection = new TileConnection();
        connection.connectionType = ConnectionType.Horizontal;

        foreach (var tile in tiles)
        {
            connection.AddTile(tile);
        }

        return connection;
    }

    // Méthode pour créer une liaison verticale
    public static TileConnection CreateVerticalConnection(List<Tile> tiles)
    {
        if (tiles.Count < 2) return null;

        // Vérifier que toutes les tuiles sont sur la même colonne
        int targetCol = -1;
        foreach (var tile in tiles)
        {
            int col = GetColumnIndex(tile.cell);
            if (targetCol == -1)
                targetCol = col;
            else if (col != targetCol)
                return null;
        }

        TileConnection connection = new TileConnection();
        connection.connectionType = ConnectionType.Vertical;

        foreach (var tile in tiles)
        {
            connection.AddTile(tile);
        }

        return connection;
    }

    // Méthode pour créer une liaison en L
    public static TileConnection CreateLConnection(List<Tile> tiles)
    {
        if (tiles.Count < 3) return null;

        // Logique pour vérifier que les tuiles forment un L
        // (implémentation simplifiée)
        TileConnection connection = new TileConnection();
        connection.connectionType = ConnectionType.L_Shape;

        foreach (var tile in tiles)
        {
            connection.AddTile(tile);
        }

        return connection;
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

    private static int GetColumnIndex(TileCell cell)
    {
        for (int i = 0; i < TileGrid.instance.rows.Length; i++)
        {
            for (int j = 0; j < TileGrid.instance.rows[i].cells.Length; j++)
            {
                if (TileGrid.instance.rows[i].cells[j] == cell)
                    return j;
            }
        }
        return -1;
    }

    // Override de CheckCellDown pour prendre en compte les liaisons
    public void CheckCellDownWithConnection()
    {
        if (isConnected && connection.connectionType == ConnectionType.Horizontal)
        {
            // Si c'est une liaison horizontale, vérifier si toute la liaison peut descendre
            if (connection.CanMoveDown())
            {
                connection.MoveAllTilesDown();
            }
        }
        else if (!isConnected)
        {
            // Comportement normal pour les tuiles non connectées
            CheckCellDown();
        }
    }

    // Méthode appelée lors d'un merge pour gérer les liaisons
    public void OnMerge()
    {
        if (isConnected)
        {
            connection.RemoveTile(this);
        }
    }

    private void Awake()
    {
        uiCamera = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        myCollider = GetComponent<Collider2D>();
    }

    

    public void Spawn(TileCell cell)
    {
        if (this.cell != null)
        {
            this.cell.tile = null;
        }

        this.cell = cell;
        this.cell.tile = this;

        rectTransform.anchoredPosition = cell.GetComponent<RectTransform>().anchoredPosition;
    }

    public void CheckCellDown()
    {
        TileCell tileCellDown = TileGrid.instance.GetCellDown(cell);
        if (tileCellDown != null && !isJoint)
        {
            if (tileCellDown.empty)
            {
                MoveTo(tileCellDown);
            }
            else if (TileBoard.instance.CanMerge(this, tileCellDown.tile))
            {
                TileBoard.instance.Merge(this, tileCellDown.tile);
            }
        }
    }

    public void ChangeCell(TileCell cell)
    {
        if (this.cell != null)
        {
            this.cell.tile = null;
        }

        this.cell = cell;
        this.cell.tile = this;
    }

    public void SetCell(TileCell cell)
    {
        this.cell = cell;
        this.cell.tile = this;
    }

    public void ClearCell()
    {
        previousCell = this.cell;
        if (this.cell != null && this.cell.tile != null)
            this.cell.tile = null;
        this.cell = null;
    }

    public void MoveTo(TileCell cell, bool ckeckDown = true)
    {
        if (isMerging || cell == null || cell.tile == this || isDragged) return;
        if (this.cell != null)
        {
            this.cell.tile = null;
        }

        this.cell = cell;
        this.cell.tile = this;

        /*Vector3 worldPos = cell.GetComponent<RectTransform>().position;
        Vector3 localPos = rectTransform.parent.InverseTransformPoint(worldPos);
        rectTransform.anchoredPosition = localPos;*/
        StartCoroutine(Animate(cell.transform.position));
            
        StartCoroutine(TileBoard.instance.WaitForChanges());

        if( ckeckDown ) {
            TileCell tileCellDown = TileGrid.instance.GetCellDown(cell);
            if (tileCellDown)
            {
                if(tileCellDown.empty) 
                    MoveTo(tileCellDown);
                else
                {
                    if(TileBoard.instance.CanMerge(this, tileCellDown.tile))
                    {
                        TileBoard.instance.Merge(this, tileCellDown.tile);
                    }
                }
            }
        }
    }

    public void MoveToWithoutAnim(TileCell cell, bool ckeckDown = true)
    {
        if (isMerging || cell == null || cell.tile == this || isDragged) return;
        if (this.cell != null)
        {
            this.cell.tile = null;
        }

        this.cell = cell;
        this.cell.tile = this;

        transform.position = cell.transform.position;
    }

    /*public void OnBeginDrag(PointerEventData eventData)
    {
        if (TileBoard.instance.waiting || isMerging )
        {
            eventData.pointerDrag = null; // Annule le drag
            return;
        }
        originalPosition = rectTransform.anchoredPosition;
        TileBoard.instance.oneTileIsDragged = true;
        myCollider.isTrigger = true;
        GetComponent<RectTransform>().sizeDelta = new Vector2(93, 93);
        if (isJoint)
        {
            jointParent.ParentTile(this, jointParent.jointTiles[1]);
        }

        /*Vector3 worldPoint = Camera.main.ScreenToWorldPoint(eventData.position);
       offset = transform.position - new Vector3(worldPoint.x, worldPoint.y, transform.position.z);/
    }*/

    // Modifiez votre classe Tile pour gérer les collisions des joint tiles

    public void OnDrag(PointerEventData eventData)
    {
        if (isMerging || TileBoard.instance.waiting)
        {
            eventData.pointerDrag = null;
            return;
        }

        // Vérifie si c'est bien CETTE tile qui est draggée (pas l'autre joint tile)
        if (isJoint && jointParent != null)
        {
            // Si une autre joint tile est déjà en train d'être draggée, ne fait rien
            if (jointParent.isMyTileDragged() && jointParent.whichTileDragged() != GetMyJointIndex())
            {
                return; // Cette tile suit passivement, elle ne contrôle pas
            }
        }

        isDragged = true;
        transform.SetAsLastSibling();

        // Calcule le déplacement souhaité
        Vector2 deltaMove = eventData.delta / canvas.scaleFactor;
        Vector2 currentPos = rectTransform.anchoredPosition;
        Vector2 targetPos = currentPos + deltaMove;

        // Si c'est une joint tile, teste aussi les collisions de l'autre tile
        if (isJoint && jointParent != null)
        {
            Vector2 finalPos = TestMovementWithJointCollisions(currentPos, targetPos, deltaMove);
            rectTransform.anchoredPosition = finalPos;
        }
        else
        {
            // Comportement normal pour les tiles non-jointes
            Vector2 finalPos = TestMovementWithSteps(currentPos, targetPos);
            rectTransform.anchoredPosition = finalPos;
        }
    }

    // Nouvelle méthode pour tester les mouvements avec les joint tiles (simplifié)
    private Vector2 TestMovementWithJointCollisions(Vector2 currentPos, Vector2 targetPos, Vector2 deltaMove)
    {
        // Trouve l'autre tile jointe
        Tile otherJointTile = GetOtherJointTile();
        if (otherJointTile == null)
        {
            return TestMovementWithSteps(currentPos, targetPos);
        }

        // Teste le mouvement pour cette tile (celle qui contrôle)
        Vector2 myFinalPos = TestMovementWithSteps(currentPos, targetPos);

        // Calcule le mouvement réel effectué
        Vector2 actualDeltaMove = myFinalPos - currentPos;

        // Vérifie si l'autre tile peut suivre ce mouvement
        Vector2 otherCurrentPos = otherJointTile.rectTransform.anchoredPosition;
        Vector2 otherTargetPos = otherCurrentPos + actualDeltaMove;

        // Teste si l'autre tile peut aller à cette position
        if (!IsPositionValidForOtherTile(otherJointTile, otherTargetPos))
        {
            // Si l'autre tile ne peut pas suivre, limite le mouvement
            // Essaie avec un mouvement plus petit
            Vector2 limitedDelta = actualDeltaMove * 0.5f; // Réduit de moitié
            Vector2 limitedTarget = currentPos + limitedDelta;
            Vector2 limitedOtherTarget = otherCurrentPos + limitedDelta;

            if (IsPositionValid(limitedTarget) && IsPositionValidForOtherTile(otherJointTile, limitedOtherTarget))
            {
                return limitedTarget;
            }
            else
            {
                // Si même réduit ça ne marche pas, pas de mouvement
                return currentPos;
            }
        }

        // L'autre tile peut suivre, on autorise le mouvement complet
        return myFinalPos;
    }

    // Trouve l'autre tile dans le joint
    private Tile GetOtherJointTile()
    {
        if (!isJoint || jointParent == null) return null;

        for (int i = 0; i < jointParent.jointTiles.Length; i++)
        {
            if (jointParent.jointTiles[i] != this)
            {
                return jointParent.jointTiles[i];
            }
        }
        return null;
    }

    // Teste le mouvement spécifiquement pour l'autre joint tile
    /*private Vector2 TestMovementForOtherJointTile(Tile otherTile, Vector2 currentPos, Vector2 targetPos)
    {
        Vector2 finalPos = currentPos;

        // Teste d'abord le mouvement horizontal
        Vector2 horizontalPos = new Vector2(targetPos.x, currentPos.y);
        if (IsPositionValidForOtherTile(otherTile, horizontalPos))
        {
            finalPos.x = horizontalPos.x;
        }

        // Puis teste le mouvement vertical
        Vector2 verticalPos = new Vector2(finalPos.x, targetPos.y);
        if (IsPositionValidForOtherTile(otherTile, verticalPos))
        {
            finalPos.y = verticalPos.y;
        }

        return finalPos;
    }*/

    // Vérifie si une position est valide pour l'autre joint tile
    private bool IsPositionValidForOtherTile(Tile otherTile, Vector2 testPosition)
    {
        // Sauvegarde la position actuelle
        Vector2 originalPos = otherTile.rectTransform.anchoredPosition;

        // Bouge temporairement à la position test
        otherTile.rectTransform.anchoredPosition = testPosition;

        // Vérifie les collisions
        bool hasCollision = CheckCollisionForOtherTile(otherTile);

        // Restaure la position originale
        otherTile.rectTransform.anchoredPosition = originalPos;

        return !hasCollision;
    }

    // Vérifie les collisions pour l'autre joint tile
    private bool CheckCollisionForOtherTile(Tile otherTile)
    {
        Vector2 worldSize = new Vector2(
            otherTile.rectTransform.rect.width * otherTile.rectTransform.lossyScale.x,
            otherTile.rectTransform.rect.height * otherTile.rectTransform.lossyScale.y
        );

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask("Tiles", "BoardBorders"));
        filter.useLayerMask = true;
        filter.useTriggers = false;

        Collider2D[] results = new Collider2D[10];
        int count = Physics2D.OverlapBox(
            otherTile.rectTransform.position,
            worldSize * 0.75f,
            0f,
            filter,
            results
        );

        for (int i = 0; i < count; i++)
        {
            Collider2D col = results[i];
            if (col.gameObject != otherTile.gameObject && col.gameObject != this.gameObject)
            {
                if (col.CompareTag("Tile"))
                {
                    Tile collidedTile = col.GetComponent<Tile>();
                    // Si on peut merger avec cette tile
                    if (collidedTile != null && TileBoard.instance.CanMerge(otherTile, collidedTile))
                    {
                        // Décidez ici si vous voulez merger automatiquement ou non
                        // TileBoard.instance.Merge(otherTile, collidedTile);
                        return false; // Autorise le mouvement pour permettre le merge
                    }
                    return true; // Collision avec une tuile non-mergeable
                }
                else if (col.CompareTag("BoardBorder"))
                {
                    return true; // Collision avec la bordure
                }
            }
        }

        return false; // Pas de collision
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (TileBoard.instance.waiting || isMerging)
        {
            eventData.pointerDrag = null; // Annule le drag
            return;
        }

        // Vérifie si c'est bien CETTE tile qui commence le drag
        if (isJoint && jointParent != null)
        {
            // Si une autre joint tile est déjà en train d'être draggée, annule
            if (jointParent.isMyTileDragged())
            {
                eventData.pointerDrag = null;
                return;
            }
        }

        originalPosition = rectTransform.anchoredPosition;
        TileBoard.instance.oneTileIsDragged = true;
        myCollider.isTrigger = true;
        GetComponent<RectTransform>().sizeDelta = new Vector2(93, 93);

        if (isJoint)
        {
            // Trouve l'autre tile et la rend enfant de celle-ci
            Tile otherTile = GetOtherJointTile();
            if (otherTile != null)
            {
                otherTile.originalPosition = otherTile.rectTransform.anchoredPosition;
                jointParent.ParentTile(this, otherTile);
            }
        }
    }

    // Méthode pour trouver l'index de cette tile dans le joint
    private int GetMyJointIndex()
    {
        if (!isJoint || jointParent == null) return -1;

        for (int i = 0; i < jointParent.jointTiles.Length; i++)
        {
            if (jointParent.jointTiles[i] == this)
            {
                return i;
            }
        }
        return -1;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("OnPointerUp appelé - isDragged: " + isDragged);

        // Si on était en train de draguer mais que OnEndDrag n'a pas été appelé
        if (isDragged)
        {
            Debug.LogWarning("OnEndDrag manqué ! OnPointerUp prend le relais");
            OnEndDrag(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragged = false;
        Debug.Log("endDrag");
        TileBoard.instance.oneTileIsDragged = false;
        myCollider.isTrigger = false;
        GetComponent<RectTransform>().sizeDelta = new Vector2(88, 88);

        // Remet l'autre joint tile dans son parent normal avant de continuer
        if (isJoint && jointParent != null)
        {
            RestoreJointTileParent();
        }

        if (isMerging || TileBoard.instance.isMerging || TileBoard.instance.waiting)
        {
            eventData.pointerDrag = null;
            rectTransform.anchoredPosition = originalPosition;
            return;
        }

        TileCell nearestCell = FindNearestCell();

        if (nearestCell != null)
        {
            if (nearestCell.tile == null)
            {
                //Debug.Log("se mttre en place");
                MoveTo(nearestCell);
                // Trouve une cellule pour l'autre joint tile aussi
                MoveOtherJointTileToNearestCell();
            }
            else if (nearestCell.tile != this && TileBoard.instance.CanMerge(this, nearestCell.tile))
            {
                TileBoard.instance.Merge(this, nearestCell.tile);
            }
            else
            {
                rectTransform.anchoredPosition = originalPosition;
                RestoreOtherJointTilePosition();
            }
        }
        else
        {
            rectTransform.anchoredPosition = originalPosition;
            RestoreOtherJointTilePosition();
        }
    }

    // Remet l'autre joint tile dans son parent normal
    private void RestoreJointTileParent()
    {
        Tile otherTile = GetOtherJointTile();
        if (otherTile != null)
        {
            // Remets l'autre tile dans son parent habituel
            // Vous devrez adapter ceci selon votre structure de parents
            Transform originalParent = TileGrid.instance.transform; // ou le parent approprié
            otherTile.transform.SetParent(originalParent);
        }
    }

    // Trouve une cellule proche pour l'autre joint tile
    private void MoveOtherJointTileToNearestCell()
    {
        Tile otherTile = GetOtherJointTile();
        if (otherTile != null)
        {
            TileCell nearestCell = otherTile.FindNearestCell();
            if (nearestCell != null && nearestCell.tile == null)
            {
                otherTile.MoveTo(nearestCell);
            }
            else
            {
                // Si pas de cellule libre, remet à la position originale
                otherTile.rectTransform.anchoredPosition = otherTile.originalPosition;
            }
        }
    }

    // Restaure la position originale de l'autre joint tile
    private void RestoreOtherJointTilePosition()
    {
        Tile otherTile = GetOtherJointTile();
        if (otherTile != null)
        {
            otherTile.rectTransform.anchoredPosition = otherTile.originalPosition;
        }
    }
    /*public void OnDrag(PointerEventData eventData)
    {
        if (isMerging || TileBoard.instance.waiting)
        {
            eventData.pointerDrag = null;
            return;
        }

        isDragged = true;
        transform.SetAsLastSibling();
        

        // Calcule le déplacement souhaité
        Vector2 deltaMove = eventData.delta / canvas.scaleFactor;
        Vector2 currentPos = rectTransform.anchoredPosition;
        Vector2 targetPos = currentPos + deltaMove;

        // Découpe le mouvement en petites étapes pour éviter le tunneling
        Vector2 finalPos = TestMovementWithSteps(currentPos, targetPos);

        rectTransform.anchoredPosition = finalPos;
    }*/

    private Vector2 TestMovementWithSteps(Vector2 currentPos, Vector2 targetPos)
    {
        Vector2 movement = targetPos - currentPos;
        float distance = movement.magnitude;

        // Si le mouvement est petit, teste directement
        if (distance <= 5f) // Ajustez cette valeur selon vos besoins
        {
            return TestMovement(currentPos, targetPos);
        }

        // Sinon, découpe en étapes
        int steps = Mathf.CeilToInt(distance / 5f); // 5 pixels par étape maximum
        Vector2 stepVector = movement / steps;
        Vector2 currentTestPos = currentPos;

        for (int i = 0; i < steps; i++)
        {
            Vector2 nextPos = currentTestPos + stepVector;

            // Teste le mouvement pour cette étape
            Vector2 validPos = TestMovement(currentTestPos, nextPos);

            // Si on ne peut pas bouger du tout, on s'arrête ici
            if (validPos == currentTestPos)
            {
                break;
            }

            currentTestPos = validPos;

            // Si on n'a pas pu faire le mouvement complet, on s'arrête
            if (validPos != nextPos)
            {
                break;
            }
        }

        return currentTestPos;
    }

    private Vector2 TestMovement(Vector2 currentPos, Vector2 targetPos)
    {
        Vector2 finalPos = currentPos;

        // Teste d'abord le mouvement horizontal
        Vector2 horizontalPos = new Vector2(targetPos.x, currentPos.y);
        if (IsPositionValid(horizontalPos))
        {
            finalPos.x = horizontalPos.x;
        }

        // Puis teste le mouvement vertical
        Vector2 verticalPos = new Vector2(finalPos.x, targetPos.y);
        if (IsPositionValid(verticalPos))
        {
            finalPos.y = verticalPos.y;
        }

        return finalPos;
    }

    private bool IsPositionValid(Vector2 testPosition)
    {
        // Sauvegarde la position actuelle
        Vector2 originalPos = rectTransform.anchoredPosition;

        // Bouge temporairement à la position test
        rectTransform.anchoredPosition = testPosition;

        // Vérifie les collisions
        bool hasCollision = CheckCollisionAtCurrentPosition();

        // Restaure la position originale
        rectTransform.anchoredPosition = originalPos;

        return !hasCollision;
    }

    private bool CheckCollisionAtCurrentPosition()
    {
        Vector2 worldSize = new Vector2(
            rectTransform.rect.width * rectTransform.lossyScale.x,
            rectTransform.rect.height * rectTransform.lossyScale.y
        );

        // Utilise un contact filter pour plus de contrôle
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask("Tiles", "BoardBorders"));
        filter.useLayerMask = true;
        filter.useTriggers = false;

        Collider2D[] results = new Collider2D[10];
        int count = Physics2D.OverlapBox(
            rectTransform.position,
            worldSize * 0.75f, // Légèrement plus petit pour éviter les faux positifs
            0f,
            filter,
            results
        );

        for (int i = 0; i < count; i++)
        {
            Collider2D col = results[i];
            if (col.gameObject != this.gameObject)
            {
                if (col.CompareTag("Tile"))
                {
                    Tile otherTile = col.GetComponent<Tile>();
                    // Si on peut merger, on autorise le mouvement pour permettre la fusion
                    if (otherTile != null && TileBoard.instance.CanMerge(this, otherTile))
                    {
                        TileBoard.instance.Merge(this, otherTile);
                        return false; // Pas de collision, on autorise le mouvement vers la tuile mergeable
                    }
                    return true; // Collision avec une tuile non-mergeable
                }
                else if (col.CompareTag("BoardBorder"))
                {
                    return true; // Collision avec la bordure
                }
            }
        }

        return false; // Pas de collision
    }

    /*public void OnEndDrag(PointerEventData eventData)
    {
        isDragged = false;
        TileBoard.instance.oneTileIsDragged = false;
        myCollider.isTrigger = false;
        GetComponent<RectTransform>().sizeDelta = new Vector2(88, 88);
        //rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;

        if (isMerging || TileBoard.instance.isMerging || TileBoard.instance.waiting)
        {
            eventData.pointerDrag = null;
            //ReturnToCell();
            rectTransform.anchoredPosition = originalPosition;
            return;
        }

        TileCell nearestCell =  FindNearestCell();

        if (nearestCell != null)
        {
            // Empêche la fusion avec soi-même
            if (nearestCell.tile == null)
            {
                MoveTo(nearestCell);
            }
            else if (nearestCell.tile != this && TileBoard.instance.CanMerge(this, nearestCell.tile))
            {
                TileBoard.instance.Merge(this, nearestCell.tile);
            }
            else
            {
                //ReturnToCell();
                rectTransform.anchoredPosition = originalPosition;
            }
        }
        else
        {
            //ReturnToCell();
            rectTransform.anchoredPosition = originalPosition;
        }
    }

    private void ReturnToCell()
    {
        if (cell != null)
        {
            StartCoroutine(Animate(cell.transform.position));
        }
        else
            Debug.LogError("No Cell To Go");
    }

    

    /*private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isDragged) return;

        if (collision.CompareTag("Tile"))
        {
            Tile otherTile = collision.GetComponent<Tile>();
            if (otherTile != this && TileBoard.instance.CanMerge(this, otherTile))
            {
                // Snap à la position de l'autre tuile pour une fusion visuelle
                rectTransform.anchoredPosition = otherTile.rectTransform.anchoredPosition;
                TileBoard.instance.Merge(this, otherTile);
            }
            else if (otherTile != this)
            {
                // Empêche la superposition en maintenant une distance minimale
                /*Vector2 dir = (rectTransform.anchoredPosition - otherTile.rectTransform.anchoredPosition).normalized;
                rectTransform.anchoredPosition = otherTile.rectTransform.anchoredPosition + dir * 20f;
                isColliding = true;
                //rectTransform.anchoredPosition = originalPos;
            }
        }
        else if (collision.CompareTag("BoardBorder"))
            isColliding = true;
    }*/

    /*private void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject == this.gameObject || !isDragged) return;

        if (other.CompareTag("Tile") || other.CompareTag("BoardBorder"))
        {
            Vector2 dir = (Vector2)transform.position - (Vector2)other.transform.position;
            dir.Normalize();

            // Distance minimale pour ne pas se coller
            float pushAmount = 0.01f;

            // Repousse légèrement la tuile dans la direction opposée
            transform.position += (Vector3)(dir * pushAmount);
        }
    }*/


    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Tile") || collision.CompareTag("BoardBorder"))
        {
            isColliding = false;
        }

        if (collision.CompareTag("Cell") && collision.gameObject.GetComponent<TileCell>() == this.cell)
        {
            // OnTriggerExit2D se déclenche déjà automatiquement quand 
            // les colliders ne se touchent plus (même aux edges)
            //Debug.Log("Sortie complète !");
            TileCell cellAbove = TileGrid.instance.GetCellUp(cell);
            ClearCell();
            if (cellAbove.tile != null)
                cellAbove.tile.CheckCellDown();
        }
    }
    public void Merge(TileCell cell)
    {
        isMerging = true;
        if (isDragged)
            TileBoard.instance.oneTileIsDragged = false;
        isDragged = false;
        if (this.cell != null)
        {
            this.cell.tile = null;
        }
        this.cell = null;

        // Désactive le collider pendant le merge
        var collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;

        StartCoroutine(Animate(cell.transform.position, true));
    }

    private TileCell FindNearestCell()
    {
        float minDistance = float.MaxValue;
        TileCell nearestCell = null;

        Vector3 tileWorldPos = rectTransform.position; // position dans le monde

        foreach (TileCell cell in TileGrid.instance.cells)
        {
            RectTransform cellRect = cell.GetComponent<RectTransform>();
            Vector3 cellWorldPos = cellRect.position;

            float distance = Vector3.Distance(tileWorldPos, cellWorldPos);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestCell = cell;
            }
        }

        return minDistance < 100f ? nearestCell : null; // seuil à ajuster
    }

    private IEnumerator Animate(Vector3 to, bool merging = false)
    {
        float elapsed = 0f;
        float duration = .1f;

        Vector3 from = transform.position;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(from, to, elapsed/duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = to;
        TileBoard.instance.CascadeTilesDown();

        if (merging)
        {
            // Réactive le collider avant destruction si besoin
            var collider = GetComponent<Collider2D>();
            if (collider != null) collider.enabled = true;
            Destroy(gameObject);
        }
    }

    /*void OnDrawGizmos()
    {
        if (rectTransform == null) return;

        // Calcul de la taille réelle
        Vector2 worldSize = new Vector2(
            rectTransform.rect.width * rectTransform.lossyScale.x,
            rectTransform.rect.height * rectTransform.lossyScale.y
        ) * .75f
        ;

        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.matrix = Matrix4x4.TRS(
            rectTransform.position,
            Quaternion.Euler(0, 0, rectTransform.eulerAngles.z),
            Vector3.one
        );

        Gizmos.DrawCube(Vector3.zero, worldSize);
        Gizmos.DrawWireCube(Vector3.zero, worldSize);
    }*/


}


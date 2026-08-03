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
        if (tileCellDown != null)
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
        

        /*Vector3 worldPoint = Camera.main.ScreenToWorldPoint(eventData.position);
       offset = transform.position - new Vector3(worldPoint.x, worldPoint.y, transform.position.z);/
    }*/

    // Modifiez votre classe Tile pour g�rer les collisions des joint tiles

    public void OnDrag(PointerEventData eventData)
    {
        if (isMerging)
        {
            eventData.pointerDrag = null;
            return;
        }

        isDragged = true;
        transform.SetAsLastSibling();

        Vector2 deltaMove = eventData.delta / canvas.scaleFactor;
        Vector2 currentPos = rectTransform.anchoredPosition;
        Vector2 targetPos = currentPos + deltaMove;

        Tile mergeableTile = null;
        Vector2 finalPos = TestMovementWithSteps(currentPos, targetPos, out mergeableTile);
        rectTransform.anchoredPosition = finalPos;

        if (mergeableTile != null)
        {
            TileBoard.instance.Merge(this, mergeableTile);
            eventData.pointerDrag = null;
        }
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (TileBoard.instance.waiting || isMerging)
        {
            eventData.pointerDrag = null; // Annule le drag
            return;
        }

        originalPosition = rectTransform.anchoredPosition;
        TileBoard.instance.oneTileIsDragged = true;
        myCollider.isTrigger = true;
        GetComponent<RectTransform>().sizeDelta = new Vector2(93, 93);
    }

    // M�thode pour trouver l'index de cette tile dans le joint
    

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("OnPointerUp appel� - isDragged: " + isDragged);

        // Si on �tait en train de draguer mais que OnEndDrag n'a pas �t� appel�
        if (isDragged)
        {
            Debug.LogWarning("OnEndDrag manqu� ! OnPointerUp prend le relais");
            OnEndDrag(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragged = false;
        TileBoard.instance.oneTileIsDragged = false;
        myCollider.isTrigger = false;
        GetComponent<RectTransform>().sizeDelta = new Vector2(88, 88);

        if (isMerging || TileBoard.instance.isMerging)
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
                MoveTo(nearestCell);
            }
            else if (nearestCell.tile != this && TileBoard.instance.CanMerge(this, nearestCell.tile))
            {
                TileBoard.instance.Merge(this, nearestCell.tile);
            }
            else
            {
                rectTransform.anchoredPosition = originalPosition;
            }
        }
        else
        {
            rectTransform.anchoredPosition = originalPosition;
        }
    }

    // Remet l'autre joint tile dans son parent normal
    private Vector2 TestMovementWithSteps(Vector2 currentPos, Vector2 targetPos, out Tile mergeableTile)
    {
        Vector2 movement = targetPos - currentPos;
        float distance = movement.magnitude;
        mergeableTile = null;

        if (distance <= 5f)
        {
            return TestMovement(currentPos, targetPos, out mergeableTile);
        }

        int steps = Mathf.CeilToInt(distance / 5f);
        Vector2 stepVector = movement / steps;
        Vector2 currentTestPos = currentPos;

        for (int i = 0; i < steps; i++)
        {
            Vector2 nextPos = currentTestPos + stepVector;
            Vector2 validPos = TestMovement(currentTestPos, nextPos, out mergeableTile);

            if (validPos == currentTestPos) break;
            currentTestPos = validPos;
            
            if (mergeableTile != null) break;
            if (validPos != nextPos) break;
        }

        return currentTestPos;
    }
    private Vector2 TestMovement(Vector2 currentPos, Vector2 targetPos, out Tile mergeableTile)
    {
        Vector2 finalPos = currentPos;
        mergeableTile = null;

        Vector2 horizontalPos = new Vector2(targetPos.x, currentPos.y);
        Tile hTile = null;
        if (IsPositionValid(horizontalPos, out hTile))
        {
            finalPos.x = horizontalPos.x;
            if (hTile != null) mergeableTile = hTile;
        }

        Vector2 verticalPos = new Vector2(finalPos.x, targetPos.y);
        Tile vTile = null;
        if (IsPositionValid(verticalPos, out vTile))
        {
            finalPos.y = verticalPos.y;
            if (vTile != null) mergeableTile = vTile;
        }

        return finalPos;
    }
    private bool IsPositionValid(Vector2 testPosition, out Tile mergeableTile)
    {
        Vector2 originalPos = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = testPosition;
        bool hasCollision = CheckCollisionAtCurrentPosition(out mergeableTile);
        rectTransform.anchoredPosition = originalPos;
        return !hasCollision;
    }
    private bool CheckCollisionAtCurrentPosition(out Tile mergeableTile)
    {
        mergeableTile = null;
        Vector2 worldSize = new Vector2(
            rectTransform.rect.width * rectTransform.lossyScale.x,
            rectTransform.rect.height * rectTransform.lossyScale.y
        );

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask("Tiles", "BoardBorders"));
        filter.useLayerMask = true;
        filter.useTriggers = false;

        Collider2D[] results = new Collider2D[10];
        int count = Physics2D.OverlapBox(
            rectTransform.position,
            worldSize * 0.75f,
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
                    if (otherTile != null && TileBoard.instance.CanMerge(this, otherTile))
                    {
                        mergeableTile = otherTile;
                        return false;
                    }
                    return true;
                }
                else if (col.CompareTag("BoardBorder"))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /*public void OnEndDrag(PointerEventData eventData)
    {
        isDragged = false;
        TileBoard.instance.oneTileIsDragged = false;
        myCollider.isTrigger = false;
        GetComponent<RectTransform>().sizeDelta = new Vector2(88, 88);
        //rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;

        if (isMerging || TileBoard.instance.isMerging)
        {
            eventData.pointerDrag = null;
            //ReturnToCell();
            rectTransform.anchoredPosition = originalPosition;
            return;
        }

        TileCell nearestCell =  FindNearestCell();

        if (nearestCell != null)
        {
            // Emp�che la fusion avec soi-m�me
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
                // Snap � la position de l'autre tuile pour une fusion visuelle
                rectTransform.anchoredPosition = otherTile.rectTransform.anchoredPosition;
                TileBoard.instance.Merge(this, otherTile);
            }
            else if (otherTile != this)
            {
                // Emp�che la superposition en maintenant une distance minimale
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

            // Repousse l�g�rement la tuile dans la direction oppos�e
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
            // OnTriggerExit2D se d�clenche d�j� automatiquement quand 
            // les colliders ne se touchent plus (m�me aux edges)
            //Debug.Log("Sortie compl�te !");
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

        // D�sactive le collider pendant le merge
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

        return minDistance < 100f ? nearestCell : null; // seuil � ajuster
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
            // R�active le collider avant destruction si besoin
            var collider = GetComponent<Collider2D>();
            if (collider != null) collider.enabled = true;
            Destroy(gameObject);
        }
    }

    /*void OnDrawGizmos()
    {
        if (rectTransform == null) return;

        // Calcul de la taille r�elle
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



$lines = Get-Content D:\Unity\Projects\CrazyMerge\Assets\Scripts\Tile.cs
$newLines = @()

$onDragContent = @"
    public void OnDrag(PointerEventData eventData)
    {
        if (isMerging || TileBoard.instance.waiting)
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
"@ -split "`r?`n"

$testMovStepsContent = @"
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
"@ -split "`r?`n"

$testMovContent = @"
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
"@ -split "`r?`n"

$isPosValidContent = @"
    private bool IsPositionValid(Vector2 testPosition, out Tile mergeableTile)
    {
        Vector2 originalPos = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = testPosition;
        bool hasCollision = CheckCollisionAtCurrentPosition(out mergeableTile);
        rectTransform.anchoredPosition = originalPos;
        return !hasCollision;
    }
"@ -split "`r?`n"

$checkCollisionContent = @"
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
"@ -split "`r?`n"

for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($i -eq 290) {
        $newLines += $onDragContent
    } elseif ($i -gt 290 -and $i -le 304) {
        # skip OnDrag old
    } elseif ($i -eq 595) {
        $newLines += $testMovStepsContent
    } elseif ($i -gt 595 -and $i -le 635) {
        # skip TestMovementWithSteps old
    } elseif ($i -eq 636) {
        $newLines += $testMovContent
    } elseif ($i -gt 636 -and $i -le 656) {
        # skip TestMovement old
    } elseif ($i -eq 657) {
        $newLines += $isPosValidContent
    } elseif ($i -gt 657 -and $i -le 673) {
        # skip IsPositionValid old
    } elseif ($i -eq 674) {
        $newLines += $checkCollisionContent
    } elseif ($i -gt 674 -and $i -le 719) {
        # skip CheckCollisionAtCurrentPosition old
    } else {
        $newLines += $lines[$i]
    }
}
Set-Content "D:\Unity\Projects\CrazyMerge\Assets\Scripts\Tile.cs" $newLines

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
        rectTransform.anchoredPosition += deltaMove;
    }
"@ -split "`r?`n"

$onBeginDragContent = @"
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
"@ -split "`r?`n"

$onEndDragContent = @"
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragged = false;
        TileBoard.instance.oneTileIsDragged = false;
        myCollider.isTrigger = false;
        GetComponent<RectTransform>().sizeDelta = new Vector2(88, 88);

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
"@ -split "`r?`n"

for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($i -eq 290) {
        $newLines += $onDragContent
    } elseif ($i -gt 290 -and $i -le 328) {
        # skip
    } elseif ($i -eq 478) {
        $newLines += $onBeginDragContent
    } elseif ($i -gt 478 -and $i -le 512) {
        # skip
    } elseif ($i -eq 541) {
        $newLines += $onEndDragContent
    } elseif ($i -gt 541 -and $i -le 588) {
        # skip
    } else {
        $newLines += $lines[$i]
    }
}
Set-Content "D:\Unity\Projects\CrazyMerge\Assets\Scripts\Tile.cs" $newLines

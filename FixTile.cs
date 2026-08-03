using System;
using System.IO;
using System.Text.RegularExpressions;

class Program {
    static void Main() {
        string path = @"D:\Unity\Projects\CrazyMerge\Assets\Scripts\Tile.cs";
        string content = File.ReadAllText(path);

        // Replace OnDrag
        string onDragPattern = @"public void OnDrag\(PointerEventData eventData\)[\s\S]*?^\s*\}";
        string newOnDrag = @"public void OnDrag(PointerEventData eventData)
    {
        if (isMerging || TileBoard.instance.waiting)
        {
            eventData.pointerDrag = null;
            return;
        }

        isDragged = true;
        transform.SetAsLastSibling();

        // Fluid drag - no physics checks during drag
        Vector2 deltaMove = eventData.delta / canvas.scaleFactor;
        rectTransform.anchoredPosition += deltaMove;
    }";
        content = Regex.Replace(content, onDragPattern, newOnDrag, RegexOptions.Multiline);

        // Replace OnBeginDrag
        string onBeginDragPattern = @"public void OnBeginDrag\(PointerEventData eventData\)[\s\S]*?^\s*\}";
        string newOnBeginDrag = @"public void OnBeginDrag(PointerEventData eventData)
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
    }";
        content = Regex.Replace(content, onBeginDragPattern, newOnBeginDrag, RegexOptions.Multiline);

        // Replace OnEndDrag
        string onEndDragPattern = @"public void OnEndDrag\(PointerEventData eventData\)[\s\S]*?^\s*\}";
        string newOnEndDrag = @"public void OnEndDrag(PointerEventData eventData)
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
    }";
        content = Regex.Replace(content, onEndDragPattern, newOnEndDrag, RegexOptions.Multiline);

        File.WriteAllText(path, content);
    }
}

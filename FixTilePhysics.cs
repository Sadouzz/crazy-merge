using System;
using System.IO;
using System.Text.RegularExpressions;

class Program {
    static void Main() {
        string path = @"D:\Unity\Projects\CrazyMerge\Assets\Scripts\Tile.cs";
        string content = File.ReadAllText(path);

        // Replace CheckCollisionAtCurrentPosition and IsPositionValid
        string checkColPattern = @"private bool CheckCollisionAtCurrentPosition\(\)[\s\S]*?return false;\s*\}";
        string newCheckCol = @"private bool CheckCollisionAtCurrentPosition(out Tile mergeableTile)
    {
        mergeableTile = null;
        Vector2 worldSize = new Vector2(
            rectTransform.rect.width * rectTransform.lossyScale.x,
            rectTransform.rect.height * rectTransform.lossyScale.y
        );

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask(""Tiles"", ""BoardBorders""));
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
                if (col.CompareTag(""Tile""))
                {
                    Tile otherTile = col.GetComponent<Tile>();
                    if (otherTile != null && TileBoard.instance.CanMerge(this, otherTile))
                    {
                        mergeableTile = otherTile;
                        return false; // Autorise le mouvement pour la fusion
                    }
                    return true; // Collision avec une tuile non-mergeable
                }
                else if (col.CompareTag(""BoardBorder""))
                {
                    return true; // Collision avec la bordure
                }
            }
        }

        return false;
    }";
        content = Regex.Replace(content, checkColPattern, newCheckCol);

        string isPosPattern = @"private bool IsPositionValid\(Vector2 testPosition\)[\s\S]*?return !hasCollision;\s*\}";
        string newIsPos = @"private bool IsPositionValid(Vector2 testPosition, out Tile mergeableTile)
    {
        Vector2 originalPos = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = testPosition;
        bool hasCollision = CheckCollisionAtCurrentPosition(out mergeableTile);
        rectTransform.anchoredPosition = originalPos;
        return !hasCollision;
    }";
        content = Regex.Replace(content, isPosPattern, newIsPos);

        // Replace TestMovement
        string testMovPattern = @"private Vector2 TestMovement\(Vector2 currentPos, Vector2 targetPos\)[\s\S]*?return finalPos;\s*\}";
        string newTestMov = @"private Vector2 TestMovement(Vector2 currentPos, Vector2 targetPos, out Tile mergeableTile)
    {
        Vector2 finalPos = currentPos;
        mergeableTile = null;

        Vector2 horizontalPos = new Vector2(targetPos.x, currentPos.y);
        Tile hTile;
        if (IsPositionValid(horizontalPos, out hTile))
        {
            finalPos.x = horizontalPos.x;
            if (hTile != null) mergeableTile = hTile;
        }

        Vector2 verticalPos = new Vector2(finalPos.x, targetPos.y);
        Tile vTile;
        if (IsPositionValid(verticalPos, out vTile))
        {
            finalPos.y = verticalPos.y;
            if (vTile != null) mergeableTile = vTile;
        }

        return finalPos;
    }";
        content = Regex.Replace(content, testMovPattern, newTestMov);

        // Replace TestMovementWithSteps
        string testMovStepsPattern = @"private Vector2 TestMovementWithSteps\(Vector2 currentPos, Vector2 targetPos\)[\s\S]*?return currentTestPos;\s*\}";
        string newTestMovSteps = @"private Vector2 TestMovementWithSteps(Vector2 currentPos, Vector2 targetPos, out Tile mergeableTile)
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
            if (mergeableTile != null) break; // Si on touche une tuile mergeable, on s'arrte pour merger
            if (validPos != nextPos) break;
        }

        return currentTestPos;
    }";
        content = Regex.Replace(content, testMovStepsPattern, newTestMovSteps);

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

        Vector2 deltaMove = eventData.delta / canvas.scaleFactor;
        Vector2 currentPos = rectTransform.anchoredPosition;
        Vector2 targetPos = currentPos + deltaMove;

        Tile mergeableTile;
        Vector2 finalPos = TestMovementWithSteps(currentPos, targetPos, out mergeableTile);
        
        rectTransform.anchoredPosition = finalPos;

        if (mergeableTile != null)
        {
            // Execute le merge instantanment
            TileBoard.instance.Merge(this, mergeableTile);
            eventData.pointerDrag = null; // Stoppe le drag
        }
    }";
        content = Regex.Replace(content, onDragPattern, newOnDrag, RegexOptions.Multiline);

        File.WriteAllText(path, content);
    }
}

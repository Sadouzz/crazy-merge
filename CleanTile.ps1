$content = [System.IO.File]::ReadAllText("D:\Unity\Projects\CrazyMerge\Assets\Scripts\Tile.cs")

# Remove tile connections variables
$content = [Regex]::Replace($content, "(?s)public bool isJoint;.*?public void OnMerge\(\)\s*\{.*?\}", "")

# Fix CheckCellDown
$content = $content.Replace("if (tileCellDown != null && !isJoint)", "if (tileCellDown != null)")

# Remove TestMovementWithJointCollisions and all joint methods up to OnBeginDrag
$content = [Regex]::Replace($content, "(?s)// Nouvelle m[^\n]*?thode pour tester les mouvements.*?public void OnBeginDrag\(PointerEventData eventData\)", "public void OnBeginDrag(PointerEventData eventData)")

# Remove GetMyJointIndex
$content = [Regex]::Replace($content, "(?s)private int GetMyJointIndex\(\).*?return -1;\s*\}", "")

# Remove RestoreJointTileParent and subsequent methods
$content = [Regex]::Replace($content, "(?s)private void RestoreJointTileParent\(\).*?private Vector2 TestMovementWithSteps", "private Vector2 TestMovementWithSteps")

# Remove OnBeginDrag joint code
$content = [Regex]::Replace($content, "(?s)if \(isJoint\)\s*\{\s*jointParent\.ParentTile\(this, jointParent\.jointTiles\[1\]\);\s*\}", "")

[System.IO.File]::WriteAllText("D:\Unity\Projects\CrazyMerge\Assets\Scripts\Tile.cs", $content)

using System;
using System.IO;
using System.Text.RegularExpressions;

class Program {
    static void Main() {
        string path = @"D:\Unity\Projects\CrazyMerge\Assets\Scripts\Tile.cs";
        string content = File.ReadAllText(path);

        // 1. Remove tile connections variables
        string varPattern = @"public bool isJoint;[\s\S]*?public void OnMerge\(\)\s*\{[\s\S]*?\}";
        content = Regex.Replace(content, varPattern, "");

        // 2. Fix CheckCellDown
        content = content.Replace("if (tileCellDown != null && !isJoint)", "if (tileCellDown != null)");

        // 3. Remove TestMovementWithJointCollisions and all joint methods up to OnBeginDrag
        string jointMethodsPattern = @"// Nouvelle méthode pour tester les mouvements avec les joint tiles[\s\S]*?public void OnBeginDrag\(PointerEventData eventData\)";
        content = Regex.Replace(content, jointMethodsPattern, "public void OnBeginDrag(PointerEventData eventData)");
        
        string jointMethodsPattern2 = @"// Nouvelle m\?thode pour tester les mouvements avec les joint tiles[\s\S]*?public void OnBeginDrag\(PointerEventData eventData\)";
        content = Regex.Replace(content, jointMethodsPattern2, "public void OnBeginDrag(PointerEventData eventData)");

        // 4. Remove GetMyJointIndex
        string getJointPattern = @"private int GetMyJointIndex\(\)[\s\S]*?return -1;\s*\}";
        content = Regex.Replace(content, getJointPattern, "");

        // 5. Remove RestoreJointTileParent and subsequent methods
        string restorePattern = @"private void RestoreJointTileParent\(\)[\s\S]*?private Vector2 TestMovementWithSteps";
        content = Regex.Replace(content, restorePattern, "private Vector2 TestMovementWithSteps");

        // Remove OnBeginDrag joint code
        string onBeginDragPattern = @"if \(isJoint\)\s*\{\s*jointParent\.ParentTile\(this, jointParent\.jointTiles\[1\]\);\s*\}";
        content = Regex.Replace(content, onBeginDragPattern, "");

        // Save
        File.WriteAllText(path, content);
    }
}

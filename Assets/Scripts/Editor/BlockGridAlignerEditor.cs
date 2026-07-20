using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BlockGridAligner))]
public class BlockGridAlignerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BlockGridAligner aligner = (BlockGridAligner)target;
        if (GUILayout.Button("Align Blocks To Grid"))
        {
            aligner.AlignAllBlocks();
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace kuro
{
    [CustomPropertyDrawer(typeof(DynamicSprite))]
    public class DynamicSpriteInspector : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var propertyName = property.FindPropertyRelative("_id.Name");

            var obj = EditorAtlasManager.GetEditorSpriteData(new(propertyName.stringValue));
            var newObject = EditorGUI.ObjectField(position, label, obj, typeof(EditorSpriteData), false) as EditorSpriteData;
            if (newObject != obj)
            {
                SpriteId newId;
                if (newObject && newObject.SpriteData != null)
                    newId = newObject.SpriteData.Id;
                else
                    newId = default;
                propertyName.stringValue = newId.Name ?? "";
                propertyName.serializedObject.ApplyModifiedProperties();
                GUI.changed = true;
            }
        }
    }
}
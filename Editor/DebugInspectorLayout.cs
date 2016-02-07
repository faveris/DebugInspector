using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

public class DebugInspectorLayout
{
    //
    // Fields
    //

    private static DebugInspectorLayout s_instance = null;

    private IDictionary<object, FoldoutNode> m_foldouts = new Dictionary<object, FoldoutNode>();
    private Stack<FoldoutIndex> m_openedFoldouts = new Stack<FoldoutIndex>();

    //
    // Static interface
    //

    public static void DrawDebugView(Component _component)
    {
        if (Application.isPlaying &&
            !EditorUtility.IsPersistent(_component))
        {
            ObjectField("Debug view", _component);
        }
    }

    public static object ObjectField(string _label, object _value, Texture _icon = null)
    {
        if (_value == null)
        {
            throw new ArgumentNullException("_value");
        }

        return GetInstance().ObjectField(_label, _value.GetType(), _value, _icon);
    }

    //
    // Service
    //

    private static DebugInspectorLayout GetInstance()
    {
        if (s_instance == null)
        {
            s_instance = new DebugInspectorLayout();
        }

        return s_instance;
    }

    private bool BeginFoldout(object _object, bool _isOpenedByDefault, string _label, Texture _icon = null)
    {
        bool wasOpened = _isOpenedByDefault;
        FoldoutNode node = null;

        if (m_openedFoldouts.Count > 0)
        {
            node = m_openedFoldouts.Peek().MoveNext();
        }
        else
        {
            m_foldouts.TryGetValue(_object, out node);
        }

        if (node != null)
        {
            wasOpened = node.isOpened;
        }

        bool isOpened = EditorGUILayout.Foldout(wasOpened, new GUIContent(_label, _icon));
        if (isOpened)
        {
            if (node == null)
            {
                node = new FoldoutNode();

                if (m_openedFoldouts.Count > 0)
                {
                    m_openedFoldouts.Peek().Add(node);
                }
                else
                {
                    m_foldouts.Add(_object, node);
                }
            }

            m_openedFoldouts.Push(new FoldoutIndex(node));
            EditorGUI.indentLevel++;
        }

        if (node != null)
        {
            node.isOpened = isOpened;
        }

        return isOpened;
    }

    private void EndFoldout()
    {
        EditorGUI.indentLevel--;
        m_openedFoldouts.Pop();
    }

    private object FieldField(string _label, Type _type, object _value)
    {
        if (_type == typeof(byte))
        {
            return (byte)Math.Min(Byte.MaxValue, Math.Max(Byte.MinValue, EditorGUILayout.IntField(_label, Convert.ToByte(_value))));
        }
        else if (_type == typeof(short))
        {
            return (short)Math.Min(Int16.MaxValue, Math.Max(Int16.MinValue, EditorGUILayout.IntField(_label, Convert.ToInt16(_value))));
        }
        if (_type == typeof(int))
        {
            return EditorGUILayout.IntField(_label, Convert.ToInt32(_value));
        }
        else if (_type == typeof(long))
        {
            return EditorGUILayout.LongField(_label, Convert.ToInt64(_value));
        }
        else if (_type == typeof(float))
        {
            return EditorGUILayout.FloatField(_label, Convert.ToSingle(_value));
        }
        else if (_type == typeof(double))
        {
            return EditorGUILayout.DoubleField(_label, Convert.ToDouble(_value));
        }
        else if (_type == typeof(bool))
        {
            return EditorGUILayout.Toggle(_label, Convert.ToBoolean(_value));
        }
        else if (_type == typeof(string))
        {
            return EditorGUILayout.DelayedTextField(_label, _value as string);
        }
        else if (_type.IsEnum)
        {
            return EditorGUILayout.EnumPopup(_label, _value as Enum);
        }
        else if (_type == typeof(Vector2))
        {
            return EditorGUILayout.Vector2Field(_label, (Vector2)_value);
        }
        else if (_type == typeof(Vector3))
        {
            return EditorGUILayout.Vector3Field(_label, (Vector3)_value);
        }
        else if (_type == typeof(Vector4))
        {
            return EditorGUILayout.Vector4Field(_label, (Vector4)_value);
        }
        else if (_type == typeof(Color))
        {
            return EditorGUILayout.ColorField(_label, (Color)_value);
        }
        else if (_type.IsSubclassOf(typeof(UnityEngine.Object)))
        {
            return EditorGUILayout.ObjectField(_label, _value as UnityEngine.Object, _type, true);
        }
        else if (typeof(IList).IsAssignableFrom(_type) ||
                 GetListGenericArgument(_type) != null)
        {
            return ListField(_label, _type, _value as IList);
        }
        else if (_type.IsClass)
        {
            return ObjectField(_label + " (" + _type.Name + ")", _type, _value);
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(_label);
        EditorGUILayout.LabelField("Unsupported type (" + _type.Name + ")");
        EditorGUILayout.EndHorizontal();
        return _value;
    }

    private object NullField(string _label, Type _type, object _value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(_label);
        EditorGUILayout.LabelField("null (" + _type.Name + ")");
        EditorGUILayout.EndHorizontal();

        // $TODO: Make possible to create object

        return _value;
    }

    private object ObjectField(string _label, Type _type, object _value, Texture _icon = null)
    {
        if (_value == null)
        {
            return NullField(_label, _type, _value);
        }

        if (BeginFoldout(_value, false, _label, _icon))
        {
            try
            {
                Type type = _value.GetType();
                List<FieldInfo> fields = new List<FieldInfo>(type.GetFields(BindingFlags.Public | BindingFlags.Instance));

                while (type != null)
                {
                    fields.AddRange(type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance));
                    type = type.BaseType;
                }

                foreach (FieldInfo field in fields)
                {
                    if (!field.IsPublic ||
                        field.GetCustomAttributes(typeof(HideInInspector), true).Length > 0)
                    {
                        field.SetValue(_value, FieldField(field.Name, field.FieldType, field.GetValue(_value)));
                    }
                }
            }
            finally
            {
                EndFoldout();
            }
        }

        return _value;
    }

    private IList ListField(string _label, Type _type, IList _value)
    {
        if (_value == null)
        {
            return NullField(_label, _type, _value) as IList;
        }

        Type listArgument = GetListGenericArgument(_type);
        if (listArgument != null &&
            BeginFoldout(_value, false, _label))
        {
            try
            {
                if (_value.IsFixedSize)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Size");
                    EditorGUILayout.LabelField(_value.Count.ToString());
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    int newCount = EditorGUILayout.DelayedIntField("Size", _value.Count);

                    for (int i = _value.Count - 1; i >= newCount; i--)
                    {
                        _value.RemoveAt(i);
                    }

                    for (int i = _value.Count; i < newCount; i++)
                    {
                        object newItem = listArgument.IsValueType ? Activator.CreateInstance(listArgument) : null;
                        _value.Add(newItem);
                    }
                }

                for (int i = 0; i < _value.Count; i++)
                {
                    object newValue = FieldField("Element " + i, listArgument, _value[i]);
                    if (!_value.IsReadOnly)
                    {
                        _value[i] = newValue;
                    }
                }
            }
            finally
            {
                EndFoldout();
            }
        }

        return _value;
    }

    private Type GetListGenericArgument(Type _type)
    {
        if (_type.IsGenericType &&
            _type.GetGenericTypeDefinition() == typeof(IList<>))
        {
            return _type.GetGenericArguments()[0];
        }

        foreach (Type interfaceType in _type.GetInterfaces())
        {
            Type type = GetListGenericArgument(interfaceType);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }

    //
    // Service definitions
    //

    private class FoldoutNode
    {
        public bool isOpened = false;
        public IList<FoldoutNode> childs = null;
    }

    private class FoldoutIndex
    {
        private FoldoutNode node;
        private int currentChildIdx = -1;

        public FoldoutIndex(FoldoutNode _node)
        {
            node = _node;
        }

        public FoldoutNode MoveNext()
        {
            currentChildIdx++;

            if (node.childs != null &&
                node.childs.Count > currentChildIdx)
            {
                return node.childs[currentChildIdx];
            }

            return null;
        }

        public void Add(FoldoutNode _node)
        {
            if (node.childs == null)
            {
                node.childs = new List<FoldoutNode>();
            }

            while (node.childs.Count <= currentChildIdx)
            {
                node.childs.Add(null);
            }

            node.childs[currentChildIdx] = _node;
        }
    }
}

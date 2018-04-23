using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public abstract class BaseMaterialGUI : BaseUnlitGUI
    {
        #region GUI Property Classes
        public abstract class BaseProperty
        {
            public BaseMaterialGUI Parent = null;

            protected Func<object, bool> IsVisible;

            protected BaseProperty(BaseMaterialGUI parent, Func<object, bool> isVisible = null)
            {
                Parent = parent;
                IsVisible = isVisible;
            }

            public abstract void OnFindProperty(MaterialProperty[] props);

            public abstract void OnGUI();
        }

        public class GroupProperty : BaseProperty
        {
            public string m_Title = string.Empty;

            private readonly BaseProperty[] m_ChildProperties;

            public GroupProperty(BaseMaterialGUI parent, BaseProperty[] childProperties, Func<object, bool> isVisible = null)
                : this(parent, string.Empty, childProperties, isVisible)
            {
            }

            public GroupProperty(BaseMaterialGUI parent, string groupTitle, BaseProperty[] childProperties, Func<object, bool> isVisible = null)
                : base(parent, isVisible)
            {
                m_Title = groupTitle;
                m_ChildProperties = childProperties;
            }

            public override void OnFindProperty(MaterialProperty[] props)
            {
                foreach (var c in m_ChildProperties)
                {
                    c.OnFindProperty(props);
                }
            }

            public override void OnGUI()
            {
                if (IsVisible == null || IsVisible(this))
                {
                    if (!string.IsNullOrEmpty(m_Title))
                    {
                        EditorGUILayout.LabelField(m_Title, EditorStyles.boldLabel);
                        EditorGUI.indentLevel++;
                    }

                    foreach (var c in m_ChildProperties)
                    {
                        c.OnGUI();
                    }

                    if (!string.IsNullOrEmpty(m_Title))
                    {
                        EditorGUI.indentLevel--;
                    }
                }
            }
        }
         
        public class Property : BaseProperty
        {
            public string PropertyName;
            public string PropertyText;

            protected MaterialProperty m_MaterialProperty = null;

            protected readonly GUIContent m_GuiContent = null;

            public bool IsMandatory = false;

            public bool IsValid
            {
                get { return m_MaterialProperty != null; }
            }
        
            public float FloatValue
            {
                get { return m_MaterialProperty.floatValue; }
                set { m_MaterialProperty.floatValue = value; }
            }

            public bool BoolValue
            {
                get { return Math.Abs(m_MaterialProperty.floatValue) > 0.0f; }
                set { m_MaterialProperty.floatValue = value ? 1.0f : 0.0f; }
            }

            public Texture TextureValue
            {
                get { return m_MaterialProperty != null ? m_MaterialProperty.textureValue : null; }
                set { if (m_MaterialProperty != null) { m_MaterialProperty.textureValue = value; } }
            }

            public Vector4 VectorValue
            {
                get { return m_MaterialProperty.vectorValue; }
                set { m_MaterialProperty.vectorValue = value; }
            }

            public Property(BaseMaterialGUI parent, string propertyName, string guiText, bool isMandatory = true)
                : this(parent, propertyName, guiText, string.Empty, isMandatory)
            {
            }

            public Property(BaseMaterialGUI parent, string propertyName, string guiText, string toolTip, bool isMandatory = true)
                : base(parent)
            {
                m_GuiContent = new GUIContent(guiText, toolTip);
                PropertyName = propertyName;
                PropertyText = guiText;
                IsMandatory = isMandatory;
            }

            public override void OnFindProperty(MaterialProperty[] props)
            {
                m_MaterialProperty = ShaderGUI.FindProperty(PropertyName, props, IsMandatory);
            }

            public override void OnGUI()
            {
                if (IsValid
                    && (IsVisible == null || IsVisible(this)))
                {
                    Parent.m_MaterialEditor.ShaderProperty(m_MaterialProperty, m_GuiContent);
                }
            }
        }

        public class ComboProperty : Property
        {
            private readonly string[] m_Options;
            private readonly int[] m_Values = null;

            public ComboProperty(BaseMaterialGUI parent, string propertyName, string guiText, string[] options, bool isMandatory = true)
                : base(parent, propertyName, guiText, isMandatory)
            {
                m_Options = options;
            }

            public ComboProperty(BaseMaterialGUI parent, string propertyName, string guiText, string toolTip, string[] options, bool isMandatory = true)
                : base(parent, propertyName, guiText, toolTip, isMandatory)
            {
                m_Options = options;
            }
            public ComboProperty(BaseMaterialGUI parent, string propertyName, string guiText, string[] options, int[] values, bool isMandatory = true)
                : base(parent, propertyName, guiText, isMandatory)
            {
                m_Options = options;
                m_Values = values;
            }

            public ComboProperty(BaseMaterialGUI parent, string propertyName, string guiText, string toolTip, string[] options, int[] values, bool isMandatory = true)
                : base(parent, propertyName, guiText, toolTip, isMandatory)
            {
                m_Options = options;
                m_Values = values;
            }

            public override void OnGUI()
            {
                if (IsValid
                    && (IsVisible == null || IsVisible(this)))
                {
                    EditorGUI.showMixedValue = m_MaterialProperty.hasMixedValue;
                    float floatValue = m_MaterialProperty.floatValue;

                    EditorGUI.BeginChangeCheck();

                    if (m_Values == null)
                    {
                        floatValue = EditorGUILayout.Popup(m_GuiContent, (int)floatValue, m_Options);
                    }
                    else
                    {
                        floatValue = EditorGUILayout.IntPopup(m_GuiContent.text, (int)floatValue, m_Options, m_Values);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        Parent.m_MaterialEditor.RegisterPropertyChangeUndo(PropertyName);
                        m_MaterialProperty.floatValue = (float)floatValue;
                    }

                    EditorGUI.showMixedValue = false;
                }
            }
        }

        public class DiffusionProfileProperty : Property
        {
            public DiffusionProfileProperty(BaseMaterialGUI parent, string propertyName, string guiText, string toolTip, bool isMandatory = true)
                : base(parent, propertyName, guiText, toolTip, isMandatory)
            {
            }

            public override void OnGUI()
            {
                if (IsValid
                    && (IsVisible == null || IsVisible(this)))
                {
                    var hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
                    if (hdPipeline == null)
                    {
                        return;
                    }

                    var diffusionProfileSettings = hdPipeline.diffusionProfileSettings;
                    if (hdPipeline.IsInternalDiffusionProfile(diffusionProfileSettings))
                    {
                        EditorGUILayout.HelpBox(
                            "No diffusion profile Settings have been assigned to the render pipeline asset.",
                            MessageType.Warning);
                        return;
                    }

                    // TODO: Optimize me
                    var profiles = diffusionProfileSettings.profiles;
                    var names = new GUIContent[profiles.Length + 1];
                    names[0] = new GUIContent("None");

                    var values = new int[names.Length];
                    values[0] = DiffusionProfileConstants.DIFFUSION_PROFILE_NEUTRAL_ID;

                    for (int i = 0; i < profiles.Length; i++)
                    {
                        names[i + 1] = new GUIContent(profiles[i].name);
                        values[i + 1] = i + 1;
                    }

                    using (var scope = new EditorGUI.ChangeCheckScope())
                    {
                        int profileID = (int) FloatValue;

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.PrefixLabel(m_GuiContent.text);

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                profileID = EditorGUILayout.IntPopup(profileID, names, values);

                                if (GUILayout.Button("Goto", EditorStyles.miniButton, GUILayout.Width(50f)))
                                {
                                    Selection.activeObject = diffusionProfileSettings;
                                }
                            }
                        }

                        if (scope.changed)
                        {
                            FloatValue = profileID;
                        }
                    }
                }
            }
        }

        public class TextureOneLineProperty : Property
        {
            public string ExtraPropertyName;

            private MaterialProperty m_ExtraProperty;

            public TextureOneLineProperty(BaseMaterialGUI parent, string propertyName, string guiText, bool isMandatory = true)
                : base(parent, propertyName, guiText, isMandatory)
            {
            }

            public TextureOneLineProperty(BaseMaterialGUI parent, string propertyName, string guiText, string toolTip, bool isMandatory = true)
                : base(parent, propertyName, guiText, toolTip, isMandatory)
            {
            }

            public TextureOneLineProperty(BaseMaterialGUI parent, string propertyName, string extraPropertyName, string guiText, string toolTip, bool isMandatory = true)
                : base(parent, propertyName, guiText, toolTip, isMandatory)
            {
                ExtraPropertyName = extraPropertyName;
            }

            public override void OnFindProperty(MaterialProperty[] props)
            {
                base.OnFindProperty(props);

                if (!string.IsNullOrEmpty(ExtraPropertyName))
                {
                    m_ExtraProperty = ShaderGUI.FindProperty(ExtraPropertyName, props, IsMandatory);
                }
            }

            public override void OnGUI()
            {
                if (IsValid && (IsVisible == null || IsVisible(this)))
                {
                    Parent.m_MaterialEditor.TexturePropertySingleLine(m_GuiContent, m_MaterialProperty, m_ExtraProperty);
                }
            }
        }

        public class TextureProperty : Property
        {
            public enum Tiling
            {
                Wrap,
                Clamp,
            }
            public enum Channel
            {
                R,
                G,
                B,
                A,
            }

            public enum UVSet
            {
                UV0,
                UV1,
                UV2,
                UV3,
                PlanarXY,
                PlanarYZ,
                PlanarZX,
                Triplanar,
            }

            public TextureOneLineProperty m_TextureProperty;

            public ComboProperty m_ChannelProperty;

            public ComboProperty m_TilingProperty;

            public ComboProperty m_UvSetProperty;

            public Property m_LocalOrWorldProperty;

            public Property m_RemapProperty;

            public Property m_InvertRemapProperty;

            public TextureProperty(BaseMaterialGUI parent, string propertyName, string constantPropertyName, string guiText, bool isMandatory = true, bool isNormalMap = false)
                : this(parent, propertyName, constantPropertyName, guiText, string.Empty, isMandatory, isNormalMap)
            {
            }

            public TextureProperty(BaseMaterialGUI parent, string propertyName, string constantPropertyName, string guiText, string toolTip, bool isMandatory = true, bool isNormalMap = false)
                : base(parent, propertyName, guiText, toolTip, isMandatory)
            {
                m_TextureProperty = new TextureOneLineProperty(parent, propertyName, constantPropertyName, guiText, toolTip, isMandatory);

                m_ChannelProperty = new ComboProperty(parent, propertyName + "Channel", "Channel", Enum.GetNames(typeof(Channel)), false);

                m_TilingProperty = new ComboProperty(parent, propertyName + "Tiling", "Tiling", Enum.GetNames(typeof(Tiling)), false);

                m_UvSetProperty = new ComboProperty(parent, propertyName + "UV", "UV Set", Enum.GetNames(typeof(UVSet)), false);
                m_LocalOrWorldProperty = new Property(parent, propertyName + "LocalOrWorld", "Local Space", "Whether Planar or Triplanar is using Local or World space.", false);

                m_RemapProperty = new Property(parent, constantPropertyName + "Remap", "Remapping", "Defines the range to remap/scale the values in texture", false);
                m_InvertRemapProperty = new Property(parent, constantPropertyName + "RemapInverted", "Invert Remapping", "Whether the mapping values are inverted.", false);
            }

            public override void OnFindProperty(MaterialProperty[] props)
            {
                base.OnFindProperty(props);

                m_TextureProperty.OnFindProperty(props);
                m_TilingProperty.OnFindProperty(props);
                m_UvSetProperty.OnFindProperty(props);
                m_LocalOrWorldProperty.OnFindProperty(props);
                m_RemapProperty.OnFindProperty(props);
                m_InvertRemapProperty.OnFindProperty(props);
            }

            public override void OnGUI()
            {
                if (m_TextureProperty.IsValid
                    && (IsVisible == null || IsVisible(this)))
                {
                    m_TextureProperty.OnGUI();

                    if (m_TextureProperty.TextureValue != null)
                    {
                        EditorGUI.indentLevel++;

                        m_TilingProperty.OnGUI();
                        m_UvSetProperty.OnGUI();
                        m_LocalOrWorldProperty.OnGUI();

                        if (m_RemapProperty.IsValid)
                        {
                            // Display the remap of texture values.
                            Vector2 remap = m_RemapProperty.VectorValue;
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.MinMaxSlider(m_RemapProperty.PropertyText, ref remap.x, ref remap.y, 0.0f, 1.0f);
                            if (EditorGUI.EndChangeCheck())
                            {
                                m_RemapProperty.VectorValue = remap;
                            }

                            m_InvertRemapProperty.OnGUI();
                        }

                        EditorGUI.indentLevel--;
                    }
                }
            }
        }
        #endregion
    }
}

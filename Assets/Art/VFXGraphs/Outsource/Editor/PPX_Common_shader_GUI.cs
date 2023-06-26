using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PPX_Common_shader_GUI : ShaderGUI
{
    MaterialProperty _Ztest;

    MaterialProperty _Zwrite;

    MaterialProperty _Cull;

    MaterialProperty _add_or_blend;

    MaterialProperty _base_texture;

    MaterialProperty _use_custom2_xyzw_control_base_uv;

    MaterialProperty _base_uv;

    MaterialProperty _base_speed;

    MaterialProperty _use_change_color;

    MaterialProperty _HUE;

    MaterialProperty _Saturation;

    MaterialProperty _Value;

    MaterialProperty _alpha_power;

    MaterialProperty _Fade_distance;

    MaterialProperty _use_doubel_pass;

    MaterialProperty _base_front_color;

    MaterialProperty _base_front_power;

    MaterialProperty _base_back_color;

    MaterialProperty _base_back_power;

    MaterialProperty _use_emissive;

    MaterialProperty _emissive_tex;
    MaterialProperty _connect_base_alone;
    MaterialProperty _emissive_uv;
    MaterialProperty _emissive_speed;
    MaterialProperty _Emissive_color;

    MaterialProperty _Emissive_power;

    MaterialProperty _use_second_tex;

    MaterialProperty _use_dissolve_or_mul;

    MaterialProperty _dissolve_texture;

    MaterialProperty _dissolve_uv;

    MaterialProperty _dissolve_speed;

    MaterialProperty _edge_hardness;

    MaterialProperty _dissolve;

    MaterialProperty _use_custom1_x_dissolve;

    MaterialProperty _use_distort;

    MaterialProperty _Distort_tex;

    MaterialProperty _Distort_mask;

    MaterialProperty _distort_uv;

    MaterialProperty _distort_speed;

    MaterialProperty _distort_power;

    MaterialProperty _use_custom1_z_distort;

    MaterialProperty _use_color_tex;

    MaterialProperty _color_Tex;

    MaterialProperty _color_uv;

    MaterialProperty _color_speed;

    MaterialProperty _reduce_color;

    MaterialProperty _use_displace;

    MaterialProperty _displace_tex;

    MaterialProperty _displace_uv;

    MaterialProperty _displace_speed;

    MaterialProperty _displace_power;

    MaterialProperty _displace_mask_tex;

    MaterialProperty _displace_mask_uv;

    MaterialProperty _displace_mask_speed;

    MaterialProperty _use_custom1_w_displace;

    public override void OnGUI(
        MaterialEditor materialEditor,
        MaterialProperty[] properties
    )
    {
        EditorGUILayout
            .LabelField("MainShader", EditorStyles.miniButton);
        EditorGUILayout
            .LabelField("MainControl", EditorStyles.miniButton);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        _add_or_blend = FindProperty("_add_or_blend", properties);
        materialEditor.ShaderProperty(_add_or_blend, "AddandBlend");
        _base_texture = FindProperty("_base_texture", properties);
        materialEditor
            .TexturePropertySingleLine(new GUIContent("Main"),
            _base_texture);

        _use_custom2_xyzw_control_base_uv =
            FindProperty("_use_custom2_xyzw_control_base_uv", properties);
        materialEditor
            .ShaderProperty(_use_custom2_xyzw_control_base_uv,
            "use custom2 xyzw");
        _base_uv = FindProperty("_base_uv", properties);
        materialEditor.ShaderProperty(_base_uv, "base UV");
        _base_speed = FindProperty("_base_speed", properties);
        materialEditor.ShaderProperty(_base_speed, "baseSpeed");
        EditorGUILayout
.LabelField("XY Flow", EditorStyles.miniButton);
        _use_change_color = FindProperty("_use_change_color", properties);
        materialEditor.ShaderProperty(_use_change_color, "use change color");
        if (_use_change_color.floatValue == 1)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _HUE = FindProperty("_HUE", properties);
            materialEditor.ShaderProperty(_HUE, "hue shift");
            _Saturation = FindProperty("_Saturation", properties);
            materialEditor.ShaderProperty(_Saturation, "saturation");
            _Value = FindProperty("_Value", properties);
            materialEditor.ShaderProperty(_Value, "Value");
            EditorGUILayout.EndVertical();
        }

        _alpha_power = FindProperty("_alpha_power", properties);
        materialEditor.ShaderProperty(_alpha_power, "alpha power");
        _Fade_distance = FindProperty("_Fade_distance", properties);
        materialEditor.ShaderProperty(_Fade_distance, "FD");
        _use_doubel_pass = FindProperty("_use_doubel_pass", properties);
        materialEditor.ShaderProperty(_use_doubel_pass, "double pass color");
        _base_front_color = FindProperty("_base_front_color", properties);
        materialEditor.ShaderProperty(_base_front_color, "base front color");
        _base_front_power = FindProperty("_base_front_power", properties);
        materialEditor.ShaderProperty(_base_front_power, "base front pow");

        if (_use_doubel_pass.floatValue == 1)
        {
            _base_back_color = FindProperty("_base_back_color", properties);
            materialEditor.ShaderProperty(_base_back_color, "base back color");
            _base_back_power = FindProperty("_base_back_power", properties);
            materialEditor.ShaderProperty(_base_back_power, "base back pow");
        }
        EditorGUILayout.EndVertical();


        #region [Emissive]
        EditorGUILayout.LabelField("Noise Emissive", EditorStyles.miniButton);

        //画box
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        _use_emissive = FindProperty("_use_emissive", properties);
        materialEditor.ShaderProperty(_use_emissive, "use emissive");

        if (_use_emissive.floatValue == 1)
        {
            _emissive_tex = FindProperty("_emissive_tex", properties);
            materialEditor
                .TexturePropertySingleLine(new GUIContent("emissive tex"),
                _emissive_tex);

            _connect_base_alone = FindProperty("_connect_base_alone", properties);
            materialEditor.ShaderProperty(_connect_base_alone, "Emissive UV");
            if (_connect_base_alone.floatValue == 1)
            {
                _emissive_uv = FindProperty("_emissive_uv", properties);
                materialEditor.ShaderProperty(_emissive_uv, "uv");
                _emissive_speed = FindProperty("_emissive_speed", properties);
                materialEditor.ShaderProperty(_emissive_speed, "emissive speed");
            }
            _Emissive_color = FindProperty("_Emissive_color", properties);
            materialEditor.ShaderProperty(_Emissive_color, "emissive color");
            _Emissive_power = FindProperty("_Emissive_power", properties);
            materialEditor.ShaderProperty(_Emissive_power, "emissive pow");
        }
        EditorGUILayout.EndVertical();


        #endregion



        #region [Dissolve]
        EditorGUILayout.LabelField("Dissolve", EditorStyles.miniButton);

        _use_second_tex = FindProperty("_use_second_tex", properties);
        materialEditor.ShaderProperty(_use_second_tex, "second tex");
        if (_use_second_tex.floatValue == 1)
        {
            _dissolve_texture = FindProperty("_dissolve_texture", properties);
            materialEditor
                .TexturePropertySingleLine(new GUIContent("dissolve texture"),
                _dissolve_texture);
            _dissolve_uv = FindProperty("_dissolve_uv", properties);
            materialEditor.ShaderProperty(_dissolve_uv, "dissolve uv");
            _dissolve_speed = FindProperty("_dissolve_speed", properties);
            materialEditor.ShaderProperty(_dissolve_speed, "dissolve speed");

            _use_dissolve_or_mul =
                FindProperty("_use_dissolve_or_mul", properties);
            materialEditor
                .ShaderProperty(_use_dissolve_or_mul, "use dissolve or mult");
            if (_use_dissolve_or_mul.floatValue == 1)
            {
                _edge_hardness = FindProperty("_edge_hardness", properties);
                materialEditor.ShaderProperty(_edge_hardness, "edge hardness");
                _dissolve = FindProperty("_dissolve", properties);
                materialEditor.ShaderProperty(_dissolve, "dissolve");
                _use_custom1_x_dissolve =
                    FindProperty("_use_custom1_x_dissolve", properties);
                materialEditor
                    .ShaderProperty(_use_custom1_x_dissolve,
                    "use custom1 x dissolve");
            }
        }
        #endregion



        #region [Distortion]
        EditorGUILayout.LabelField("Distortion", EditorStyles.miniButton);
        _use_distort = FindProperty("_use_distort", properties);
        materialEditor.ShaderProperty(_use_distort, "use distortion");
        if (_use_distort.floatValue == 1)
        {
            _Distort_tex = FindProperty("_Distort_tex", properties);
            materialEditor
                .TexturePropertySingleLine(new GUIContent("Distortion tex"),
                _Distort_tex);
            _distort_uv = FindProperty("_distort_uv", properties);
            materialEditor.ShaderProperty(_distort_uv, "distortion uv");
            _distort_speed = FindProperty("_distort_speed", properties);
            materialEditor.ShaderProperty(_distort_speed, "distortion speed");
            _Distort_mask = FindProperty("_Distort_mask", properties);
            materialEditor.ShaderProperty(_Distort_mask, "distortion mask");
            _distort_power = FindProperty("_distort_power", properties);
            materialEditor.ShaderProperty(_distort_power, "distortion pow");
            _use_custom1_z_distort =
                FindProperty("_use_custom1_z_distort", properties);
            materialEditor
                .ShaderProperty(_use_custom1_z_distort,
                "use custom1 z distortion");
        }
        #endregion



        #region [Color]
        EditorGUILayout.LabelField("Color", EditorStyles.miniButton);

        _use_color_tex = FindProperty("_use_color_tex", properties);
        materialEditor.ShaderProperty(_use_color_tex, "use color tex");
        if (_use_color_tex.floatValue == 1)
        {
            _color_Tex = FindProperty("_color_Tex", properties);
            materialEditor
                .TexturePropertySingleLine(new GUIContent("color tex"), _color_Tex);
            _color_uv = FindProperty("_color_uv", properties);
            materialEditor.ShaderProperty(_color_uv, "uv");
            _color_speed = FindProperty("_color_speed", properties);
            materialEditor.ShaderProperty(_color_speed, "Color speed");
            _reduce_color = FindProperty("_reduce_color", properties);
            materialEditor.ShaderProperty(_reduce_color, "desat");
        }
        #endregion



        #region [WPO]
        EditorGUILayout.LabelField("WPO", EditorStyles.miniButton);
        _use_displace = FindProperty("_use_displace", properties);
        materialEditor.ShaderProperty(_use_displace, "use displace");
        if (_use_displace.floatValue == 1)
        {
            _displace_tex = FindProperty("_displace_tex", properties);
            materialEditor
                .TexturePropertySingleLine(new GUIContent("WPO tex"),
                _displace_tex);
            _displace_uv = FindProperty("_displace_uv", properties);
            materialEditor.ShaderProperty(_displace_uv, "WPO UV");
            _displace_speed = FindProperty("_displace_speed", properties);
            materialEditor.ShaderProperty(_displace_speed, "WPO Speed");
            _displace_power = FindProperty("_displace_power", properties);
            materialEditor.ShaderProperty(_displace_power, "WPO Pow");
            _displace_mask_tex = FindProperty("_displace_mask_tex", properties);
            materialEditor
                .TexturePropertySingleLine(new GUIContent("displace mask uv "),
                _displace_mask_tex);
            _displace_mask_uv = FindProperty("_displace_mask_uv", properties);
            materialEditor.ShaderProperty(_displace_mask_uv, "uv");
            _displace_mask_speed =
                FindProperty("_displace_mask_speed", properties);
            materialEditor.ShaderProperty(_displace_mask_speed, "displace mask speed");
            _use_custom1_w_displace =
                FindProperty("_use_custom1_w_displace", properties);
            materialEditor
                .ShaderProperty(_use_custom1_w_displace,
                "use custom1 w displace");
        }
        #endregion



        #region [ZDepth]

        EditorGUILayout.LabelField("Zdepth", EditorStyles.miniButton);

        //画box
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        _Ztest = FindProperty("_Ztest", properties);
        materialEditor.ShaderProperty(_Ztest, "_Ztest");
        _Zwrite = FindProperty("_Zwrite", properties);
        materialEditor.ShaderProperty(_Zwrite, "_Zwrite");
        _Cull = FindProperty("_Cull", properties);
        materialEditor.ShaderProperty(_Cull, "_Cull");

        materialEditor.RenderQueueField();
        EditorGUILayout.EndVertical();
        #endregion



        //#region [EndNotes]
        //EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        //EditorGUILayout
        //    .LabelField("ParticleShader",
        //    EditorStyles.miniButton);
        //EditorGUILayout
        //    .LabelField("Test in demo",
        //    EditorStyles.miniButton);
        //EditorGUILayout
        //    .LabelField("Vince Wedde shader",
        //    EditorStyles.miniButton);
        //EditorGUILayout
        //    .LabelField("End", EditorStyles.miniButton);

        //EditorGUILayout.EndVertical();


        //#endregion

    }
}
